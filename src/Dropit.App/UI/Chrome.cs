using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using Dropit.Core;
using static Dropit.Core.Loc;
using WVisual = System.Windows.Media.Visual;

namespace Dropit.App.UI;

/// <summary>
/// Drops the system title bar while keeping native dragging, snapping and resizing, so a dark window
/// does not end up with a light caption bar above it. Because the caption is gone, every window using
/// this must draw its own close button with <see cref="CaptionButtons"/> - otherwise there is no way
/// out of it.
/// </summary>
internal static class Chrome
{
    /// <summary>Height of the strip at the top of the content that drags and double-click-maximises.</summary>
    public const double Caption = 38;

    public static void Apply(Window window, double captionHeight = Caption)
    {
        window.WindowStyle = WindowStyle.None;
        window.ResizeMode = ResizeMode.CanResize;
        window.Background = Brushes.Transparent;
        WindowChrome.SetWindowChrome(window, new WindowChrome
        {
            CaptionHeight = captionHeight,
            // Thin, but wide enough to grab. Resizing is part of what a window is for.
            ResizeBorderThickness = new Thickness(6),
            GlassFrameThickness = new Thickness(0),
            CornerRadius = new CornerRadius(10),
            UseAeroCaptionButtons = false,
        });
    }

    /// <summary>
    /// Clamps a window to the working area. A hard-coded 820px settings window is taller than the
    /// usable height of a 1366x768 laptop, and WPF will happily hang it off the bottom of the screen
    /// where the taskbar and the close button are.
    /// </summary>
    public static void FitToWorkArea(Window window, double preferredWidth, double preferredHeight, double inset = 32)
    {
        var work = SystemParameters.WorkArea;
        window.Width = Math.Min(preferredWidth, Math.Max(320, work.Width - inset));
        window.Height = Math.Min(preferredHeight, Math.Max(320, work.Height - inset));
        window.MaxWidth = work.Width;
        window.MaxHeight = work.Height;
        window.MinWidth = Math.Min(window.MinWidth, window.MaxWidth);
        window.MinHeight = Math.Min(window.MinHeight, window.MaxHeight);
    }

    /// <summary>
    /// The buttons a chromeless window needs on its own: minimise and close, in reading order.
    /// Returns elements already docked, ready to be added to a DockPanel.
    /// </summary>
    public static IEnumerable<UIElement> CaptionButtons(Window window, bool minimisable = true)
    {
        if (minimisable)
        {
            var min = GlyphButton("\uE921", T("Minimize", "تصغير"), "", close: false);
            min.MouseLeftButtonUp += (_, _) => window.WindowState = WindowState.Minimized;
            DockPanel.SetDock(min, Loc.IsRtl ? Dock.Left : Dock.Right);
            yield return min;
        }

        var close = GlyphButton(Glyphs.Close, T("Close", "إغلاق"), "CaptionClose", close: true);
        close.MouseLeftButtonUp += (_, _) => window.Close();
        DockPanel.SetDock(close, Loc.IsRtl ? Dock.Left : Dock.Right);
        yield return close;
    }

    /// <summary>
    /// The close button of a window's caption, or null if the window has none. Named so the self-test
    /// can prove the button really closes the window: removing the system title bar means a missing
    /// button here leaves the user with no way out.
    /// </summary>
    public static Border? FindCloseButton(Window window) => Find<Border>(window, "CaptionClose");

    private static T? Find<T>(DependencyObject? root, string name) where T : FrameworkElement
    {
        if (root is null)
            return null;
        var queue = new Queue<DependencyObject>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var o = queue.Dequeue();
            if (o is T found && found.Name == name)
                return found;
            // Content and templates are not always in the visual tree, so walk both. VisualTreeHelper
            // throws on ColumnDefinition and friends, hence the type guard.
            foreach (var child in LogicalTreeHelper.GetChildren(o).OfType<DependencyObject>())
                queue.Enqueue(child);
            if (o is not Visual and not System.Windows.Media.Media3D.Visual3D)
                continue;
            var count = VisualTreeHelper.GetChildrenCount(o);
            for (var i = 0; i < count; i++)
                queue.Enqueue(VisualTreeHelper.GetChild(o, i));
        }
        return null;
    }

    /// <summary>
    /// Any button that lives in the caption strip: a glyph, a generous hit area, an obvious hover, and
    /// the hit-test opt-out from the chrome. Every window that draws its own caption must build its
    /// buttons here - a copy of this without the last line is a picture, not a button.
    /// </summary>
    public static Border IconButton(string glyph, string tip, Action click, string name = "", bool destructive = false)
    {
        var icon = new TextBlock
        {
            Text = glyph,
            FontFamily = Theme.Icons,
            FontSize = 11,
            Foreground = Theme.Muted,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            // The glyph must not eat the clicks meant for the padding around it.
            IsHitTestVisible = false,
        };
        var button = new Border
        {
            Name = name,
            Background = Brushes.Transparent,
            CornerRadius = new CornerRadius(6),
            Width = 40,
            Height = 32,
            Margin = new Thickness(2, 3, 2, 3),
            Cursor = Cursors.Hand,
            ToolTip = tip,
            Child = icon,
            Focusable = false,
        };
        button.MouseEnter += (_, _) =>
        {
            button.Background = destructive ? Theme.AccentRed : Theme.Hover;
            icon.Foreground = destructive ? Brushes.White : Theme.Text;
        };
        button.MouseLeave += (_, _) =>
        {
            button.Background = Brushes.Transparent;
            icon.Foreground = Theme.Muted;
        };
        button.MouseLeftButtonUp += (_, e) =>
        {
            e.Handled = true;
            click();
        };
        WindowChrome.SetIsHitTestVisibleInChrome(button, true);
        return button;
    }

    /// <summary>
    /// A caption button. Two things make it feel grabbable: the hit area covers the whole strip height
    /// instead of hugging a 10px glyph, and the hover state is obvious. Close additionally turns red,
    /// which is the one place in a window where a colour carries meaning.
    /// </summary>
    private static Border GlyphButton(string glyph, string tip, string name, bool close)
    {
        var icon = new TextBlock
        {
            Text = glyph,
            FontFamily = Theme.Icons,
            FontSize = 11,
            Foreground = Theme.Muted,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            // The glyph must not eat the clicks meant for the padding around it.
            IsHitTestVisible = false,
        };
        var button = new Border
        {
            Name = name,
            Background = Brushes.Transparent,
            CornerRadius = new CornerRadius(6),
            Width = 40,
            Height = 32,
            Margin = new Thickness(2, 3, 2, 3),
            Cursor = Cursors.Hand,
            ToolTip = tip,
            Child = icon,
            Focusable = false,
        };
        void Enter()
        {
            button.Background = close ? Theme.AccentRed : Theme.Hover;
            icon.Foreground = close ? Brushes.White : Theme.Text;
        }
        void Leave()
        {
            button.Background = Brushes.Transparent;
            icon.Foreground = Theme.Muted;
        }
        button.MouseEnter += (_, _) => Enter();
        button.MouseLeave += (_, _) => Leave();
        // Without this the WindowChrome keeps the hit test for the drag gesture and the button is
        // only a picture: no hover, no click.
        WindowChrome.SetIsHitTestVisibleInChrome(button, true);
        return button;
    }

}
