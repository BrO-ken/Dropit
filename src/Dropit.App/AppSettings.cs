using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dropit.App.Input;
using Dropit.Core;
using static Dropit.Core.Loc;

namespace Dropit.App;

public enum HotkeyPreset
{
    None,
    CtrlAltSpace,
    CtrlShiftSpace,
    CtrlAltF,
    CtrlAltW,
}

/// <summary>User preferences, stored as JSON in %APPDATA%\Dropit\settings.json.</summary>
public sealed class AppSettings
{
    public static string Folder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dropit");

    private static string FilePath => Path.Combine(Folder, "settings.json");

    public ModifierKey ConvertKey { get; set; } = ModifierKey.Shift;
    public ModifierKey ToolsKey { get; set; } = ModifierKey.Ctrl;
    public ModifierKey ActionsKey { get; set; } = ModifierKey.Alt;
    public HotkeyPreset Hotkey { get; set; } = HotkeyPreset.CtrlAltSpace;

    /// <summary>Null = follow the Windows display language.</summary>
    public Language? Language { get; set; }

    /// <summary>Empty = save results next to the original file.</summary>
    public string? OutputDirectory { get; set; }

    public bool RevealResult { get; set; }
    public bool Paused { get; set; }
    public bool FirstRunDone { get; set; }
    public string? LastCopyFolder { get; set; }
    public string? LastMoveFolder { get; set; }

    public Settings Quality { get; set; } = new();

    /// <summary>Operation ids the user removed from the wheels.</summary>
    public List<string> HiddenOperations { get; set; } = [];

    /// <summary>Operation ids that always come first on their wheel, in this order.</summary>
    public List<string> FavoriteOperations { get; set; } = [];

    /// <summary>The last operation run, for "repeat last" (with the values it asked for, e.g. trim times).</summary>
    public string? LastOperationId { get; set; }

    public Dictionary<string, string>? LastParameters { get; set; }

    public bool CheckForUpdates { get; set; } = true;

    /// <summary>Null follows the Windows light/dark setting; otherwise one of the built-in directions.</summary>
    public UI.AppTheme? Theme { get; set; }

    /// <summary>
    /// The corner radius every rounded surface uses, in pixels. One value for the whole app, so it can read as
    /// soft or as sharp without anything else changing.
    /// </summary>
    public int CornerRadius { get; set; } = 10;

    /// <summary>Leave the animations out. The app behaves the same, it just stops moving.</summary>
    public bool ReduceMotion { get; set; }

    /// <summary>
    /// The user's <c>theme.json</c>, if there is one. It overrides the colours of the built-in themes; anything
    /// it does not mention keeps the colour the app ships with.
    /// </summary>
    public static CustomTheme? LoadCustomTheme(IList<string>? problems = null) => CustomTheme.Load(Folder, problems);

    public WheelKind? WheelFor(ModifierKey key)
    {
        if (key == ModifierKey.None)
            return null;
        if (key == ConvertKey)
            return WheelKind.Convert;
        if (key == ToolsKey)
            return WheelKind.Tools;
        if (key == ActionsKey)
            return WheelKind.Actions;
        return null;
    }

    public ModifierKey KeyFor(WheelKind wheel) => wheel switch
    {
        WheelKind.Convert => ConvertKey,
        WheelKind.Tools => ToolsKey,
        _ => ActionsKey,
    };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize(File.ReadAllText(FilePath), SettingsJson.Default.AppSettings) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Broken settings file: start with defaults.
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            var temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(this, SettingsJson.Default.AppSettings));
            File.Move(temp, FilePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Not fatal; settings simply won't persist.
        }
    }

    public static string KeyName(ModifierKey key) => key switch
    {
        ModifierKey.Shift => "Shift",
        ModifierKey.Ctrl => "Ctrl",
        ModifierKey.Alt => "Alt",
        _ => "—",
    };

    public static string WheelName(WheelKind wheel) => wheel switch
    {
        WheelKind.Convert => T("Convert", "تحويل"),
        WheelKind.Tools => T("Tools", "أدوات"),
        _ => T("Actions", "إجراءات"),
    };

    public static string HotkeyName(HotkeyPreset preset) => preset switch
    {
        HotkeyPreset.CtrlAltSpace => T("Ctrl + Alt + Space", "Ctrl + Alt + Space"),
        HotkeyPreset.CtrlShiftSpace => T("Ctrl + Shift + Space", "Ctrl + Shift + Space"),
        HotkeyPreset.CtrlAltF => "Ctrl + Alt + F",
        HotkeyPreset.CtrlAltW => "Ctrl + Alt + W",
        _ => T("Off", "معطّل"),
    };
}

/// <summary>Source-generated JSON for the settings file (works in the trimmed build without reflection).</summary>
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class SettingsJson : JsonSerializerContext;
