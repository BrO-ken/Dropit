using System.Windows;
using System.Windows.Shell;

namespace Dropit.App.UI;

/// <summary>
/// Drops the system title bar while keeping native dragging, snapping and resizing, so a dark window
/// does not end up with a light caption bar above it. The window draws its own buttons in
/// <see cref="captionHeight"/> of space at the top.
/// </summary>
internal static class Chrome
{
    /// <summary>Height of the strip at the top of the content that drags and double-click-maximises.</summary>
    public const double Caption = 38;

    public static void Apply(Window window, double captionHeight = Caption)
    {
        window.WindowStyle = WindowStyle.None;
        window.ResizeMode = ResizeMode.CanResize;
        window.Background = System.Windows.Media.Brushes.Transparent;
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
}