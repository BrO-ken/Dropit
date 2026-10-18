using static Dropit.Core.Loc;

namespace Dropit.Linux.Shell;

/// <summary>
/// "Dropit…" in the right-click menu of Nautilus (Files), Dolphin and Nemo, for the current user.
/// The Arch package installs the Dolphin and Nemo entries system-wide; Nautilus only reads per-user scripts.
/// </summary>
public static class Integration
{
    private static string DataHome => Environment.GetEnvironmentVariable("XDG_DATA_HOME") is { Length: > 0 } xdg
        ? xdg
        : Path.Combine(LinuxSettings.Home, ".local", "share");

    /// <summary>"dropit" when it is on PATH (package install), otherwise the full path of this binary.</summary>
    private static string Command =>
        TesseractOcr.FindInPath("dropit") is not null ? "dropit" : Environment.ProcessPath ?? "dropit";

    public static string NautilusScript(string command) => $"""
        #!/bin/sh
        # Added by "dropit integrate".
        IFS='
        '
        exec {command} $NAUTILUS_SCRIPT_SELECTED_FILE_PATHS

        """;

    public static string DolphinServiceMenu(string command) => $"""
        [Desktop Entry]
        Type=Service
        MimeType=all/all;
        Actions=dropit
        X-KDE-Priority=TopLevel

        [Desktop Action dropit]
        Name=Dropit…
        Icon=dropit
        Exec={command} %F

        """;

    public static string NemoAction(string command) => $"""
        [Nemo Action]
        Name=Dropit…
        Comment=Convert or process with Dropit
        Exec={command} %F
        Icon-Name=dropit
        Selection=notnone
        Extensions=any;

        """;

    /// <summary>The desktop launcher installed by packages.</summary>
    public static string DesktopEntry(string command) => $"""
        [Desktop Entry]
        Type=Application
        Name=Dropit
        GenericName=File converter
        GenericName[ar]=محوّل ملفات
        Comment=Convert files by dropping them on a wheel
        Comment[ar]=تحويل الملفات بإفلاتها على عجلة
        Exec={command} %F
        Icon=dropit
        Terminal=false
        Categories=Utility;FileTools;
        Keywords=convert;converter;image;video;audio;pdf;ocr;

        """;

    /// <summary>Writes the files a package installs system-wide (used by packaging).</summary>
    public static IEnumerable<string> Export(string dir)
    {
        Directory.CreateDirectory(dir);
        var files = new (string Name, string Content)[]
        {
            ("dropit.desktop", DesktopEntry("dropit")),
            ("dolphin-servicemenu.desktop", DolphinServiceMenu("dropit")),
            ("dropit.nemo_action", NemoAction("dropit")),
        };
        foreach (var (name, content) in files)
        {
            File.WriteAllText(Path.Combine(dir, name), content);
            yield return Path.Combine(dir, name);
        }
    }

    public static IEnumerable<string> Apply(bool install)
    {
        var command = Command;
        var targets = new (string Path, string Content, bool Executable, string Name)[]
        {
            (Path.Combine(DataHome, "nautilus", "scripts", "Dropit"), NautilusScript(command), true, "Nautilus (Files)"),
            (Path.Combine(DataHome, "kio", "servicemenus", "dropit.desktop"), DolphinServiceMenu(command), true, "Dolphin"),
            (Path.Combine(DataHome, "nemo", "actions", "dropit.nemo_action"), NemoAction(command), false, "Nemo"),
        };
        foreach (var (path, content, executable, name) in targets)
        {
            if (!install)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    yield return T($"Removed: {name}", $"تمت الإزالة: {name}");
                }
                continue;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
            if (executable)
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
            yield return T($"Added: {name} → {path}", $"تمت الإضافة: {name} إلى {path}");
        }
        if (install)
        {
            yield return T("In Nautilus the item is under Scripts → Dropit. Thunar: Edit → Configure custom actions, command: ", "في Nautilus يوجد العنصر ضمن Scripts ← Dropit. وفي Thunar: Edit ← Configure custom actions، الأمر: ") + $"{command} %F";
        }
    }
}
