using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dropit.App.Actions;
using Dropit.App.Jobs;
using Dropit.App.UI;
using Dropit.App.Wheel;
using Dropit.Core;
using Dropit.Core.Operations;

namespace Dropit.App;

/// <summary>
/// <c>Dropit.exe --self-test [folder]</c>: builds every window offscreen and renders previews to PNG.
/// Used by CI to catch UI construction errors on a real Windows machine.
/// </summary>
internal static class SelfTest
{
    public static int Run(string outDir)
    {
        var log = new List<string>();
        try
        {
            Directory.CreateDirectory(outDir);
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            JobCard.Animate = false;
            foreach (var (language, theme, folder) in new[]
                     {
                         (Language.English, AppTheme.DarkGlass, "en"),
                         (Language.English, AppTheme.Light, "en-light"),
                         (Language.English, AppTheme.DeepNight, "en-night"),
(Language.English, AppTheme.Mint, "en-mint"),
                          (Language.English, AppTheme.Warm, "en-warm"),
                          (Language.Arabic, AppTheme.DarkGlass, "ar"),
                         (Language.Arabic, AppTheme.Light, "ar-light"),
                         (Language.Arabic, AppTheme.DeepNight, "ar-night"),
                         (Language.Arabic, AppTheme.Mint, "ar-mint"),
                          (Language.Arabic, AppTheme.Warm, "ar-warm"),
                     })
            {
                Loc.Current = language;
                Theme.Override = theme;
                Theme.Custom = null;
                Theme.Refresh();
                RenderAll(Path.Combine(outDir, folder), log, theme);
            }

            // The same windows, with a theme file the way a user would write one: a real file on disk, read by
            // the real loader, applied to the real palette. This is the only way to see that the file path
            // works and not just the palette behind it.
            RenderCustomTheme(outDir, log);


            log.AddRange(SmokeTest.Run(Path.Combine(outDir, "smoke")));
            // Saved after every step, so a hard crash (which skips the catch below) still shows where it stopped.
            foreach (var step in new Action[] { () => TestSettingsRoundTrip(log), () => TestCaptionClosesWindow(log), () => TestMinimisedWindowsComeBack(log), () => TestThemeSlots(log), () => TestExplorerCom(outDir, log), () => TestDrops(log), () => TestOcr(outDir, log), () => TestUpdateCheck(log) })
            {
                File.WriteAllLines(Path.Combine(outDir, "self-test.txt"), log);
                step();
            }

            File.WriteAllLines(Path.Combine(outDir, "self-test.txt"), log);
            return 0;
        }
        catch (Exception ex)
        {
            log.Add("FAILED: " + ex);
            try
            {
                Directory.CreateDirectory(outDir);
                File.WriteAllLines(Path.Combine(outDir, "self-test.txt"), log);
            }
            catch (IOException)
            {
            }
            return 1;
        }
    }

    private static void RenderAll(string outDir, List<string> log, UI.AppTheme? theme = null)
    {
        Directory.CreateDirectory(outDir);
        string N(string en, string ar) => Loc.Current == Language.Arabic ? ar : en;
        var settings = new AppSettings { Theme = theme };
        var tools = new ToolLocator();
        var core = WheelController.BuildOperations();
        var actions = Dropits.Build(settings, () => { });
        IReadOnlyList<Operation> Source(WheelKind wheel, IReadOnlyList<FileItem> items) =>
            Catalog.For(wheel == WheelKind.Actions ? actions : core, wheel, items, tools);

        // Paths stay latin in every language: that is the harder bidi case, an LTR path inside
        // a right-to-left panel, and the previews are where it gets checked.
        var samples = new (string Name, WheelMode Mode, WheelKind Wheel, string[] Files, int Highlight)[]
        {
            ("wheel-image-convert", WheelMode.Drag, WheelKind.Convert, [@"C:\Photos\vacation.jpg"], 1),
            ("wheel-image-tools", WheelMode.Click, WheelKind.Tools, [@"C:\Photos\vacation.jpg"], 0),
            ("wheel-video-convert", WheelMode.Drag, WheelKind.Convert, [@"C:\Videos\clip.mov"], 0),
            ("wheel-video-tools", WheelMode.Drag, WheelKind.Tools, [@"C:\Videos\clip.mov"], 3),
            ("wheel-pdf-convert", WheelMode.Drag, WheelKind.Convert, [@"C:\Documents\contract.pdf"], 2),
            ("wheel-pdf-tools", WheelMode.Click, WheelKind.Tools, [@"C:\a.pdf", @"C:\b.pdf"], 0),
            ("wheel-sheet-convert", WheelMode.Drag, WheelKind.Convert, [@"C:\Data\sales.xlsx"], -1),
            ("wheel-actions", WheelMode.Drag, WheelKind.Actions, [@"C:\Photos\vacation.jpg"], 4),
            ("wheel-waiting-for-drop", WheelMode.Drag, WheelKind.Convert, [], -1),
        };
        foreach (var s in samples)
        {
            var window = new WheelWindow(s.Mode, s.Wheel, s.Files.Select(f => new FileItem(f)).ToList(), settings, Source, (_, _) => { });
            window.RenderPreview(Path.Combine(outDir, s.Name + ".png"), s.Highlight);
            window.Close();
            log.Add($"ok {Path.GetFileName(outDir)}/{s.Name}");
        }

        SettingsWindow.Operations = () => [.. core, .. actions];
        var settingsWindow = new SettingsWindow(settings, tools, () => { });
        SaveWindow(settingsWindow, Path.Combine(outDir, "settings.png"), 560);
        log.Add($"ok {Path.GetFileName(outDir)}/settings");

        var jobs = new JobsWindow();
        var running = jobs.Add(N("Convert to MP4", "تحويل إلى MP4"), "clip.mov", () => { }, @"C:\x.mov");
        running.Report(0.42);
        jobs.Add(N("Compress", "ضغط"), "vacation.jpg", () => { }, @"C:\x.jpg").Complete([@"C:\Photos\vacation (compressed).jpg"], null);
        jobs.Add(N("Convert to DOCX", "تحويل إلى DOCX"), "contract.pdf", () => { }, @"C:\x.pdf").Fail(N("Pandoc is missing. Install it with: ", "‏‏Pandoc غير مثبّت. ثبّته باستخدام: ") + "winget install JohnMacFarlane.Pandoc");
        SaveWindow(jobs, Path.Combine(outDir, "jobs.png"), 380, desktop: true);
        jobs.Close();
        log.Add($"ok {Path.GetFileName(outDir)}/jobs");

        var home = new HomeWindow(settings, () => { }, _ => { });
        SaveWindow(home, Path.Combine(outDir, "home.png"), 560);
        home.Close();
        log.Add($"ok {Path.GetFileName(outDir)}/home");

        settings.FavoriteOperations = ["image.to.webp", "image.compress"];
        settings.HiddenOperations = ["image.to.tiff"];
        var editor = new WheelEditorWindow(settings, [.. core, .. actions], () => { });
        SaveWindow(editor, Path.Combine(outDir, "wheel-editor.png"), 640);
        log.Add($"ok {Path.GetFileName(outDir)}/wheel-editor");

        // Customised wheel and the "repeat last" view.
        var photo = new List<FileItem> { new(@"C:\Photos\vacation.jpg") };
        IReadOnlyList<Operation> Arranged(WheelKind wheel, IReadOnlyList<FileItem> items) =>
            WheelLayout.Arrange(Source(wheel, items), settings.HiddenOperations, settings.FavoriteOperations);
        var custom = new WheelWindow(WheelMode.Drag, WheelKind.Convert, photo, settings, Arranged, (_, _) => { });
        Check(custom.CurrentWheel == WheelKind.Convert, "custom wheel");
        custom.RenderPreview(Path.Combine(outDir, "wheel-customized.png"), 0);
        custom.Close();
        var webp = core.Single(o => o.Id == "image.to.webp");
        var repeat = new WheelWindow(WheelMode.Drag, WheelKind.Convert, photo, settings, Arranged, (_, _) => { }) { LastOperation = _ => webp };
        repeat.ToggleRepeat();
        repeat.RenderPreview(Path.Combine(outDir, "wheel-repeat.png"), 0);
        repeat.Close();
        log.Add($"ok {Path.GetFileName(outDir)}/wheel-repeat");

        var trim = core.Single(o => o.Id == "video.trim");
        var dialog = new ParameterDialog(trim, [new FileItem(@"C:\Videos\clip.mov")]);
        SaveWindow(dialog, Path.Combine(outDir, "trim-dialog.png"), 360);
        log.Add($"ok {Path.GetFileName(outDir)}/parameter dialog");
    }

    /// <summary>The settings file is written with source-generated JSON (no reflection in the trimmed build).</summary>
    /// <summary>
    /// Renders the windows with a theme file that a user could have written, so the whole path is exercised:
    /// a file on disk, the loader, the palette, and the drawing.
    /// </summary>
    private static void RenderCustomTheme(string outDir, List<string> log)
    {
        var folder = Path.Combine(Path.GetTempPath(), "dropit-selftest-theme");
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, Core.CustomTheme.FileName), """
                {
                  "Warm": {
                    "accent": "#7A2BD1",
                    "accent2": "#4C108C",
                    "page": "#F2EFF7",
                    "card": "#FBFAFD"
                  }
                }
                """);
            var problems = new List<string>();
            var custom = Core.CustomTheme.Load(folder, problems);
            Check(custom is not null, "a theme file the user wrote is read");
            Check(problems.Count == 0, "a valid theme file has no complaints");
            if (custom is null)
                return;

            Loc.Current = Language.English;
            Theme.Override = AppTheme.Warm;
            Theme.Custom = custom;
            Theme.Refresh();
            Check(((System.Windows.Media.SolidColorBrush)Theme.Accent).Color == System.Windows.Media.Color.FromRgb(0x7A, 0x2B, 0xD1),
                "the colour from the theme file is the one the app paints with");

            RenderAll(Path.Combine(outDir, "en-custom"), log, AppTheme.Warm);

            Theme.Custom = null;
            Theme.Refresh();
        }
        finally
        {
            try
            {
                Directory.Delete(folder, true);
            }
            catch (IOException)
            {
            }
        }
    }

    private static void TestSettingsRoundTrip(List<string> log)
    {
        var settings = new AppSettings { Hotkey = HotkeyPreset.CtrlAltW, FavoriteOperations = ["image.to.webp"], Language = Language.Arabic };
        var json = System.Text.Json.JsonSerializer.Serialize(settings, SettingsJson.Default.AppSettings);
        var back = System.Text.Json.JsonSerializer.Deserialize(json, SettingsJson.Default.AppSettings)!;
        Check(back.Hotkey == HotkeyPreset.CtrlAltW && back.FavoriteOperations.Single() == "image.to.webp" && back.Language == Language.Arabic, "settings JSON round trip");
        Check(json.Contains("\"CtrlAltW\""), "enums stored as names");
        log.Add("ok settings json");
    }

    /// <summary>
    /// Dropping the system title bar left the settings window with no way to close it, which is worse
    /// than the plain white caption it replaced. Raises a real MouseLeftButtonUp on the caption's
    /// close button and checks the window actually goes away.
    /// </summary>
private static void TestCaptionClosesWindow(List<string> log)
    {
        // Every window that draws its own caption must have working close buttons. The home window
        // keeps its own icon button, which is exactly how one of them survived without the hit-test
        // opt-out and became a picture.
        Check(Closes<SettingsWindow>(() => new SettingsWindow(new AppSettings(), new ToolLocator(), () => { })),
            "settings window closes from its caption button");
        Check(Closes<HomeWindow>(() => new HomeWindow(new AppSettings(), () => { }, _ => { })),
            "home window closes from its caption button");
        Check(Closes<WheelEditorWindow>(() => new WheelEditorWindow(new AppSettings(), [], () => { })),
            "wheel editor closes from its caption button");
        log.Add("ok caption close");
    }

    /// <summary>
    /// A button that sits in the window header is only clickable if the chrome has no caption strip
    /// competing for the hit test over that area. Raising MouseLeftButtonUp proves nothing about it:
    /// RaiseEvent invokes the handlers directly and skips hit testing altogether.
    /// </summary>
    private static bool Closes<T>(Func<T> make) where T : Window
    {
        var window = make();
        window.Show();
        window.UpdateLayout();

        // A non-zero CaptionHeight makes WindowChrome claim the hit test over that strip for the drag
        // gesture. The buttons sit in the header, so any caption height at all can make them
        // unreachable - this is the check that actually protects them.
        var chrome = System.Windows.Shell.WindowChrome.GetWindowChrome(window);
        if (chrome is null || chrome.CaptionHeight != 0)
        {
            window.Close();
            return false;
        }

        var close = UI.Chrome.FindCloseButton(window);
        if (close is null || close.ActualWidth < 32 || close.ActualHeight < 28)
        {
            window.Close();
            return false;
        }
        // DragMove() opens a modal loop and eats the mouse release, so a press the drag handler does
        // not recognise is a button that can never be clicked. That is what kept these dead even after
        // the caption strip was removed.
        if (!UI.Chrome.IsOnControl(close))
        {
            window.Close();
            return false;
        }
        close.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(
            System.Windows.Input.Mouse.PrimaryDevice, Environment.TickCount, System.Windows.Input.MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonUpEvent,
        });
        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
            () => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        var gone = !window.IsVisible;
        if (!gone)
            window.Close();
        return gone;
    }

    /// <summary>Reading the Explorer selection goes through late-bound COM, which trimming could break.</summary>
    private static void TestExplorerCom(string outDir, List<string> log)
    {
        var file = Path.Combine(outDir, "smoke", "smoke.jpg");
        using (System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{file}\"")) { }
        for (var attempt = 0; attempt < 40; attempt++)
        {
            Thread.Sleep(500);
            var frame = FindWindow("CabinetWClass", null);
            if (frame == IntPtr.Zero)
                continue;
            var selection = Shell.ExplorerSelection.Get(frame);
            if (selection.Any(p => string.Equals(p, file, StringComparison.OrdinalIgnoreCase)))
            {
                log.Add("ok explorer selection");
                PostMessage(frame, 0x0010 /* WM_CLOSE */, IntPtr.Zero, IntPtr.Zero);
                return;
            }
        }
        // Without an interactive desktop Explorer may not open a window; the COM calls above still ran.
        log.Add("skipped explorer selection (no Explorer window appeared)");
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? className, string? windowName);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    /// <summary>Virtual files (as browsers and Outlook drag them) and raw images become real files.</summary>
    private static void TestDrops(List<string> log)
    {
        var descriptor = new byte[4 + 592];
        BitConverter.GetBytes(1).CopyTo(descriptor, 0);
        System.Text.Encoding.Unicode.GetBytes("selftest-virtual.txt").CopyTo(descriptor, 4 + 72);
        var data = new DataObject();
        data.SetData("FileGroupDescriptorW", new MemoryStream(descriptor));
        data.SetData("FileContents", new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hello")));

        var preview = DropContent.Read(data, materialize: false) ?? throw new InvalidOperationException("virtual file not recognised");
        Check(preview.Items.Single().Format == "txt", "virtual file preview has the right type");
        var dropped = DropContent.Read(data, materialize: true) ?? throw new InvalidOperationException("virtual file not saved");
        var path = dropped.Items.Single().Path;
        Check(File.ReadAllText(path) == "hello", "virtual file contents");
        File.Delete(path);
        log.Add("ok drop/virtual-file");

        var bitmap = BitmapSource.Create(4, 4, 96, 96, PixelFormats.Bgra32, null, new byte[4 * 4 * 4], 16);
        var image = new DataObject(DataFormats.Bitmap, bitmap);
        var imageDrop = DropContent.Read(image, materialize: true) ?? throw new InvalidOperationException("image not saved");
        var png = imageDrop.Items.Single().Path;
        Check(File.Exists(png) && new FileInfo(png).Length > 0, "dropped image saved as PNG");
        File.Delete(png);
        log.Add("ok drop/image");

        var link = new DataObject(DataFormats.UnicodeText, "https://example.com/photos/cat.webp");
        var linkDrop = DropContent.Read(link, materialize: false) ?? throw new InvalidOperationException("link not recognised");
        Check(linkDrop.Items.Single().Format == "webp", "link keeps its file type");
        log.Add("ok drop/link");
    }

    /// <summary>Renders a line of text and reads it back with Windows OCR, when the runner has it.</summary>
    private static void TestOcr(string outDir, List<string> log)
    {
        if (!Ocr.OcrOperation.Available)
        {
            log.Add("skipped ocr (no recognizer languages on this machine)");
            return;
        }
        var text = new TextBlock
        {
            Text = "Dropit OCR 12345",
            FontSize = 48,
            FontFamily = new FontFamily("Segoe UI"),
            Foreground = Brushes.Black,
            Background = Brushes.White,
            Padding = new Thickness(30),
        };
        var png = Path.Combine(outDir, "ocr-input.png");
        SaveElement(text, png);
        var engine = Windows.Media.Ocr.OcrEngine.TryCreateFromUserProfileLanguages()
            ?? Windows.Media.Ocr.OcrEngine.TryCreateFromLanguage(Windows.Media.Ocr.OcrEngine.AvailableRecognizerLanguages[0]);
        var result = Task.Run(() => Ocr.OcrOperation.RecognizeAsync(engine, png)).GetAwaiter().GetResult();
        Check(result.Contains("12345"), $"OCR read \"{result}\"");
        log.Add($"ok ocr: {result}");
    }

    /// <summary>
    /// A theme file can only change the colours it names, so the names and the palette have to agree: every
    /// colour the app paints needs a slot to reach it, and every slot needs a colour behind it. A colour
    /// added to the palette without a slot would be unreachable for the user, and a slot without a colour
    /// would silently do nothing.
    /// </summary>
    private static void TestThemeSlots(List<string> log)
    {
        var colours = typeof(UI.Theme.Palette)
            .GetProperties()
            .Where(p => p.PropertyType == typeof(System.Windows.Media.Color))
            .Select(p => char.ToLowerInvariant(p.Name[0]) + p.Name[1..])
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();
        var slots = Core.CustomTheme.Tokens.OrderBy(n => n, StringComparer.Ordinal).ToList();

        Check(colours.SequenceEqual(slots, StringComparer.Ordinal),
            "every colour of the palette has a theme file slot, and every slot has a colour");

        // Each slot has to change the palette, not just exist.
        var palette = UI.Theme.PaletteFor(UI.AppTheme.Warm);
        foreach (var slot in Core.CustomTheme.Tokens)
        {
            var changed = UI.Theme.PaletteFor(UI.AppTheme.Warm, new Dictionary<string, string> { [slot] = "#FF00FF" });
            Check(!changed.Equals(palette), $"theme slot '{slot}' changes the palette");
        }

        // A file with one colour changes that colour and leaves the rest alone.
        var one = UI.Theme.PaletteFor(UI.AppTheme.Warm, new Dictionary<string, string> { ["accent"] = "#123456" });
        Check(one.Accent == System.Windows.Media.Color.FromRgb(0x12, 0x34, 0x56) && one.Page == palette.Page,
            "a theme file changes only the colours it names");

        // Every theme the app offers has to be nameable in a theme file, and no others.
        var themes = Enum.GetNames<UI.AppTheme>()
            .Where(n => n != nameof(UI.AppTheme.System))
            .OrderBy(n => n, StringComparer.Ordinal);
        var names = Core.CustomTheme.ThemeNames.OrderBy(n => n, StringComparer.Ordinal);
        Check(themes.SequenceEqual(names, StringComparer.Ordinal),
            "every theme of the app can be named in a theme file, and the file names no other");
        log.Add("ok theme slots");
    }

    /// <summary>
    /// The settings, home and wheel editor windows are opened from the tray. When one of them was minimised,
    /// opening it again has to bring it back: Activate() on a minimised window does nothing at all, and the
    /// user is left with nothing on screen. This check exists because a version of that fix was commented out
    /// by accident - it still compiled, still passed every build, and the window stayed reduced.
    /// </summary>
    private static void TestMinimisedWindowsComeBack(List<string> log)
    {
        Check(Restores(w => UI.SettingsWindow.BringToFront(w)),
            "the settings window comes back when it was minimised");
        Check(Restores(w => UI.HomeWindow.BringToFront(w)),
            "the home window comes back when it was minimised");
        Check(Restores(w => UI.WheelEditorWindow.BringToFront(w)),
            "the wheel editor comes back when it was minimised");
        log.Add("ok minimised windows");
    }

    private static bool Restores(Action<Window> bringToFront)
    {
        var window = new Window { Width = 200, Height = 100, WindowStyle = WindowStyle.None, ShowInTaskbar = false, Left = -5000 };
        try
        {
            window.Show();
            window.WindowState = WindowState.Minimized;
            if (window.WindowState != WindowState.Minimized)
                return false;
            bringToFront(window);
            return window.WindowState == WindowState.Normal;
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Asks the real GitHub API, as the app does; logged only (the network may be unavailable).</summary>
    private static void TestUpdateCheck(List<string> log)
    {
        try
        {
            using var http = new System.Net.Http.HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Dropit-selftest");
            var json = http.GetStringAsync(UpdateInfo.LatestReleaseApi).GetAwaiter().GetResult();
            var latest = UpdateInfo.Parse(json);
            log.Add(latest is null
                ? "update check: no usable release"
                : $"update check: latest {latest.Tag}, installer {(latest.InstallerUrl is null ? "missing" : "found")}, sha256 {(latest.InstallerSha256 is null ? "missing" : "present")}");
        }
        catch (Exception ex)
        {
            log.Add("update check skipped: " + ex.Message);
        }
    }

    private static void Check(bool condition, string what)
    {
        if (!condition)
            throw new InvalidOperationException("Self-test check failed: " + what);
    }

    private static void SaveWindow(Window window, string file, double width, bool desktop = false)
    {
        var content = (UIElement)window.Content;
        window.Content = null;
        var root = new Border
        {
            Width = width,
            Background = desktop ? Desktop() : window.Background ?? Brushes.White,
            Child = content,
        };
        if (content is ScrollViewer scroll)
            scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        SaveElement(root, file);
    }

    /// <summary>A wallpaper-like backdrop for floating surfaces in previews.</summary>
    public static Brush Desktop() => Theme.IsDark
        ? new LinearGradientBrush(Color.FromRgb(0x1F, 0x2A, 0x44), Color.FromRgb(0x3A, 0x2A, 0x4A), 45)
        : new LinearGradientBrush(Color.FromRgb(0xC9, 0xD8, 0xEE), Color.FromRgb(0xE9, 0xD9, 0xEE), 45);

    public static void SaveElement(FrameworkElement element, string file)
    {
        element.Measure(new Size(element.Width > 0 ? element.Width : 800, double.PositiveInfinity));
        var size = element.DesiredSize;
        element.Arrange(new Rect(size));
        element.UpdateLayout();
        const double scale = 1.5;
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(size.Width * scale), (int)Math.Ceiling(size.Height * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(file);
        encoder.Save(stream);
    }
}
