using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Windows.Threading;
using FileAction.App.Native;

namespace FileAction.App.Shell;

/// <summary>
/// Keeps one running copy. Later launches (e.g. from the Explorer context menu) hand their arguments to
/// the running copy over a named pipe and exit.
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    // Pipe names are machine-wide, so include the user; string.GetHashCode is randomized per process.
    private static readonly string Id = "FileAction-" + new string(Environment.UserName.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _cts = new();

    private SingleInstance(Mutex mutex) => _mutex = mutex;

    public static SingleInstance? TryAcquire()
    {
        var mutex = new Mutex(initiallyOwned: true, @"Local\" + Id, out var created);
        if (created)
            return new SingleInstance(mutex);
        mutex.Dispose();
        return null;
    }

    /// <summary>Sends arguments to the running copy; returns false if it could not be reached.</summary>
    public static bool Forward(string[] args)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", Id, PipeDirection.Out);
            client.Connect(3000);
            // Let the running copy bring its wheel to the front; we were started by the user so we may grant this.
            if (Win32.GetNamedPipeServerProcessId(client.SafePipeHandle.DangerousGetHandle(), out var pid))
                Win32.AllowSetForegroundWindow((int)pid);
            using var writer = new StreamWriter(client, new UTF8Encoding(false));
            writer.Write(string.Join("\n", args));
            writer.Flush();
            return true;
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// The pipe is machine-wide and its name contains the user name, so without an explicit ACL any other
    /// local account could push arguments to the running copy. Grant access to this user only.
    /// </summary>
    private static PipeSecurity CurrentUserOnly()
    {
        var security = new PipeSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        var user = WindowsIdentity.GetCurrent().User;
        if (user is not null)
            security.SetAccessRule(new PipeAccessRule(user, PipeAccessRights.ReadWrite | PipeAccessRights.Synchronize, AccessControlType.Allow));
        foreach (var sid in new[]
                 {
                     new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                     new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                 })
            security.SetAccessRule(new PipeAccessRule(sid, PipeAccessRights.FullControl, AccessControlType.Allow));
        return security;
    }

    public void Listen(Dispatcher dispatcher, Action<string[]> onArgs)
    {
        Task.Run(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    // .NET 10 takes the security descriptor through this factory; the NamedPipeServerStream
                    // constructors no longer accept a PipeSecurity.
                    await using var server = NamedPipeServerStreamAcl.Create(
                        Id, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous,
                        0, 0, CurrentUserOnly(), HandleInheritability.None, (PipeAccessRights)0);
                    await server.WaitForConnectionAsync(_cts.Token);
                    using var reader = new StreamReader(server, Encoding.UTF8);
                    var text = await reader.ReadToEndAsync(_cts.Token);
                    var args = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    _ = dispatcher.BeginInvoke(() => onArgs(args));
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (IOException)
                {
                    await Task.Delay(200);
                }
            }
        });
    }

    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            _mutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
        }
        _mutex.Dispose();
    }
}
