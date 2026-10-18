using System.Globalization;

namespace Dropit.Core;

public enum Language
{
    English,
    Arabic,
}

/// <summary>
/// Tiny two-language localization: every user-facing string is written as <c>T("English", "العربية")</c>
/// at its call site, so the context stays next to the text.
/// <para>
/// The Arabic argument has no default value on purpose: that turns "this string was never
/// translated" into a compile error naming the file and line, instead of an English string
/// quietly leaking into the Arabic UI.
/// </para>
/// <para>
/// Numbers and dates keep the invariant culture. Arabic-Indic digits are authentic but they
/// would leak into generated file names and into strings that converters parse back, so the
/// whole app stays on Western digits.
/// </para>
/// </summary>
public static class Loc
{
    public static Language Current { get; set; } = Detect();

    /// <summary>Arabic is written right to left, so panels, labels and alignment follow it.</summary>
    public static bool IsRtl => Current == Language.Arabic;

    /// <summary>Arabic for Arabic Windows, English everywhere else.</summary>
    public static Language Detect() =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName is "ar" ? Language.Arabic : Language.English;

    public static string T(string english, string arabic) => Current == Language.Arabic ? arabic : english;

    /// <summary>The native name of a language, always written in that language.</summary>
    public static string NativeName(Language language) => language switch
    {
        Language.Arabic => "العربية",
        _ => "English",
    };

    /// <summary>
    /// Keeps a left-to-right fragment - a path, a format name, a duration, a version - in one
    /// piece when it sits inside Arabic text. The isolates stop the bidirectional algorithm from
    /// reordering "photo - 2.png" or a Windows path into something unreadable.
    /// </summary>
    public static string Ltr(string value) => IsRtl ? "\u2066" + value + "\u2069" : value;

    /// <summary>
    /// Arabic counts differently from English: zero, one, two, few (3-10) and many (11+).
    /// <c>"1 item" / "3 items" / "5 items"</c>, <c>"ملف واحد" / "٣ ملفات" / "١١ ملفًا"</c>.
    /// </summary>
    public static string Items(int n)
    {
        if (Current != Language.Arabic)
            return n == 1 ? "1 item" : $"{n} items";

        var lastTwo = Math.Abs(n) % 100;
        return n switch
        {
            0 => "لا ملفات",
            1 => "ملف واحد",
            2 => "ملفان",
            _ when lastTwo is >= 3 and <= 10 => $"{n} ملفات",
            _ => $"{n} ملفًا",
        };
    }
}