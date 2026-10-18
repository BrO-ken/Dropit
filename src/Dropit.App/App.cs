using System.IO;
using System.Windows;
using System.Windows.Threading;
using Dropit.App.Actions;
using Dropit.App.Input;
using Dropit.App.Jobs;
using Dropit.App.Shell;
using Dropit.App.UI;
using Dropit.App.Wheel;
using Dropit.Core;
using static Dropit.Core.Loc;
using System.Globalization;

namespace Dropit.App;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length >= 1 && args[0] == "--self-test")
            return SelfTest.Run(args.Length >= 2 ? args[1] : "self-test");
        if (args.Contains("--unregister"))
        {
            // Called by the uninstaller.
            ShellIntegration.SetContextMenu(false);
            ShellIntegration.SetAutostart(false);
            return 0;
        }

        using var instance = SingleInstance.TryAcquire();
        if (instance is null)
        {
            // Already running: hand over the request. A bare launch opens the window, not the settings.
            SingleInstance.Forward(args.Length > 0 ? args : ["--home"]);
            return 0;
        }
        var app = new App(instance, args);
        return app.Run();
    }
}

internal sealed class App : Application
{
    private readonly SingleInstance _instance;
    private readonly string[] _args;
    private AppSettings _settings = null!;
    private ToolLocator _tools = null!;
    private GlobalInput? _input;
    private WheelController _controller = null!;
    private TrayIcon? _tray;
    private HotkeyListener? _hotkey;
    private HotkeyPreset? _activeHotkey;
    private readonly Updater _updater = new();

    public App(SingleInstance instance, string[] args)
    {
        _instance = instance;
        _args = args;
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        DispatcherUnhandledException += OnUnhandled;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settings = AppSettings.Load();
        Loc.Current = _settings.Language ?? Loc.Detect();
        // The saved theme has to be applied before anything builds its brushes from it.
        Theme.Override = _settings.Theme;
        Theme.Refresh();
        _tools = new ToolLocator();
        var jobs = new JobRunner(_settings, _tools);
        SettingsWindow.DownloadTool = tool => jobs.DownloadToolsAsync([tool], ask: false);

        _input = new GlobalInput(Dispatcher);
        try
        {
            _input.Start();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Dropit", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        _controller = new WheelController(_settings, _tools, jobs, _input);
        Ocr.OcrOperation.CopyToClipboard = text => Dispatcher.BeginInvoke(() =>
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (System.Runtime.InteropServices.COMException)
            {
            }
        });
        _tray = CreateTray();
        SettingsWindow.Updater = _updater;
        SettingsWindow.Operations = () => _controller.AllOperations;
        _updater.StartBackgroundChecks(_settings, update => _tray?.OfferUpdate(update.Version, () => _ = _updater.InstallAsync(update)));
        _hotkey = new HotkeyListener(_controller.OnHotkey);
        ApplyHotkey();
        _instance.Listen(Dispatcher, HandleArgs);

        ShellIntegration.Refresh();
        if (!_settings.FirstRunDone)
        {
            _settings.FirstRunDone = true;
            _settings.Save();
            TryIntegrateOnFirstRun();
            OpenHome();
        }
        else if (_args.Contains("--updated"))
        {
            _tray.Show(T("Dropit was updated", "تم تحديث Dropit"), T($"Version {Updater.Current}", $"الإصدار {Updater.Current}"), false);
        }
        else if (!_args.Contains("--background"))
        {
            // Launched from the exe: show the window rather than a balloon tip, which cannot be
            // moved, resized or closed and tells the user nothing they can act on.
            OpenHome();
        }
        HandleArgs(_args);
    }

    private void OpenHome() => UI.HomeWindow.ShowSingle(_settings, OpenSettings, ShowWheel);

    /// <summary>Opens the wheel for dropped paths, or for the clipboard when there are none.</summary>
    private void ShowWheel(IReadOnlyList<string>? paths)
    {
        if (paths is { Count: > 0 })
        {
            _controller.QueuePaths(paths);
            return;
        }
        _controller.OpenForClipboard();
    }

    private static void TryIntegrateOnFirstRun()
    {
        try
        {
            ShellIntegration.SetContextMenu(true);
            ShellIntegration.SetAutostart(true);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
        }
    }

    private TrayIcon CreateTray()
    {
        var tray = new TrayIcon(OpenSettings, () => _controller.OpenForClipboard(), () => ShelfWindow.Open([]), ShowHelp, SetPaused, _settings.Paused, () => Shutdown());
        if (_updater.Available is { } update)
            tray.OfferUpdate(update.Version, () => _ = _updater.InstallAsync(update));
        return tray;
    }

    private void HandleArgs(string[] args)
    {
        if (args.Contains("--settings"))
            OpenSettings();
        if (args.Contains("--home"))
            OpenHome();
        var wheelIndex = Array.IndexOf(args, "--wheel");
        if (wheelIndex >= 0)
            _controller.QueuePaths(args.Skip(wheelIndex + 1).Where(a => !a.StartsWith("--", StringComparison.Ordinal)));
    }

    private void OpenSettings() => SettingsWindow.ShowSingle(_settings, _tools, OnSettingsChanged);

    private void OnSettingsChanged()
    {
        var language = _settings.Language ?? Loc.Detect();
        if (language != Loc.Current)
        {
            Loc.Current = language;
            _controller.Relocalize();
            // Arabic is right to left: every window already open has to be re-flowed.
            Direction.ApplyToOpenWindows();
            _tray?.Dispose();
            _tray = CreateTray();
        }
        ApplyHotkey();
        if (_tray is not null)
            _tray.Paused = _settings.Paused;
    }

    /// <summary>
    /// Registers the chosen shortcut. If another app already owns it, switches to the first free preset
    /// and says so once, instead of leaving the user without a shortcut.
    /// </summary>
    private void ApplyHotkey()
    {
        var wanted = _settings.Hotkey;
        if (_hotkey is null || _activeHotkey == wanted)
            return;
        if (_hotkey.Register(wanted))
        {
            _activeHotkey = wanted;
            return;
        }

        var taken = AppSettings.HotkeyName(wanted);
        foreach (var preset in Enum.GetValues<HotkeyPreset>().Where(p => p != HotkeyPreset.None && p != wanted))
        {
            if (!_hotkey.Register(preset))
                continue;
            _activeHotkey = preset;
            _settings.Hotkey = preset;
            _settings.Save();
            SettingsWindow.RefreshIfOpen();
            var now = AppSettings.HotkeyName(preset);
            Notifications.Show(T("Shortcut changed", "تم تغيير الاختصار"),
                T($"{taken} is used by another app, so Dropit now uses {now}. You can change it in Settings.", $"{taken} مستخدم من تطبيق آخر، لذا يستخدم Dropit الآن {now}. يمكنك تغييره في الإعدادات."));
            return;
        }

        _activeHotkey = HotkeyPreset.None;
        Notifications.Show(T("Shortcut is taken", "الاختصار مستخدم بالفعل"),
            T("All offered shortcuts are used by other apps. Open the wheel by dragging or from the tray menu.", "كل الاختصارات المعروضة مستخدمة من تطبيقات أخرى. افتح العجلة بالسحب أو من قائمة منطقة الإشعارات."), error: true);
    }

    private void SetPaused(bool paused)
    {
        _settings.Paused = paused;
        _settings.Save();
    }

    private void ShowHelp()
    {
        var convert = AppSettings.KeyName(_settings.ConvertKey);
        var tools = AppSettings.KeyName(_settings.ToolsKey);
        var actions = AppSettings.KeyName(_settings.ActionsKey);
        var hotkey = AppSettings.HotkeyName(_settings.Hotkey);
        var text = T(
            $"""
            1. Grab a file with the mouse (in File Explorer, on the desktop, an image in your browser, an Outlook attachment…).
            2. While dragging, tap a key:
                 {convert} – convert to another format
                 {tools} – tools (compress, shrink, trim, merge…)
                 {actions} – actions (copy, move, path, terminal, floating shelf, share)
            3. Drop the file on a slice of the wheel. Drop it in the middle to cancel.

            The wheel stays open until you drop; you can release the key and tap another one to switch wheels.
            Tap the same key again to "repeat the last action".

            Without the mouse: select files in File Explorer (or copy a file or a screenshot) and press {hotkey}.
            Arrows pick a slice, Enter runs it, Tab switches wheels, Esc closes.
            Also via right-click → “Dropit…”.

            The result is saved next to the original file; the original stays untouched.
            """,
            $"""
            ‏1. امسك ملفًا بالفأرة (في مستكشف الملفات، أو على سطح المكتب، أو صورة في متصفحك، أو مرفق في Outlook…).
            2. أثناء السحب، اضغط على مفتاح:
                 {convert} – التحويل إلى صيغة أخرى
                 {tools} – الأدوات (ضغط، تصغير، قصّ، دمج…)
                 {actions} – الإجراءات (نسخ، نقل، مسار، طرفية، لوحة عائمة، مشاركة)
            3. أفلت الملف على إحدى شرائح العجلة. والإفلات في المنتصف يُلغي العملية.

            تبقى العجلة مفتوحة حتى تُفلت الملف؛ ويمكنك ترك المفتاح والضغط على مفتاح آخر لتبديل العجلات.
            واضغط المفتاح نفسه مجددًا لتكرار آخر إجراء.

            بلا فأرة: حدّد ملفات في مستكشف الملفات (أو انسخ ملفًا أو لقطة شاشة) واضغط {hotkey}.
            تختار الأسهم الشريحة، ويشغّلها Enter، ويبدّل Tab العجلات، ويغلق Esc.
            وأيضًا عبر زر الفأرة الأيمن ← «Dropit…».

            يُحفظ الناتج بجوار الملف الأصلي؛ ويبقى الملف الأصلي كما هو دون تغيير.
            """);
        MessageBox.Show(text, T("How Dropit works", "كيف يعمل Dropit"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log(e.Exception);
        e.Handled = true;
        Notifications.Show(T("Dropit – error", "Dropit – خطأ"), e.Exception.Message, error: true);
    }

    public static void Log(Exception ex)
    {
        try
        {
            Directory.CreateDirectory(AppSettings.Folder);
            File.AppendAllText(Path.Combine(AppSettings.Folder, "log.txt"), $"[{DateTime.Now.ToString("s", CultureInfo.InvariantCulture)}] {ex}{Environment.NewLine}");
        }
        catch (IOException)
        {
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkey?.Dispose();
        _input?.Dispose();
        _tray?.Dispose();
        _settings.Save();
        base.OnExit(e);
    }
}
