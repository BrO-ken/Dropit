using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Dropit.App.Shell;
using Dropit.Core;
using static Dropit.Core.Loc;

namespace Dropit.App.UI;

/// <summary>
/// What you get when you double-click Dropit.exe: a real window with the three wheels, a drop target and
/// a close button. The tray app itself used to answer a bare launch with a balloon tip, which is not
/// something you can move, resize or dismiss, and it gave no way to start a conversion without knowing
/// the gesture already.
/// </summary>
internal sealed class HomeWindow : Window
{
    private static HomeWindow? _open;

    public static void ShowSingle(AppSettings settings, Action openSettings, Action<IReadOnlyList<string>?> showWheel)
    {
        if (_open is not null)
        {
            // Activate() on a minimised window does nothing visible: the window stays in the             // taskbar and nothing appears. Restore it before bringing it forward.             if (_open.WindowState == WindowState.Minimized)                 _open.WindowState = WindowState.Normal;             _open.Show();             _open.Activate();
            return;
        }
        _open = new HomeWindow(settings, openSettings, showWheel);
        _open.Show();
        _open.Activate();
    }

    internal HomeWindow(AppSettings settings, Action openSettings, Action<IReadOnlyList<string>?> showWheel)
    {
        Direction.Apply(this);
        Theme.Refresh();

        Title = "Dropit";
        MinWidth = 400;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Chrome.Apply(this);
        Chrome.FitToWorkArea(this, 560, 640);
        FontFamily = Theme.UiFont;

        // Dragging the window: every chrome-less area of the shell has to move it, otherwise the user
        // grabs it and nothing happens.
        var root = new Grid { Background = Theme.Page };
        root.Children.Add(new Border { Background = Theme.Ambient, IsHitTestVisible = false });
        root.Children.Add(ContentGrid(settings, openSettings, showWheel));
        Content = new Border
        {
            CornerRadius = new CornerRadius(10),
            Background = Brushes.Transparent,
            ClipToBounds = true,
            Child = root,
        };
        root.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                TryDrag(e);
        };
        Closed += (_, _) => _open = null;
    }

    /// <summary>Starts a window drag, unless the press landed on something interactive.</summary>
    private void TryDrag(MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject o && FindAncestor<Button>(o) is not null)
            return;
        if (e.OriginalSource is DependencyObject o2 && FindAncestor<ScrollBar>(o2) is not null)
            return;
        DragMove();
    }

    private static T? FindAncestor<T>(DependencyObject start) where T : DependencyObject
    {
        for (var o = start; o is not null; o = VisualTreeHelper.GetParent(o))
        {
            if (o is T t)
                return t;
        }
        return null;
    }

    private UIElement ContentGrid(AppSettings settings, Action openSettings, Action<IReadOnlyList<string>?> showWheel)
    {
        var column = new Grid { Margin = new Thickness(28, 6, 28, 22) };
        column.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // header
        column.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // drop zone
        column.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // wheels
        column.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        column.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // footer

        column.Children.Add(Header(openSettings));
        Grid.SetRow(column.Children[^1], 0);

        var drop = DropZone(showWheel);
        Grid.SetRow(drop, 1);
        column.Children.Add(drop);

        var wheels = new StackPanel { Margin = new Thickness(0, 18, 0, 0) };
        foreach (var kind in new[] { WheelKind.Convert, WheelKind.Tools, WheelKind.Actions })
        {
            var key = settings.KeyFor(kind);
            if (key == Input.ModifierKey.None)
                continue;
            wheels.Children.Add(WheelRow(kind, key, showWheel));
        }
        Grid.SetRow(wheels, 2);
        column.Children.Add(wheels);

        var footer = Controls.Text(
            T("Everything runs locally — your files never leave this PC.", "كل شيء يعمل محليًا — ملفاتك لا تغادر جهازك."),
            11.5, brush: Theme.Muted, wrap: false);
        footer.Margin = new Thickness(2, 10, 0, 0);
        Grid.SetRow(footer, 4);
        column.Children.Add(footer);

        return column;
    }

    private UIElement Header(Action openSettings)
    {
        var row = new DockPanel { Margin = new Thickness(0, 0, 0, 16) };
        var settings = Chrome.IconButton(Glyphs.Settings, T("Settings", "الإعدادات"), () =>
        {
            Hide();
            openSettings();
        });
        var close = Chrome.IconButton(Glyphs.Close, T("Close", "إغلاق"), Close, "CaptionClose", destructive: true);
        DockPanel.SetDock(close, Loc.IsRtl ? Dock.Left : Dock.Right);
        DockPanel.SetDock(settings, Loc.IsRtl ? Dock.Left : Dock.Right);
        row.Children.Add(close);
        row.Children.Add(settings);

        var title = new TextBlock
        {
            Text = "Dropit",
            FontFamily = Theme.DisplayFont,
            FontSize = 26,
            FontWeight = FontWeights.SemiBold,
            Foreground = Theme.Text,
            VerticalAlignment = VerticalAlignment.Center,
        };
var version = Controls.Text($"v{typeof(HomeWindow).Assembly.GetName().Version?.ToString(3)}", 11.5, brush: Theme.Muted, wrap: false);
        // Row reverses in Arabic, so the gap has to move to the other side of the version too.
        version.Margin = Loc.IsRtl ? new Thickness(0, 0, 10, 0) : new Thickness(10, 0, 0, 0);
        version.VerticalAlignment = VerticalAlignment.Center;

        var names = Controls.Row(title, version);
        names.VerticalAlignment = VerticalAlignment.Center;
        // Shrink to content and sit on the reading edge; a stretched horizontal stack would keep its
        // children on the left whatever the language.
        names.HorizontalAlignment = Loc.IsRtl ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        row.Children.Add(names);
        Chrome.MakeDraggable(this, row);
        return row;
    }

    /// <summary>
    /// The drop target doubles as the explanation of the gesture, so the window teaches the shortcut it
    /// replaces instead of just accepting a drop.
    /// </summary>
    private UIElement DropZone(Action<IReadOnlyList<string>?> showWheel)
    {
        var glyph = new TextBlock
        {
            Text = Glyphs.Extract,
            FontFamily = Theme.Icons,
            FontSize = 34,
            Foreground = Theme.Accent,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        var headline = Controls.Text(T("Drop a file here", "أفلت ملفًا هنا"), 17, FontWeights.SemiBold);
        headline.HorizontalAlignment = HorizontalAlignment.Center;
        headline.Margin = new Thickness(0, 12, 0, 0);
        var hint = Controls.Text(
            T("It opens the wheel for that file type. No mouse needed.", "ستفتح عجلة مناسبة لنوع الملف. لا حاجة للفأرة."),
            12, brush: Theme.Muted);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        hint.TextAlignment = TextAlignment.Center;
        hint.Margin = new Thickness(0, 6, 0, 0);

        var stack = new StackPanel
        {
            Margin = new Thickness(0, 24, 0, 0),
            MinHeight = 168,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        stack.Children.Add(glyph);
        stack.Children.Add(headline);
        stack.Children.Add(hint);

        var border = new Border
        {
            Background = Theme.Card,
            BorderBrush = Theme.CardBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            AllowDrop = true,
            Child = stack,
            Effect = new DropShadowEffect { BlurRadius = 28, ShadowDepth = 4, Direction = 270, Opacity = 0.35, Color = Colors.Black },
        };
        border.AllowDrop = true;
        border.DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effects = DragDropEffects.Copy;
                border.BorderBrush = Theme.Accent;
            }
        };
        border.DragLeave += (_, _) => border.BorderBrush = Theme.CardBorder;
        border.Drop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } paths)
            {
                Hide();
                showWheel(paths);
            }
        };
        border.MouseLeftButtonUp += (_, _) =>
        {
            Hide();
            showWheel(null);
        };

        return border;
    }

    private UIElement WheelRow(WheelKind kind, Input.ModifierKey key, Action<IReadOnlyList<string>?> showWheel)
    {
        var cap = new TextBlock
        {
            Text = AppSettings.KeyName(key),
            FontFamily = Theme.DisplayFont,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = Theme.TextOnAccent,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 54,
        };
        var pill = new Border
        {
            Background = Theme.AccentGradient,
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(10, 4, 10, 5),
            Child = cap,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var name = Controls.Text(AppSettings.WheelName(kind), 13.5, FontWeights.SemiBold);
        name.Margin = Loc.IsRtl ? new Thickness(0, 0, 12, 0) : new Thickness(12, 0, 0, 0);
        var what = Controls.Text(kind switch
        {
            WheelKind.Convert => T("JPG, PNG, MP4, DOCX, PDF and more", "JPG وPNG وMP4 وDOCX وPDF وغيرها"),
            WheelKind.Tools => T("Compress, resize, trim, merge, extract text", "ضغط، تغيير الحجم، قصّ، دمج، استخراج نص"),
            _ => T("Copy, move, rename, share, terminal", "نسخ، نقل، إعادة تسمية، مشاركة، طرفية"),
        }, 11.5, brush: Theme.Muted, wrap: false);
        what.Margin = Loc.IsRtl ? new Thickness(0, 0, 12, 0) : new Thickness(12, 0, 0, 0);

        var row = new DockPanel { Margin = new Thickness(0, 6, 0, 0), Cursor = Cursors.Hand };
        DockPanel.SetDock(pill, Loc.IsRtl ? Dock.Right : Dock.Left);
        row.Children.Add(pill);
        var texts = Controls.Row(name, what);
        texts.VerticalAlignment = VerticalAlignment.Center;
        texts.HorizontalAlignment = Loc.IsRtl ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        row.Children.Add(texts);

        var border = new Border
        {
            Background = Theme.Panel,
            BorderBrush = Theme.PanelBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 10, 14, 10),
            Child = row,
        };
        border.MouseLeftButtonUp += (_, _) =>
        {
            Hide();
            showWheel(null);
        };
        return border;
    }
}
