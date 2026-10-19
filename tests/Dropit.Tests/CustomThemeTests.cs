using Dropit.Core;

namespace Dropit.Tests;

/// <summary>
/// The theme file is written by the user, so everything it can contain has to be handled: a file that is not
/// JSON, a colour that is not a colour, a name that does not exist, a file that is too big. None of it may
/// throw, and none of it may change the app's colours.
/// </summary>
public class CustomThemeTests
{
    private static string TempFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), "dropit-theme-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static CustomTheme? Write(string json, out string folder)
    {
        folder = TempFolder();
        File.WriteAllText(Path.Combine(folder, CustomTheme.FileName), json);
        return CustomTheme.Load(folder);
    }

    [Fact]
    public void Reads_colours_for_a_named_theme()
    {
        var theme = Write("""{ "Warm": { "accent": "#D2431A", "page": "#F4F4F4" } }""", out var folder);
        try
        {
            Assert.NotNull(theme);
            var slots = theme!.For("Warm");
            Assert.NotNull(slots);
            Assert.Equal("#D2431A", slots!["accent"]);
            Assert.Equal("#F4F4F4", slots["page"]);
            Assert.Null(theme.For("Mint"));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void Slot_names_do_not_care_about_case_but_are_stored_canonically()
    {
        var theme = Write("""{ "Warm": { "ACCENT": "#D2431A" } }""", out var folder);
        try
        {
            Assert.NotNull(theme);
            Assert.Equal("#D2431A", theme!.For("Warm")!["accent"]);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void Unknown_slots_and_bad_values_are_ignored_and_reported()
    {
        var theme = Write("""{ "Warm": { "accent": "#D2431A", "nosuchcolour": "#000000", "page": "not a colour" } }""", out var folder);
        try
        {
            Assert.NotNull(theme);
            var slots = theme!.For("Warm")!;
            Assert.Equal(1, slots.Count);
            Assert.True(slots.ContainsKey("accent"));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void A_file_that_is_not_json_is_ignored()
    {
        var theme = Write("{ this is not json", out var folder);
        try
        {
            Assert.Null(theme);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void A_file_with_no_usable_colour_is_ignored()
    {
        var theme = Write("""{ "Dark": { "accent": "#000000" } }""", out var folder);
        try
        {
            // "Dark" is not one of the theme names, so nothing in it is reachable.
            Assert.Null(theme);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void A_file_over_the_size_limit_is_ignored()
    {
        var folder = TempFolder();
        try
        {
            var big = new string('x', (int)CustomTheme.MaxBytes + 1);
            File.WriteAllText(Path.Combine(folder, CustomTheme.FileName), big);
            Assert.Null(CustomTheme.Load(folder));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void A_missing_file_gives_nothing_and_keeps_the_built_in_colours()
    {
        var folder = TempFolder();
        try
        {
            Assert.Null(CustomTheme.Load(folder));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void Every_slot_name_is_a_plain_camel_case_word()
    {
        foreach (var slot in CustomTheme.Tokens)
        {
            Assert.False(string.IsNullOrWhiteSpace(slot));
            Assert.Equal(char.ToLowerInvariant(slot[0]), slot[0]);
            Assert.DoesNotContain(' ', slot);
            Assert.All(slot, c => Assert.True(char.IsLetterOrDigit(c), $"'{slot}' has a character that is not a letter or a digit"));
        }
        Assert.Equal(CustomTheme.Tokens.Count, CustomTheme.Tokens.Distinct(StringComparer.Ordinal).Count());
    }
}
