using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Microsoft.Win32;

namespace Dropit.App.UI;

/// <summary>The art directions the user can pick between.</summary>
public enum AppTheme
{
    /// <summary>Follow the Windows light/dark app setting, using the matching built-in surface.</summary>
    System,
    /// <summary>Anthracite, layered frosted surfaces, thin luminous rims, a restrained amber glow.</summary>
    DarkGlass,
    /// <summary>Near-black navy under a slow atmospheric wash, cool cyan hairlines.</summary>
    DeepNight,
    /// <summary>A light surface with the same layering, for bright rooms.</summary>
    Light,
}

/// <summary>
/// Colours and fonts shared by the wheel, the job cards and the shelf.
/// <para>
/// The app really has four layers - window backdrop, floating panel, card, control - so the palette
/// carries a token per layer plus a hairline and an inner highlight. That is what makes the surfaces
/// read as stacked glass instead of flat grey boxes.
/// </para>
/// <para>
/// Every brush is a mutable property rather than a readonly field so the whole UI can be re-themed
/// while it is running; <see cref="Refresh"/> swaps them all at once and freezes the new ones.
/// </para>
/// </summary>
internal static class Theme
{
    // ------------------------------------------------------------------ palettes

    private static readonly Palette DarkGlassPalette = new(
        Backdrop: Color.FromArgb(0xEC, 0x16, 0x16, 0x1C),
        Ambient: Color.FromArgb(0x00, 0xFF, 0x9A, 0x4A),
        AmbientStrength: 0.22,
        Panel: Color.FromArgb(0xF4, 0x20, 0x20, 0x27),
        PanelEdge: Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF),
        Highlight: Color.FromArgb(0x2E, 0xFF, 0xFF, 0xFF),
        Card: Color.FromArgb(0xFF, 0x29, 0x29, 0x32),
        CardEdge: Color.FromArgb(0x16, 0xFF, 0xFF, 0xFF),
        Slice: Color.FromArgb(0xFF, 0x25, 0x25, 0x2E),
        SliceEdge: Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF),
        Hub: Color.FromArgb(0xFF, 0x1C, 0x1C, 0x23),
        Track: Color.FromArgb(0x2E, 0xFF, 0xFF, 0xFF),
        Page: Color.FromArgb(0xFF, 0x1B, 0x1B, 0x21),
        Hover: Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF),
        Text: Color.FromRgb(0xF5, 0xF5, 0xF7),
        Muted: Color.FromRgb(0x9B, 0x9B, 0xA8),
        Accent: Color.FromRgb(0xFF, 0x9F, 0x3D),
        Accent2: Color.FromRgb(0xFF, 0x5F, 0x63),
        TextOnAccent: Colors.White,
        Shadow: 0.55,
        IsDark: true);

    private static readonly Palette DeepNightPalette = new(
        Backdrop: Color.FromArgb(0xEC, 0x07, 0x0A, 0x12),
        Ambient: Color.FromArgb(0x00, 0x3C, 0x7C, 0xE8),
        AmbientStrength: 0.85,
        Panel: Color.FromArgb(0xF4, 0x0E, 0x13, 0x1F),
        PanelEdge: Color.FromArgb(0x22, 0x9C, 0xD8, 0xFF),
        Highlight: Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF),
        Card: Color.FromArgb(0xFF, 0x15, 0x1B, 0x2A),
        CardEdge: Color.FromArgb(0x1C, 0x9C, 0xD8, 0xFF),
        Slice: Color.FromArgb(0xFF, 0x14, 0x1A, 0x29),
        SliceEdge: Color.FromArgb(0x18, 0x9C, 0xD8, 0xFF),
        Hub: Color.FromArgb(0xFF, 0x0C, 0x11, 0x1D),
        Track: Color.FromArgb(0x30, 0x9C, 0xD8, 0xFF),
        Page: Color.FromArgb(0xFF, 0x0A, 0x0E, 0x18),
        Hover: Color.FromArgb(0x16, 0x9C, 0xD8, 0xFF),
        Text: Color.FromRgb(0xEC, 0xF3, 0xFA),
        Muted: Color.FromRgb(0x86, 0x93, 0xA8),
        Accent: Color.FromRgb(0x4C, 0xD5, 0xE8),
        Accent2: Color.FromRgb(0x6E, 0x8C, 0xFF),
        TextOnAccent: Color.FromRgb(0x05, 0x0A, 0x14),
        Shadow: 0.62,
        IsDark: true);

    private static readonly Palette LightPalette = new(
        Backdrop: Color.FromArgb(0xF4, 0xF4, 0xF6, 0xF9),
        Ambient: Color.FromArgb(0x00, 0xFF, 0x9A, 0x4A),
        AmbientStrength: 0.10,
        Panel: Color.FromArgb(0xFC, 0xFF, 0xFF, 0xFF),
        PanelEdge: Color.FromArgb(0x16, 0x00, 0x00, 0x00),
        Highlight: Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF),
        Card: Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF),
        CardEdge: Color.FromArgb(0x14, 0x00, 0x00, 0x00),
        Slice: Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF),
        SliceEdge: Color.FromArgb(0x10, 0x00, 0x00, 0x00),
        Hub: Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF),
        Track: Color.FromArgb(0x18, 0x00, 0x00, 0x00),
        Page: Color.FromArgb(0xFF, 0xF1, 0xF1, 0xF5),
        Hover: Color.FromArgb(0x0C, 0x00, 0x00, 0x00),
        Text: Color.FromRgb(0x1A, 0x1A, 0x21),
        Muted: Color.FromRgb(0x6B, 0x6B, 0x77),
        Accent: Color.FromRgb(0xE0, 0x77, 0x1F),
        Accent2: Color.FromRgb(0xD8, 0x43, 0x50),
        TextOnAccent: Colors.White,
        Shadow: 0.20,
        IsDark: false);

    private sealed record Palette(
        Color Backdrop,
        Color Ambient,
        double AmbientStrength,
        Color Panel,
        Color PanelEdge,
        Color Highlight,
        Color Card,
        Color CardEdge,
        Color Slice,
        Color SliceEdge,
        Color Hub,
        Color Track,
        Color Page,
        Color Hover,
        Color Text,
        Color Muted,
        Color Accent,
        Color Accent2,
        Color TextOnAccent,
        double Shadow,
        bool IsDark);

    // ------------------------------------------------------------------ brushes

    private static Palette _palette = DarkGlassPalette;

    public static readonly FontFamily Icons = new("Segoe Fluent Icons, Segoe MDL2 Assets");
    public static readonly FontFamily UiFont = new("Segoe UI Variable Text, Segoe UI");
    public static readonly FontFamily DisplayFont = new("Segoe UI Variable Display, Segoe UI");

    /// <summary>
    /// Arabic stacks diacritics below the baseline and descenders are deep, so body text needs more
    /// leading than the 1.0 WPF default. One value, applied everywhere, keeps latin text readable too.
    /// </summary>
    public const double BodyLineHeight = 1.32;

    public static Brush Backdrop { get; private set; } = null!;
    public static Brush BackdropEdge { get; private set; } = null!;
    public static Brush Panel { get; private set; } = null!;
    public static Brush PanelBorder { get; private set; } = null!;
    public static Brush Highlight { get; private set; } = null!;

    /// <summary>Same rim colour as a <see cref="Color"/>, for gradient stops that need one.</summary>
    public static Color HighlightColor { get; private set; }
    public static Brush Card { get; private set; } = null!;
    public static Brush CardBorder { get; private set; } = null!;
    public static Brush Slice { get; private set; } = null!;
    public static Brush SliceBorder { get; private set; } = null!;
    public static Brush Hub { get; private set; } = null!;
    public static Brush Track { get; private set; } = null!;
    public static Brush Page { get; private set; } = null!;
    public static Brush Hover { get; private set; } = null!;
    public static Brush Text { get; private set; } = null!;
    public static Brush Muted { get; private set; } = null!;
    public static Brush Accent { get; private set; } = null!;
    public static Brush TextOnAccent { get; private set; } = null!;
    public static Brush Success { get; private set; } = null!;
    public static Brush Error { get; private set; } = null!;
    public static LinearGradientBrush AccentGradient { get; private set; } = null!;

    /// <summary>
    /// The slow wash behind the content. Near-invisible in DarkGlass, the whole point of DeepNight,
    /// and a faint warmth on light so the light theme is not clinical.
    /// </summary>
    public static RadialGradientBrush Ambient { get; private set; } = null!;

    /// <summary>Forces one theme; null follows the Windows app setting. Used by the self-test.</summary>
    public static AppTheme? Override { get; set; }

    public static bool IsDark => _palette.IsDark;

    public static Brush AccentSoft { get; private set; } = null!;

    // ------------------------------------------------------------------ lifecycle

    /// <summary>Re-reads the chosen theme and the Windows light/dark setting, then repaints the tokens.</summary>
    public static void Refresh() => Use((Override ?? AppTheme.System) switch
    {
        AppTheme.DarkGlass => DarkGlassPalette,
        AppTheme.DeepNight => DeepNightPalette,
        AppTheme.Light => LightPalette,
        _ => ReadSystemDark() ? DarkGlassPalette : LightPalette,
    });

    private static void Use(Palette palette)
    {
        _palette = palette;
        Backdrop = Freeze(new SolidColorBrush(palette.Backdrop));
        BackdropEdge = Freeze(new SolidColorBrush(palette.PanelEdge));
        Panel = Freeze(new SolidColorBrush(palette.Panel));
        PanelBorder = Freeze(new SolidColorBrush(palette.PanelEdge));
        Highlight = Freeze(new SolidColorBrush(palette.Highlight));
        HighlightColor = palette.Highlight;
        Card = Freeze(new SolidColorBrush(palette.Card));
        CardBorder = Freeze(new SolidColorBrush(palette.CardEdge));
        Slice = Freeze(new SolidColorBrush(palette.Slice));
        SliceBorder = Freeze(new SolidColorBrush(palette.SliceEdge));
        Hub = Freeze(new SolidColorBrush(palette.Hub));
        Track = Freeze(new SolidColorBrush(palette.Track));
        Page = Freeze(new SolidColorBrush(palette.Page));
        Hover = Freeze(new SolidColorBrush(palette.Hover));
        Text = Freeze(new SolidColorBrush(palette.Text));
        Muted = Freeze(new SolidColorBrush(palette.Muted));
        Accent = Freeze(new SolidColorBrush(palette.Accent));
        TextOnAccent = Freeze(new SolidColorBrush(palette.TextOnAccent));
        AccentSoft = Freeze(new SolidColorBrush(Color.FromArgb(
            palette.IsDark ? (byte)0x38 : (byte)0x22, palette.Accent.R, palette.Accent.G, palette.Accent.B)));
        AccentGradient = Freeze(new LinearGradientBrush(palette.Accent, palette.Accent2, new Point(0, 0), new Point(1, 1)));
        Success = Freeze(new SolidColorBrush(Color.FromRgb(0x3C, 0xC3, 0x6E)));
        Error = Freeze(new SolidColorBrush(Color.FromRgb(0xF0, 0x5A, 0x5A)));
        Ambient = Freeze(new RadialGradientBrush
        {
            GradientOrigin = new Point(0.5, 0.18),
            Center = new Point(0.5, 0.18),
            RadiusX = 0.95,
            RadiusY = 1.15,
            GradientStops =
            [
                new GradientStop(Fade(palette.Ambient, palette.AmbientStrength), 0),
                new GradientStop(Color.FromArgb(0, palette.Ambient.R, palette.Ambient.G, palette.Ambient.B), 1),
            ],
        });
    }

    private static Color Fade(Color c, double amount) =>
        Color.FromArgb((byte)Math.Clamp(c.A == 0 ? 255 * amount : c.A * amount, 0, 255), c.R, c.G, c.B);

    private static bool ReadSystemDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is not int light || light == 0;
        }
        catch (System.Security.SecurityException)
        {
            return true;
        }
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>Soft drop shadow under floating surfaces.</summary>
    public static DropShadowEffect Shadow(double blur = 24) =>
        new() { BlurRadius = blur, ShadowDepth = 3, Direction = 270, Opacity = _palette.Shadow, Color = Colors.Black };

    /// <summary>Coloured glow for the highlighted slice.</summary>
    public static DropShadowEffect Glow() =>
        new() { BlurRadius = 26, ShadowDepth = 0, Opacity = IsDark ? 0.75 : 0.5, Color = _palette.Accent };

    private static T Freeze<T>(T brush)
        where T : Freezable
    {
        brush.Freeze();
        return brush;
    }
}