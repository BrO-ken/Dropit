using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Dropit.Core;
using static Dropit.Core.Loc;

namespace Dropit.App.Shell;

/// <summary>Where an in-app update stands.</summary>
internal enum UpdateStep
{
    /// <summary>Nothing is going on.</summary>
    Idle,
    /// <summary>Asking GitHub for the latest release.</summary>
    Checking,
    /// <summary>A newer release is there and is waiting for the user.</summary>
    Available,
    /// <summary>The installer is being downloaded.</summary>
    Downloading,
    /// <summary>The installer is running; it will close the app and start the new version.</summary>
    Installing,
    /// <summary>The update did not work; the reason is in <see cref="Updater.LastError"/>.</summary>
    Failed,
    /// <summary>The user declined the administrator prompt, or closed the installer.</summary>
    Cancelled,
}

/// <summary>
/// Over-the-air updates from GitHub releases. Asks GitHub for the latest release now and then (no data about
/// your files is sent), downloads the installer, checks its SHA-256 and runs it silently; the installer then
/// starts the new version. Portable copies (no uninstaller next to the exe) get the release page instead.
/// </summary>
internal sealed class Updater
{
    private static readonly HttpClient Http = CreateHttp();

    /// <summary>A download that receives nothing for this long is given up.</summary>
    private static readonly TimeSpan StallTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Win32 ERROR_CANCELLED: the user declined the administrator prompt.</summary>
    private const int ErrorCancelled = 1223;

    private DispatcherTimer? _timer;
    private bool _announced;

    public static Version Current => UpdateInfo.Normalize(typeof(Updater).Assembly.GetName().Version ?? new Version(0, 0, 0));

    /// <summary>Installed with the setup (which can update in place) rather than unzipped somewhere.</summary>
    public static bool IsInstalled => File.Exists(Path.Combine(AppContext.BaseDirectory, "unins000.exe"));

    public UpdateInfo? Available { get; private set; }

    public DateTime? LastChecked { get; private set; }

    public string? LastError { get; private set; }

    /// <summary>Where the update stands. The window and the settings both read this.</summary>
    public UpdateStep Step { get; private set; } = UpdateStep.Idle;

    /// <summary>How much of the installer has been downloaded, 0 to 1. Null when the size is unknown.</summary>
    public double? Progress { get; private set; }

    public event Action? Changed;

    public async Task<UpdateInfo?> CheckAsync()
    {
        Step = UpdateStep.Checking;
        Changed?.Invoke();
        try
        {
            var json = await Http.GetStringAsync(UpdateInfo.LatestReleaseApi);
            var latest = UpdateInfo.Parse(json);
            Available = latest is not null && latest.IsNewerThan(Current) ? latest : null;
            LastError = null;
            Step = Available is not null ? UpdateStep.Available : UpdateStep.Idle;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or KeyNotFoundException)
        {
            LastError = ex.Message;
            Step = UpdateStep.Failed;
        }
        LastChecked = DateTime.Now;
        Changed?.Invoke();
        return Available;
    }

    /// <summary>Checks shortly after start and then every few hours while enabled.</summary>
    public void StartBackgroundChecks(AppSettings settings, Action<UpdateInfo> found)
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
        _timer.Tick += async (_, _) =>
        {
            _timer.Interval = TimeSpan.FromHours(6);
            if (!settings.CheckForUpdates || Step is UpdateStep.Downloading or UpdateStep.Installing)
                return;
            if (await CheckAsync() is { } update && !_announced)
            {
                _announced = true;
                found(update);
            }
        };
        _timer.Start();
    }

    /// <summary>
    /// Downloads the installer, checks it against the checksum published on the release and runs it. A copy
    /// that was not installed through the setup has no uninstaller to replace, so it gets the release page.
    /// </summary>
    public async Task InstallAsync(UpdateInfo update)
    {
        if (Step is UpdateStep.Downloading or UpdateStep.Installing)
            return;
        if (!IsInstalled || update.InstallerUrl is null)
        {
            Open(update.PageUrl);
            return;
        }

        // Never run a release whose checksum GitHub did not publish: without one there is nothing
        // to check the download against, and the installer runs silently.
        if (update.InstallerSha256 is not { } expected || !IsSha256Hex(expected))
        {
            Fail(T("The release has no published SHA-256, so the update was refused.", "لا يحتوي الإصدار على قيمة SHA-256 منشورة، لذلك رُفض التحديث."));
            return;
        }

        Step = UpdateStep.Downloading;
        Progress = 0;
        Changed?.Invoke();
        // A unique name: a predictable file in %TEMP% could be swapped between the check and the launch.
        var target = Path.Combine(Path.GetTempPath(), $"Dropit-Setup-{update.Version}-{Guid.NewGuid():N}.exe");
        try
        {
            Notifications.Show(T("Downloading the update…", "جارٍ تنزيل التحديث…"), $"Dropit {update.Version}");

            // An earlier attempt may already have downloaded the same file; if it is the right one, keep it.
            var reusable = await FindVerifiedCopy(update.Version, expected);
            if (reusable is not null)
            {
                target = reusable;
            }
            else
            {
                await DownloadAsync(update.InstallerUrl, target);
            }

            await using (var file = File.OpenRead(target))
            {
                var actual = Convert.ToHexString(await SHA256.HashDataAsync(file)).ToLowerInvariant();
                if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(actual), Encoding.ASCII.GetBytes(expected)))
                    throw new InvalidOperationException(T("The downloaded installer doesn't match the checksum published on GitHub.", "لا يطابق المثبِّت المنزَّل المجموع الاختباري المنشور على GitHub."));
            }

            Step = UpdateStep.Installing;
            Progress = null;
            Changed?.Invoke();
            // The installer closes this app, installs over it and starts the new version (see installer\Dropit.iss).
            Process.Start(new ProcessStartInfo(target, "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS") { UseShellExecute = true });
            Application.Current.Shutdown();
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
        {
            // The user said no at the administrator prompt. That is an answer, not a failure.
            Step = UpdateStep.Cancelled;
            Progress = null;
            LastError = null;
            Changed?.Invoke();
        }
        catch (Exception ex)
        {
            Fail(ex.Message);
            OutputPaths.TryDelete(target);
        }
    }

    /// <summary>
    /// Looks for a file this app already downloaded for the same version and checks it. The name is unique each
    /// time, so an interrupted attempt leaves its file behind rather than a half-written one.
    /// </summary>
    private static async Task<string?> FindVerifiedCopy(Version version, string expected)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(Path.GetTempPath(), $"Dropit-Setup-{version}-*.exe"))
            {
                await using var stream = File.OpenRead(file);
                var actual = Convert.ToHexString(await SHA256.HashDataAsync(stream)).ToLowerInvariant();
                if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(actual), Encoding.ASCII.GetBytes(expected)))
                    return file;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
        return null;
    }

    /// <summary>
    /// Downloads to a file, giving up if nothing arrives for a while. A server that keeps the connection open
    /// without sending would otherwise hang the app forever.
    /// </summary>
    private async Task DownloadAsync(string url, string target)
    {
        using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength;
        await using var input = await response.Content.ReadAsStreamAsync();
        await using var output = File.Create(target);
        var buffer = new byte[81920];
        using var stall = new CancellationTokenSource();
        stall.CancelAfter(StallTimeout);
        long read = 0;
        while (true)
        {
            var count = await input.ReadAsync(buffer, stall.Token);
            if (count == 0)
                break;
            // Data arrived: give the server another full stall window before giving up on it.
            stall.CancelAfter(StallTimeout);
            await output.WriteAsync(buffer.AsMemory(0, count));
            read += count;
            if (total is > 0)
            {
                Progress = Math.Clamp((double)read / total.Value, 0, 1);
                Changed?.Invoke();
            }
        }
    }

    private void Fail(string message)
    {
        Step = UpdateStep.Failed;
        Progress = null;
        LastError = message;
        Changed?.Invoke();
        Notifications.Show(T("Update failed", "فشل التحديث"), message, error: true);
    }

    private static bool IsSha256Hex(string value) =>
        value.Length == 64 && value.All(Uri.IsHexDigit);

    public static void Open(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
    }

    private static HttpClient CreateHttp()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"Dropit/{Current}");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }
}
