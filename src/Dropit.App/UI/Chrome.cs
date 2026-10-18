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
            var min = GlyphButton("\uE921", T("Minimize", "تصغير"));
            min.MouseLeftButtonUp += (_, _) => window.WindowState = WindowState.Minimized;
            DockPanel.SetDock(min, Loc.IsRtl ? Dock.Left : Dock.Right);
            yield return min;
        }

        var close = GlyphButton(Glyphs.Close, T("Close", "إغلاق"), "CaptionClose");
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

    /// <summary>A square icon button with a hover state, sized for the caption strip.</summary>
    private static Border GlyphButton(string glyph, string tip, string name = "")
    {
        var icon = new TextBlock
        {
            Text = glyph,
            FontFamily = Theme.Icons,
            FontSize = 10,
            Foreground = Theme.Muted,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var button = new Border
        {
            Name = name,
            Background = Brushes.Transparent,
            CornerRadius = new CornerRadius(6),
            Width = 30,
            Height = 26,
            Margin = new Thickness(4, 0, 0, 0),
            Cursor = Cursors.Hand,
            ToolTip = tip,
            Child = icon,
        };
        button.MouseEnter += (_, _) => { button.Background = Theme.Hover; icon.Foreground = Theme.Text; };
        button.MouseLeave += (_, _) => { button.Background = Brushes.Transparent; icon.Foreground = Theme.Muted; };
        return button;
    }
}