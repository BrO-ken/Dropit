using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Dropit.Core;

/// <summary>
/// A user colour theme, in <c>theme.json</c> next to <c>settings.json</c>. It overrides the colours of the
/// built-in themes; anything it does not mention keeps the colour the app ships with.
/// <para>
/// It is a separate file, not part of settings.json, so a theme can be shared and replaced on its own - and so
/// the app never rewrites a file the user wrote. Nothing in it can change how the app behaves, only how it looks.
/// </para>
/// <para>
/// The shape is one object per built-in theme name, each holding colour slots:
/// <code>
/// {
///   "Warm": { "accent": "#D2431A", "page": "#F4F4F4" },
///   "DarkGlass": { "text": "#FFFFFF" }
/// }
/// </code>
/// </para>
/// </summary>
[JsonConverter(typeof(CustomThemeConverter))]
public sealed partial class CustomTheme
{
    /// <summary>File name inside the data folder (next to settings.json).</summary>
    public const string FileName = "theme.json";

    /// <summary>
    /// Bigger files are skipped. The file is read at start-up and whenever the settings are saved, and a real
    /// theme is well under a kilobyte.
    /// </summary>
    public const long MaxBytes = 64 * 1024;

    /// <summary>
    /// The colour slots a theme file may set. Every colour the app paints has one, and the list is checked
    /// against the palette by the UI self-test, so a new colour cannot be added to the app without a slot to
    /// reach it.
    /// </summary>
    public static readonly IReadOnlyList<string> Tokens =
    [
        "backdrop",
        "ambient",
        "panel",
        "panelEdge",
        "highlight",
        "card",
        "cardEdge",
        "slice",
        "sliceEdge",
        "hub",
        "track",
        "page",
        "hover",
        "text",
        "muted",
        "success",
        "error",
        "accent",
        "accent2",
        "textOnAccent",
    ];

    /// <summary>
    /// The built-in themes a file may name. The app's theme list has to match this, which the UI self-test
    /// checks: a theme the app gains later, or loses, must be reachable here.
    /// </summary>
    public static readonly IReadOnlyList<string> ThemeNames =
    [
        "DarkGlass",
        "DeepNight",
        "Mint",
        "Light",
        "Warm",
    ];

    /// <summary>Theme name (as in the app's theme list) to colour slot to <c>#RRGGBB</c> or <c>#AARRGGBB</c>.</summary>
    public Dictionary<string, Dictionary<string, string>> Themes { get; set; } = [];

    /// <summary>True when no theme sets a colour the app can use.</summary>
    public bool IsEmpty => Themes.Count == 0 || Themes.Values.All(v => v.Count == 0);

    /// <summary>
    /// Reads <c>theme.json</c> from <paramref name="directory"/>. Unknown slot names and values that are not
    /// hex colours are dropped and reported in <paramref name="problems"/>; a missing, empty or unreadable file
    /// gives null, so the built-in colours apply.
    /// </summary>
    public static CustomTheme? Load(string directory, IList<string>? problems = null)
    {
        problems ??= [];
        var path = Path.Combine(directory, FileName);
        var info = new FileInfo(path);
        if (!info.Exists)
            return null;
        if (info.Length > MaxBytes)
        {
            problems.Add($"The theme file is larger than {MaxBytes / 1024} KB and was ignored.");
            return null;
        }

        CustomTheme? theme;
        try
        {
            using var stream = File.OpenRead(path);
            theme = JsonSerializer.Deserialize(stream, CustomThemeJson.Default.CustomTheme);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            problems.Add($"The theme file could not be read ({ex.Message}).");
            return null;
        }

        if (theme is null)
        {
            problems.Add("The theme file is empty or is not an object.");
            return null;
        }

        var cleaned = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (themeName, slots) in theme.Themes)
        {
            if (slots is null)
                continue;
            // The name has to be one the app knows, or the colours in it would never be used.
            if (!ThemeNames.Any(t => string.Equals(t, themeName, StringComparison.OrdinalIgnoreCase)))
            {
                problems.Add($"Unknown theme '{themeName}' was ignored.");
                continue;
            }
            var kept = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (slot, value) in slots)
            {
                // Slot names are matched without regard to case and stored in their canonical spelling.
                var canonical = Tokens.FirstOrDefault(t => string.Equals(t, slot, StringComparison.OrdinalIgnoreCase));
                if (canonical is null)
                    problems.Add($"Unknown colour '{slot}' in theme '{themeName}' was ignored.");
                else if (value is null || !HexColor().IsMatch(value))
                    problems.Add($"'{slot}' in theme '{themeName}' is not #RRGGBB or #AARRGGBB, and was ignored.");
                else
                    kept[canonical] = value;
            }
            if (kept.Count > 0)
                cleaned[themeName] = kept;
        }

        if (cleaned.Count == 0)
        {
            problems.Add("The theme file sets no colour the app can use.");
            return null;
        }

        return new CustomTheme { Themes = cleaned };
    }

    /// <summary>The colours set for one theme, or null when that theme is untouched.</summary>
    public IReadOnlyDictionary<string, string>? For(string themeName) =>
        Themes.TryGetValue(themeName, out var slots) ? slots : null;

    [GeneratedRegex("^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$")]
    private static partial Regex HexColor();
}

/// <summary>Source-generated JSON for the theme file (works in the trimmed build without reflection).</summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(CustomTheme))]
internal sealed partial class CustomThemeJson : JsonSerializerContext;

/// <summary>
/// Reads the theme file, whose top level is the theme names themselves:
/// <code>{ "Warm": { "accent": "#D2431A" } }</code>.
/// The names are not a fixed list - a theme the app gains later can be in the file - so they are read as
/// they come and checked afterwards.
/// </summary>
internal sealed class CustomThemeConverter : JsonConverter<CustomTheme>
{
    public override CustomTheme? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("The theme file has to be an object of theme names.");
        var theme = new CustomTheme();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return theme;
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("The theme file has to be an object of theme names.");
            var name = reader.GetString() ?? "";
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"'{name}' has to be an object of colours.");
            var slots = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;
                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException($"'{name}' has to be an object of colours.");
                var slot = reader.GetString() ?? "";
                if (!reader.Read())
                    throw new JsonException($"'{slot}' in '{name}' has no value.");
                slots[slot] = reader.TokenType == JsonTokenType.String ? reader.GetString() ?? "" : "";
            }
            theme.Themes[name] = slots;
        }
        throw new JsonException("The theme file is not complete.");
    }

    public override void Write(Utf8JsonWriter writer, CustomTheme value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (name, slots) in value.Themes)
        {
            writer.WritePropertyName(name);
            writer.WriteStartObject();
            foreach (var (slot, color) in slots)
                writer.WriteString(slot, color);
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }
}
