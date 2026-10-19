using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Dropit.Core;
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
    /// <summary>
    /// Deep navy under a mint accent. The palette, not the code, comes from Sahba on macOS: one accent
    /// colour carries selection everywhere, on a three-stop navy-to-teal base, with surfaces built from
    /// translucent white rather than coloured fills.
    /// </summary>
    Mint,
    /// <summary>A light surface with the same layering, for bright rooms.</summary>
    Light,
    /// <summary>
    /// Warm paper neutrals with a vivid orange accent, the palette of Tangerine on macOS - the app this
    /// whole idea comes from. The neutrals there are not grey: the text is #282b29 and the muted tone
    /// #828782, both carrying a green cast, over a #f4f4f4 page. The accent #f35b28 is deepened a step
    /// for anything that carries white text, which #f35b28 does not at a legible ratio.
    /// </summary>
    Warm,
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
        Success: Color.FromRgb(0x3C, 0xC3, 0x6E),
        Error: Color.FromRgb(0xF0, 0x5A, 0x5A),
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
        Success: Color.FromRgb(0x2F, 0xD4, 0xA0),
        Error: Color.FromRgb(0xFF, 0x7A, 0x7A),
        Accent: Color.FromRgb(0x4C, 0xD5, 0xE8),
        Accent2: Color.FromRgb(0x6E, 0x8C, 0xFF),
        TextOnAccent: Color.FromRgb(0x05, 0x0A, 0x14),
        Shadow: 0.62,
        IsDark: true);

    /// <summary>
    /// Mint on deep navy. The accent is Sahba's <c>sahbaMint</c>, rgb(0.08, 0.72, 0.61). It is used
    /// sparingly - selection, focus and progress - the way that project uses it, rather than as a
    /// two-stop gradient painted over everything.
    /// </summary>
    private static readonly Palette MintPalette = new(
        Backdrop: Color.FromArgb(0xF2, 0x06, 0x0A, 0x16),
        Ambient: Color.FromArgb(0x00, 0x1C, 0x5E, 0x63),
        AmbientStrength: 0.72,
        Panel: Color.FromArgb(0xF6, 0x0A, 0x11, 0x22),
        PanelEdge: Color.FromArgb(0x2E, 0x1A, 0xB2, 0x83),
        Highlight: Color.FromArgb(0x24, 0xFF, 0xFF, 0xFF),
        Card: Color.FromArgb(0xFF, 0x0E, 0x17, 0x2C),
        CardEdge: Color.FromArgb(0x24, 0x1A, 0xB2, 0x83),
        Slice: Color.FromArgb(0xF2, 0x14, 0x1E, 0x36),
        SliceEdge: Color.FromArgb(0x1C, 0xFF, 0xFF, 0xFF),
        Hub: Color.FromArgb(0xFF, 0x07, 0x0D, 0x1C),
        Track: Color.FromArgb(0x2C, 0xB2, 0xC4, 0xFF),
        Page: Color.FromArgb(0xFF, 0x07, 0x0B, 0x17),
        Hover: Color.FromArgb(0x1C, 0x4D, 0xF5, 0xB8),
        Text: Color.FromRgb(0xEB, 0xF0, 0xFF),
        Muted: Color.FromRgb(0x8A, 0x9A, 0xB4),
        Success: Color.FromRgb(0x3C, 0xE0, 0x9A),
        Error: Color.FromRgb(0xFF, 0x6B, 0x6B),
        Accent: Color.FromRgb(0x4A, 0xF2, 0xD6),
        Accent2: Color.FromRgb(0x14, 0xB8, 0x9B),
        TextOnAccent: Color.FromRgb(0x05, 0x26, 0x1F),
        Shadow: 0.66,
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
        Track: Color.FromArgb(0xFF, 0xE4, 0xE4, 0xEC),
        Page: Color.FromArgb(0xFF, 0xF1, 0xF1, 0xF5),
        Hover: Color.FromArgb(0x0C, 0x00, 0x00, 0x00),
        Text: Color.FromRgb(0x1A, 0x1A, 0x21),
        Muted: Color.FromRgb(0x5C, 0x5C, 0x69),
        Success: Color.FromRgb(0x11, 0x76, 0x42),
        Error: Color.FromRgb(0xB4, 0x26, 0x1B),
        // Dark enough for white label text: the previous pair sat near 2.6:1 on white.
        Accent: Color.FromRgb(0xB8, 0x5A, 0x0A),
        Accent2: Color.FromRgb(0x9E, 0x2F, 0x1C),
        TextOnAccent: Colors.White,
        Shadow: 0.20,
        IsDark: false);

    // Tangerine on macOS: a warm paper light theme. Every neutral below is lifted straight off the site,
    // and they are green-grey rather than true grey - #282b29 text, #828782 muted, #e9ece8 rules on a
    // #f4f4f4 page. Cards get the accent at 6% (#c5641810) on their border instead of plain black, which
    // is what gives the page its warm edge, and the filled pills get the #fff0e4 tint.
    private static readonly Palette WarmPalette = new(
        Backdrop: Color.FromArgb(0xF4, 0xF4, 0xF4, 0xF4),
        Ambient: Color.FromArgb(0x00, 0xF3, 0x5B, 0x28),
        AmbientStrength: 0.13,
        Panel: Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF),
        PanelEdge: Color.FromArgb(0x18, 0xC5, 0x64, 0x18),
        Highlight: Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF),
        Card: Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF),
        CardEdge: Color.FromArgb(0x14, 0xC5, 0x64, 0x18),
        Slice: Color.FromArgb(0xFF, 0xF4, 0xF5, 0xF1),
        SliceEdge: Color.FromArgb(0x0C, 0xC5, 0x64, 0x18),
        Hub: Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF),
        Track: Color.FromArgb(0xFF, 0xE5, 0xE7, 0xE4),
        Page: Color.FromArgb(0xFF, 0xF4, 0xF4, 0xF4),
        Hover: Color.FromArgb(0x0A, 0xC5, 0x64, 0x18),
        Text: Color.FromRgb(0x28, 0x2B, 0x29),
        Muted: Color.FromRgb(0x77, 0x7D, 0x78),
        Success: Color.FromRgb(0x4C, 0x6B, 0x3F),
        Error: Color.FromRgb(0xC5, 0x36, 0x0B),
        // The site's #f35b28 is only 3.3:1 against white text, so the filled pills use the next step down
        // in the same ramp: white clears 4.8:1 on #d2431a and 6.5:1 on #ad3210.
        Accent: Color.FromRgb(0xD2, 0x43, 0x1A),
        Accent2: Color.FromRgb(0xAD, 0x32, 0x10),
        TextOnAccent: Colors.White,
        Shadow: 0.16,
        IsDark: false);

    internal sealed record Palette(
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
        Color Success,
        Color Error,
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

    /// <summary>The close button hover. Red is the one place in the UI where a colour carries meaning.</summary>
    public static Brush AccentRed { get; private set; } = null!;

    /// <summary>Same rim colour as a <see cref="Color"/>, for gradient stops that need one.</summary>
    public static Color HighlightColor { get; private set; }
    public static Brush Card { get; private set; } = null!;
    public static Brush CardBorder { get; private set; } = null!;

    /// <summary>Border for controls that have to read as a line on any surface.</summary>
    public static Brush Border { get; private set; } = null!;
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

    /// <summary>
    /// The user's <c>theme.json</c>, if there is one. Its colours are applied on top of the built-in palette of
    /// the theme in use, so a file that mentions one colour changes that colour and nothing else.
    /// </summary>
    public static CustomTheme? Custom { get; set; }

    /// <summary>
    /// What was wrong with the theme file, if anything. Kept with the theme it describes, so the settings can
    /// say what happened without reading the file again.
    /// </summary>
    public static IReadOnlyList<string> CustomProblems { get; set; } = [];

    /// <summary>
    /// The corner radius every rounded surface uses. One setting drives the whole UI, so the app can read as
    /// soft or as sharp without any other change.
    /// </summary>
    public static double Radius { get; private set; } = 10;

    /// <summary>Radius for small controls (buttons, segmented controls): a little less than <see cref="Radius"/>.</summary>
    public static double RadiusSmall { get; private set; } = 7;

    /// <summary>Radius for the smallest surfaces (badges, chips).</summary>
    public static double RadiusTiny { get; private set; } = 6;

    /// <summary>
    /// When the user asked for less movement, the windows appear without the slide and fade. The app still
    /// works the same; only the animation is left out.
    /// </summary>
    public static bool ReduceMotion { get; set; }

    public static bool IsDark => _palette.IsDark;

    public static Brush AccentSoft { get; private set; } = null!;

    // ------------------------------------------------------------------ lifecycle

    /// <summary>Re-reads the chosen theme and the Windows light/dark setting, then repaints the tokens.</summary>
    public static void Refresh()
    {
        var (name, palette) = Resolve();
        // A theme file that mentions "Light" also changes the System theme when Windows is in light mode,
        // because there is no separate palette behind the name: System is DarkGlass or Light.
        var over = Custom?.For(name);
        Use(over is { Count: > 0 } ? Apply(palette, over) : palette);
    }

    /// <summary>Sets the corner radius family from the user's value.</summary>
    public static void SetRadius(double radius)
    {
        Radius = Math.Clamp(radius, 0, 28);
        RadiusSmall = Math.Round(Radius * 0.7);
        RadiusTiny = Math.Round(Radius * 0.6);
    }

    /// <summary>
    /// The palette of a theme with the given colours applied. The self-test uses this to check that every
    /// colour slot in a theme file reaches a real colour, and that every colour the app paints has a slot.
    /// </summary>
    internal static Palette PaletteFor(AppTheme theme, IReadOnlyDictionary<string, string>? over = null)
    {
        var palette = theme switch
        {
            AppTheme.DarkGlass => DarkGlassPalette,
            AppTheme.DeepNight => DeepNightPalette,
            AppTheme.Mint => MintPalette,
            AppTheme.Light => LightPalette,
            AppTheme.Warm => WarmPalette,
            _ => DarkGlassPalette,
        };
        return over is { Count: > 0 } ? Apply(palette, over) : palette;
    }

    /// <summary>The built-in palette for the chosen theme, and the name its theme file section is under.</summary>
    private static (string Name, Palette Palette) Resolve() => (Override ?? AppTheme.System) switch
    {
        AppTheme.DarkGlass => (nameof(AppTheme.DarkGlass), DarkGlassPalette),
        AppTheme.DeepNight => (nameof(AppTheme.DeepNight), DeepNightPalette),
        AppTheme.Mint => (nameof(AppTheme.Mint), MintPalette),
        AppTheme.Light => (nameof(AppTheme.Light), LightPalette),
        AppTheme.Warm => (nameof(AppTheme.Warm), WarmPalette),
        _ => ReadSystemDark()
            ? (nameof(AppTheme.DarkGlass), DarkGlassPalette)
            : (nameof(AppTheme.Light), LightPalette),
    };

    /// <summary>
    /// One colour slot of the palette. The slot names are <see cref="CustomTheme.Tokens"/>; the self-test checks
    /// that every colour the app paints is reachable here, so a new one cannot be added without a name.
    /// </summary>
    private static Palette Apply(Palette p, IReadOnlyDictionary<string, string> over)
    {
        foreach (var (slot, value) in over)
        {
            if (!TryParseColor(value, out var color))
                continue;
            p = slot switch
            {
                "backdrop" => p with { Backdrop = color },
                "ambient" => p with { Ambient = color },
                "panel" => p with { Panel = color },
                "panelEdge" => p with { PanelEdge = color },
                "highlight" => p with { Highlight = color },
                "card" => p with { Card = color },
                "cardEdge" => p with { CardEdge = color },
                "slice" => p with { Slice = color },
                "sliceEdge" => p with { SliceEdge = color },
                "hub" => p with { Hub = color },
                "track" => p with { Track = color },
                "page" => p with { Page = color },
                "hover" => p with { Hover = color },
                "text" => p with { Text = color },
                "muted" => p with { Muted = color },
                "success" => p with { Success = color },
                "error" => p with { Error = color },
                "accent" => p with { Accent = color },
                "accent2" => p with { Accent2 = color },
                "textOnAccent" => p with { TextOnAccent = color },
                _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "Unknown theme colour slot."),
            };
        }
        return p;
    }

    /// <summary>Parses <c>#RRGGBB</c> or <c>#AARRGGBB</c>.</summary>
    internal static bool TryParseColor(string text, out Color color)
    {
        color = default;
        if (text.Length is not (7 or 9) || text[0] != '#')
            return false;
        try
        {
            var hex = text[1..];
            if (hex.Length == 6)
            {
                color = Color.FromRgb(
                    Convert.ToByte(hex[..2], 16),
                    Convert.ToByte(hex[2..4], 16),
                    Convert.ToByte(hex[4..], 16));
            }
            else
            {
                color = Color.FromArgb(
                    Convert.ToByte(hex[..2], 16),
                    Convert.ToByte(hex[2..4], 16),
                    Convert.ToByte(hex[4..6], 16),
                    Convert.ToByte(hex[6..], 16));
            }
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void Use(Palette palette)
    {
        _palette = palette;
        Backdrop = Freeze(new SolidColorBrush(palette.Backdrop));
        BackdropEdge = Freeze(new SolidColorBrush(palette.PanelEdge));
        Panel = Freeze(new SolidColorBrush(palette.Panel));
        PanelBorder = Freeze(new SolidColorBrush(palette.PanelEdge));
        Highlight = Freeze(new SolidColorBrush(palette.Highlight));
        HighlightColor = palette.Highlight;
        AccentRed = Freeze(new SolidColorBrush(Color.FromRgb(0xC4, 0x2B, 0x1C)));
        Card = Freeze(new SolidColorBrush(palette.Card));
        CardBorder = Freeze(new SolidColorBrush(palette.CardEdge));
        Border = Freeze(new SolidColorBrush(palette.IsDark ? Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x28, 0x00, 0x00, 0x00)));
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
        Success = Freeze(new SolidColorBrush(palette.Success));
        Error = Freeze(new SolidColorBrush(palette.Error));
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