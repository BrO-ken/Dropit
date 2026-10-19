using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Dropit.App.Input;
using Dropit.App.Shell;
using Dropit.Core;
using static Dropit.App.UI.Controls;
using static Dropit.Core.Loc;

namespace Dropit.App.UI;

/// <summary>Settings in the style of the Windows 11 Settings app: sections of rounded cards.</summary>
internal sealed class SettingsWindow : Window
{
    private static SettingsWindow? _open;

    /// <summary>Set by the app: the update checker and the operations the wheel editor lists.</summary>
    public static Shell.Updater? Updater { get; set; }

    public static Func<IReadOnlyList<Operation>>? Operations { get; set; }

    /// <summary>Downloads FFmpeg or Pandoc in the app (set by the app).</summary>
    public static Func<ExternalTool, Task<bool>>? DownloadTool { get; set; }
    private readonly AppSettings _settings;
    private readonly ToolLocator _tools;
    private readonly Action _changed;
    private readonly ScrollViewer _scroll = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

    internal SettingsWindow(AppSettings settings, ToolLocator tools, Action changed)
    {
        _settings = settings;
        _tools = tools;
        _changed = changed;
        Direction.Apply(this);
        Theme.Refresh();
        Title = T("Dropit – settings", "Dropit – الإعدادات");
        MinWidth = 460;
        Chrome.Apply(this);
        Chrome.FitToWorkArea(this, 680, 900);
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Icon = new BitmapImage(new Uri("pack://application:,,,/Assets/Dropit.ico"));
        Background = Theme.Page;
        FontFamily = Theme.UiFont;
        Content = _scroll;
        Build();
        Action refresh = () => Dispatcher.BeginInvoke(Rebuild);
        if (Updater is not null)
            Updater.Changed += refresh;
        Closed += (_, _) =>
        {
            _open = null;
            if (Updater is not null)
                Updater.Changed -= refresh;
        };
    }

    public static void ShowSingle(AppSettings settings, ToolLocator tools, Action changed)
    {
        if (_open is not null)
        {
            BringToFront(_open);
            return;
        }
        _open = new SettingsWindow(settings, tools, changed);
        _open.Show();
        _open.Activate();
    }

    /// <summary>Redraws the open settings window after the app changed a setting itself.</summary>
    public static void RefreshIfOpen() => _open?.Rebuild();

    /// <summary>
    /// Brings the window forward. Activate() on a minimised window does nothing visible: the window stays in
    /// the taskbar and nothing appears, so it has to be restored first. The self-test calls this directly,
    /// because a version of this that was commented out by accident still compiled and still passed every
    /// build, and the window stayed reduced.
    /// </summary>
    internal static void BringToFront(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;
        window.Show();
        window.Activate();
    }

    private void Save()
    {
        _settings.Save();
        _changed();
    }

    /// <summary>Rebuilds every card (used when one change affects others), keeping the scroll position.</summary>
    private void Rebuild()
    {
        var offset = _scroll.VerticalOffset;
        Build();
        _scroll.UpdateLayout();
        _scroll.ScrollToVerticalOffset(offset);
    }

    private void Build()
    {
        var root = new StackPanel { Margin = new Thickness(32, 8, 32, 32), MaxWidth = 720 };
        root.Children.Add(Header());

        root.Children.Add(SectionHeader(T("Language", "اللغة")));
        root.Children.Add(Card(T("App language", "لغة التطبيق"), null,
            Segmented<Core.Language?>(
                [(T("Windows", "لغة Windows"), null),
                 (Core.Loc.NativeName(Core.Language.Arabic), Core.Language.Arabic),
                 (Core.Loc.NativeName(Core.Language.English), Core.Language.English)],
                _settings.Language,
                value =>
                {
                    _settings.Language = value;
                    Save();
                    // Rebuild the window in the new language.
                    Title = T("Dropit – settings", "Dropit – الإعدادات");
                    Rebuild();
                }),
            glyph: "\uE774"));

        root.Children.Add(SectionHeader(T("Gesture", "الإيماءة")));
        root.Children.Add(Card(
            T("Grab a file and tap a key while dragging", "امسك ملفًا واضغط مفتاحًا أثناء السحب"),
            T("A wheel opens; drop the file on a slice. Drop in the middle to cancel.", "تُفتح عجلة؛ أفلت الملف على إحدى الشرائح. والإفلات في المنتصف يُلغي العملية."),
            null, glyph: Glyphs.Extract));
        foreach (var (wheel, description) in new[]
                 {
                     (WheelKind.Convert, T("Convert to another format", "التحويل إلى صيغة أخرى")),
                     (WheelKind.Tools, T("Compress, shrink, trim, merge…", "ضغط، تصغير، قصّ، دمج…")),
                     (WheelKind.Actions, T("Copy, move, path, terminal, share…", "نسخ، نقل، مسار، طرفية، مشاركة…")),
                 })
        {
            root.Children.Add(Card(AppSettings.WheelName(wheel), description, KeyChooser(wheel)));
        }
        root.Children.Add(Card(T("Customize wheels", "تخصيص العجلات"),
            T("Favourite slices first, hide the ones you never use. Tap the same key again (or press R) to repeat the last action.", "ضع الشرائح المفضّلة في الأعلى، وأخفِ التي لا تستخدمها. اضغط المفتاح نفسه مجددًا (أو اضغط R) لتكرار آخر إجراء."),
            Button(T("Customize…", "تخصيص…"), () =>
            {
                if (Operations is not null)
                    WheelEditorWindow.ShowSingle(_settings, Operations(), _changed);
            }, glyph: "\uE70F"),
            glyph: "\uE8EE"));
        root.Children.Add(Card(T("Pause gestures", "إيقاف الإيماءات مؤقتًا"), T("The wheel won’t open while dragging.", "لن تُفتح العجلة أثناء السحب."),
            Toggle(_settings.Paused, v =>
            {
                _settings.Paused = v;
                Save();
            })));

        root.Children.Add(SectionHeader(T("Keyboard", "لوحة المفاتيح")));
        root.Children.Add(Card(T("Shortcut for the File Explorer selection", "اختصار التحديد في مستكشف الملفات"),
            T("Arrows pick a slice, Enter runs it, Tab switches wheels, Esc closes.", "تختار الأسهم الشريحة، ويشغّلها Enter، ويبدّل Tab العجلات، ويغلق Esc."),
            null,
            below: Wrap(Segmented(
                Enum.GetValues<HotkeyPreset>().Where(p => p != HotkeyPreset.None).Append(HotkeyPreset.None)
                    .Select(p => (AppSettings.HotkeyName(p), p)).ToList(),
                _settings.Hotkey,
                value =>
                {
                    _settings.Hotkey = value;
                    Save();
                })),
            glyph: "\uE765"));

        root.Children.Add(SectionHeader(T("Results", "النتائج")));
        var folderLine = Text(_settings.OutputDirectory ?? "", 12, brush: Theme.Muted, wrap: false);
        var folderRow = Controls.Row();
        folderRow.Margin = new Thickness(0, 12, 0, 0);
        folderRow.Children.Add(Button(T("Choose folder…", "اختر مجلدًا…"), PickFolder, glyph: Glyphs.Folder));
        folderLine.Margin = new Thickness(12, 0, 0, 0);
        folderLine.VerticalAlignment = VerticalAlignment.Center;
        folderRow.Children.Add(folderLine);
        var toFolder = !string.IsNullOrWhiteSpace(_settings.OutputDirectory);
        var below = new StackPanel();
        below.Children.Add(Wrap(Segmented(
            [(T("Next to the original", "بجانب الملف الأصلي"), false), (T("To a folder", "إلى مجلد"), true)],
            toFolder,
            value =>
            {
                if (value)
                {
                    PickFolder();
                }
                else
                {
                    _settings.OutputDirectory = null;
                    Save();
                    Rebuild();
                }
            })));
        if (toFolder)
            below.Children.Add(folderRow);
        root.Children.Add(Card(T("Save results", "حفظ النتائج"), T("The original file is never changed.", "لا يُعدَّل الملف الأصلي أبدًا."), null, below, Glyphs.Folder));
        root.Children.Add(Card(T("Show in folder when done", "الإظهار في المجلد عند الانتهاء"), null,
            Toggle(_settings.RevealResult, v =>
            {
                _settings.RevealResult = v;
                Save();
            })));

        root.Children.Add(SectionHeader(T("Quality", "الجودة")));
        root.Children.Add(Card(T("JPG, WebP and AVIF", "JPG وWebP وAVIF"), T("Quality when converting images", "الجودة عند تحويل الصور"),
            Slider(50, 100, _settings.Quality.JpegQuality, v => _settings.Quality.JpegQuality = v, Save), glyph: Glyphs.Photo));
        root.Children.Add(Card(T("Compress", "ضغط"), T("Quality of the Compress tool", "جودة أداة الضغط"),
            Slider(30, 95, _settings.Quality.CompressQuality, v => _settings.Quality.CompressQuality = v, Save), glyph: Glyphs.Shrink));
        root.Children.Add(Card(T("PDF to images", "PDF إلى صور"), T("Resolution in DPI", "الدقة بوحدة DPI"),
            Slider(72, 600, _settings.Quality.PdfDpi, v => _settings.Quality.PdfDpi = v, Save), glyph: Glyphs.Page));

        root.Children.Add(SectionHeader(T("Appearance", "المظهر")));
        root.Children.Add(Card(T("Theme", "السمة"), null,
            Segmented<AppTheme?>(
                [
                    (T("Windows", "Windows"), null),
                    (T("Dark glass", "زجاج داكن"), AppTheme.DarkGlass),
                    (T("Deep night", "ليل عميق"), AppTheme.DeepNight),
                    (T("Mint", "نعناعي"), AppTheme.Mint),
                    (T("Light", "فاتح"), AppTheme.Light),
                    (T("Warm", "دافئ"), AppTheme.Warm),
                ],
                _settings.Theme,
                value =>
                {
                    _settings.Theme = value;
                    Theme.Override = value;
                    Theme.Refresh();
                    Save();
                    Rebuild();
                }),
            glyph: "\uE790"));

        root.Children.Add(Card(T("Softness", "النعومة"),
            T("Rounds every corner by the same amount.", "يجعل كل الزوايا مستديرة بالقدر نفسه."),
            Slider(0, 24, _settings.CornerRadius, v => _settings.CornerRadius = v, () =>
            {
                Theme.SetRadius(_settings.CornerRadius);
                Save();
                Rebuild();
            })));

        root.Children.Add(Card(T("Less movement", "حركة أقل"),
            T("Skips the animations. The app works the same.", "يتخطى الحركات. يعمل التطبيق بالطريقة نفسها."),
            Toggle(_settings.ReduceMotion, v =>
            {
                _settings.ReduceMotion = v;
                Theme.ReduceMotion = v;
                Save();
            })));

        root.Children.Add(Card(T("Your own colours", "ألوانك الخاصة"),
            CustomThemeDescription(),
            Button(T("Open the folder", "افتح المجلد"), () => OpenThemeFolder(), glyph: "\uE838")));

        root.Children.Add(SectionHeader(T("System", "النظام")));
        root.Children.Add(Card(T("Start when I sign in", "التشغيل عند تسجيل الدخول"), null,
            Toggle(ShellIntegration.IsAutostartEnabled, v => Apply(() => ShellIntegration.SetAutostart(v))), glyph: "\uE7E8"));
        root.Children.Add(Card(T("File Explorer context menu", "قائمة سياق مستكشف الملفات"), T("A “Dropit…” item on right-click", "عنصر «Dropit…» في زر الفأرة الأيمن"),
            Toggle(ShellIntegration.IsContextMenuEnabled, v => Apply(() => ShellIntegration.SetContextMenu(v))), glyph: "\uE8A7"));

        root.Children.Add(SectionHeader(T("Updates", "التحديثات")));
        root.Children.Add(UpdateCard());
        root.Children.Add(Card(T("Check for updates automatically", "التحقق من التحديثات تلقائيًا"),
            T("Now and then asks GitHub for the latest release. Nothing about your files is sent.", "يسأل GitHub بين الحين والآخر عن أحدث إصدار. ولا يُرسل أي شيء عن ملفاتك."),
            Toggle(_settings.CheckForUpdates, v =>
            {
                _settings.CheckForUpdates = v;
                Save();
            })));

        root.Children.Add(SectionHeader(T("Helper programs", "البرامج المساعدة")));
        root.Children.Add(Card(T("Built in", "مدمج"),
            T("Images, PDFs, spreadsheets, subtitles and archives. Audio and video need FFmpeg, documents need Pandoc and Office files need LibreOffice.", "الصور وملفات PDF وجداول البيانات والترجمات والأرشيفات. أما الصوت والفيديو فيحتاجان FFmpeg، والمستندات تحتاج Pandoc، وملفات Office تحتاج LibreOffice."),
            Button(T("Check again", "تحقّق مجددًا"), () =>
            {
                _tools.Refresh();
                Rebuild();
            }, glyph: "\uE72C"),
            glyph: "\uE90F"));
        foreach (var tool in Enum.GetValues<ExternalTool>())
            root.Children.Add(ToolCard(tool));

        var version = typeof(SettingsWindow).Assembly.GetName().Version;
        var footer = Text($"Dropit {version?.ToString(3)} · " + T("everything runs locally, files never leave your PC.", "كل شيء يعمل محليًا، ولا تغادر ملفاتك جهازك."), 12, brush: Theme.Muted);
        footer.Margin = new Thickness(2, 24, 0, 0);
        root.Children.Add(footer);

        _scroll.Content = root;
    }

    private static FrameworkElement Wrap(FrameworkElement element)
    {
        element.HorizontalAlignment = Loc.IsRtl ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        element.Margin = new Thickness(0, 12, 0, 0);
        return element;
    }

    private UIElement Header()
    {
        var panel = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
        // Added first so they dock to the trailing edge ahead of the logo.
        foreach (var b in Chrome.CaptionButtons(this))
            panel.Children.Add(b);
        var image = new Image
        {
            Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Dropit.png")),
            Width = 56,
            Height = 56,
            Margin = new Thickness(0, 0, 18, 0),
        };
        panel.Children.Add(image);
        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var title = Text("Dropit", 28, FontWeights.SemiBold);
        title.FontFamily = Theme.DisplayFont;
        text.Children.Add(title);
        text.Children.Add(Text(T("Grab a file, tap a key, drop it on a format.", "امسك ملفًا، واضغط مفتاحًا، وأفلته على صيغة."), 13.5, brush: Theme.Muted));
        panel.Children.Add(text);
        Chrome.MakeDraggable(this, panel);
        return panel;
    }

    private Border KeyChooser(WheelKind wheel) => Segmented(
        [("Shift", ModifierKey.Shift), ("Ctrl", ModifierKey.Ctrl), ("Alt", ModifierKey.Alt), (T("Off", "معطّل"), ModifierKey.None)],
        _settings.KeyFor(wheel),
        key =>
        {
            var old = _settings.KeyFor(wheel);
            // Keep keys unique: the wheel that had this key gets the old one.
            foreach (var other in new[] { WheelKind.Convert, WheelKind.Tools, WheelKind.Actions })
            {
                if (other != wheel && key != ModifierKey.None && _settings.KeyFor(other) == key)
                    SetKey(other, old);
            }
            SetKey(wheel, key);
            Save();
            Rebuild();
        });

    private void SetKey(WheelKind wheel, ModifierKey key)
    {
        switch (wheel)
        {
            case WheelKind.Convert:
                _settings.ConvertKey = key;
                break;
            case WheelKind.Tools:
                _settings.ToolsKey = key;
                break;
            default:
                _settings.ActionsKey = key;
                break;
        }
    }

    private void PickFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = T("Output folder", "مجلد الإخراج") };
        if (dialog.ShowDialog(this) == true)
        {
            _settings.OutputDirectory = dialog.FolderName;
            Save();
        }
        Rebuild();
    }

    private void Apply(Action action)
    {
        try
        {
            action();
            Save();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Dropit", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>
    /// What the theme file is doing, in one line: nothing, applied, or ignored with the reason why. It
    /// describes the theme that is in use, so it stays true even after the file on disk has changed.
    /// </summary>
    private string CustomThemeDescription()
    {
        var theme = Theme.Custom;
        if (theme is null)
        {
            var path = Path.Combine(AppSettings.Folder, Core.CustomTheme.FileName);
            if (!File.Exists(path))
                return T("No theme file. Put a theme.json in this folder to change the colours; anything you leave out keeps the built-in one.",
                         "لا يوجد ملف سمة. ضع ملف theme.json في هذا المجلد لتغيير الألوان، وما تتركه يبقى كما هو.");
            var problems = Theme.CustomProblems;
            return problems.Count > 0
                ? T("The theme file was ignored: ", "تم تجاهل ملف السمة: ") + string.Join(" ", problems)
                : T("The theme file was ignored.", "تم تجاهل ملف السمة.");
        }

        var count = theme.Themes.Values.Sum(v => v.Count);
        var names = string.Join(", ", theme.Themes.Keys);
        return T($"A theme file is in use: {count} colour(s) in {names}.", $"ملف السمة مستخدم: {count} لون في {names}.");
    }

    /// <summary>Opens the folder that holds settings.json and theme.json.</summary>
    private void OpenThemeFolder()
    {
        try
        {
            Directory.CreateDirectory(AppSettings.Folder);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(AppSettings.Folder) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
        }
    }

    private Border UpdateCard()
    {
        var updater = Updater;
        var current = Shell.Updater.Current;
        string status;
        UIElement? action = null;
        if (updater is null)
        {
            status = "";
        }
        else if (updater.Step == Shell.UpdateStep.Downloading)
        {
            status = T("Downloading the installer…", "جارٍ تنزيل المثبِّت…");
        }
        else if (updater.Step == Shell.UpdateStep.Installing)
        {
            status = T("Installing…", "جارٍ التثبيت…");
        }
        else if (updater.Available is { } update)
        {
            status = T($"Version {update.Version} is available.", $"الإصدار {update.Version} متاح.");
            action = Button(Shell.Updater.IsInstalled ? T("Update", "تحديث") : T("Download", "تنزيل"),
                () => UI.UpdateNoticeWindow.Show(updater, update), primary: true, glyph: "\uE896");
        }
        else
        {
            status = updater.LastError is not null ? T("Check failed: ", "فشل التحقق: ") + updater.LastError
                : updater.LastChecked is { } at ? T($"You're up to date (checked {at:HH:mm}).", $"أنت تستخدم أحدث إصدار (تم التحقق في {at:HH:mm}).")
                : T("Not checked yet.", "لم يتم التحقق بعد.");
            action = Button(T("Check now", "تحقّق الآن"), async () =>
            {
                await updater.CheckAsync();
                Rebuild();
            }, glyph: "\uE72C");
        }
        return Card($"Dropit {current}", status, action, glyph: "\uE777");
    }

    private Border ToolCard(ExternalTool tool)
    {
        var path = _tools.Find(tool);
        var description = path ?? tool switch
        {
            ExternalTool.Ffmpeg => T("Audio and video", "الصوت والفيديو"),
            ExternalTool.Pandoc => T("Documents (DOCX, Markdown, EPUB…)", "المستندات (DOCX، Markdown، EPUB…)"),
            _ => T("DOC, PPTX, ODS and conversion to PDF", "DOC وPPTX وODS والتحويل إلى PDF"),
        };
        var right = new StackPanel { Orientation = Orientation.Horizontal };
        right.Children.Add(Badge(path is null ? T("missing", "مفقود") : T("found", "موجود"), path is not null));
        if (path is null)
        {
            var id = tool switch
            {
                ExternalTool.Ffmpeg => "Gyan.FFmpeg",
                ExternalTool.Pandoc => "JohnMacFarlane.Pandoc",
                _ => "TheDocumentFoundation.LibreOffice",
            };
            var install = Shell.ToolInstaller.CanInstall(tool) && DownloadTool is not null
                ? Button(T($"Download ({Shell.ToolInstaller.DownloadSize(tool)})", $"تنزيل ({Shell.ToolInstaller.DownloadSize(tool)})"), async () =>
                {
                    if (await DownloadTool(tool))
                        Rebuild();
                }, primary: true, glyph: "\uE896")
                : Button(T("Install", "تثبيت"), () =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo("cmd.exe",
                        $"/c winget install -e --id {id} --accept-source-agreements --accept-package-agreements & echo. & echo {T("Done. Click Check again in Dropit.", "تم. اضغط «تحقّق مجددًا» في Dropit.")} & pause")
                    { UseShellExecute = true });
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Dropit", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }, primary: true, glyph: "\uE896");
            install.Margin = new Thickness(10, 0, 0, 0);
            right.Children.Add(install);
        }
        return Card(ToolLocator.DisplayName(tool), description, right);
    }
}
