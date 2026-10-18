using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using FileAction.Core;
using static FileAction.Core.Loc;

namespace FileAction.App.Shell;

/// <summary>
/// Over-the-air updates from GitHub releases. Asks GitHub for the latest release now and then (no data about
/// your files is sent), downloads the installer, checks its SHA-256 and runs it silently; the installer then
/// starts the new version. Portable copies (no uninstaller next to the exe) get the release page instead.
/// </summary>
internal sealed class Updater
{
    private static readonly HttpClient Http = CreateHttp();
    private DispatcherTimer? _timer;
    private bool _announced;

    public static Version Current => UpdateInfo.Normalize(typeof(Updater).Assembly.GetName().Version ?? new Version(0, 0, 0));

    /// <summary>Installed with the setup (which can update in place) rather than unzipped somewhere.</summary>
    public static bool IsInstalled => File.Exists(Path.Combine(AppContext.BaseDirectory, "unins000.exe"));

    public UpdateInfo? Available { get; private set; }

    public DateTime? LastChecked { get; private set; }

    public string? LastError { get; private set; }

    public bool Installing { get; private set; }

    public event Action? Changed;

    public async Task<UpdateInfo?> CheckAsync()
    {
        try
        {
            var json = await Http.GetStringAsync(UpdateInfo.LatestReleaseApi);
            var latest = UpdateInfo.Parse(json);
            Available = latest is not null && latest.IsNewerThan(Current) ? latest : null;
            LastError = null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or KeyNotFoundException)
        {
            LastError = ex.Message;
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
            if (!settings.CheckForUpdates || Installing)
                return;
            if (await CheckAsync() is { } update && !_announced)
            {
                _announced = true;
                found(update);
            }
        };
        _timer.Start();
    }

    public async Task InstallAsync(UpdateInfo update)
    {
        if (Installing)
            return;
        if (!IsInstalled || update.InstallerUrl is null)
        {
            Open(update.PageUrl);
            return;
        }

        Installing = true;
        Changed?.Invoke();
        // A unique name: a predictable file in %TEMP% could be swapped between the check and the launch.
        var target = Path.Combine(Path.GetTempPath(), $"FileAction-Setup-{update.Version}-{Guid.NewGuid():N}.exe");
        try
        {
            Notifications.Show(T("Downloading the update…", "جارٍ تنزيل التحديث…"), $"FileAction {update.Version}");
            using (var response = await Http.GetAsync(update.InstallerUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                await using var input = await response.Content.ReadAsStreamAsync();
                await using var output = File.Create(target);
                await input.CopyToAsync(output);
            }

            // Never run a release whose checksum GitHub did not publish: without one there is nothing
            // to check the download against, and the installer runs silently.
            if (update.InstallerSha256 is not { } expected || !IsSha256Hex(expected))
                throw new InvalidOperationException(T("The release has no published SHA-256, so the update was refused.", "لا يحتوي الإصدار على قيمة SHA-256 منشورة، لذلك رُفض التحديث."));
            await using var file = File.OpenRead(target);
            var actual = Convert.ToHexString(await SHA256.HashDataAsync(file)).ToLowerInvariant();
            if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(actual), Encoding.ASCII.GetBytes(expected)))
                throw new InvalidOperationException(T("The downloaded installer doesn't match the checksum published on GitHub.", "لا يطابق المثبِّت المنزَّل المجموع الاختباري المنشور على GitHub."));

            // The installer closes this app, installs over it and starts the new version (see installer\FileAction.iss).
            Process.Start(new ProcessStartInfo(target, "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS") { UseShellExecute = true });
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            Installing = false;
            Changed?.Invoke();
            OutputPaths.TryDelete(target);
            Notifications.Show(T("Update failed", "فشل التحديث"), ex.Message, error: true);
        }
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
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"FileAction/{Current}");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }
}
