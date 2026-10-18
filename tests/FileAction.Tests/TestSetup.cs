using System.Runtime.CompilerServices;
using FileAction.Core;

namespace FileAction.Tests;

internal static class TestSetup
{
    /// <summary>Tests assert Arabic messages and file names, whatever the machine's language.</summary>
    [ModuleInitializer]
    internal static void Init() => Loc.Current = Language.Arabic;
}

[CollectionDefinition(nameof(LanguageSwitching), DisableParallelization = true)]
public class LanguageSwitching;

[Collection(nameof(LanguageSwitching))]
public class LocalizationTests
{
    /// <summary>Any Arabic letter, so an untranslated label cannot slip into the English UI.</summary>
    private static bool HasArabic(string s) => s.Any(c => c is >= '؀' and <= 'ۿ');

    [Fact]
    public void English_labels_and_output_names()
    {
        try
        {
            Loc.Current = Language.English;
            var ops = FileAction.Core.Operations.Catalog.Build();
            Assert.Equal("Compress", ops.Single(o => o.Id == "image.compress").Label);
            Assert.Equal("Trim", ops.Single(o => o.Id == "video.trim").Label);
            Assert.Equal("3 items", Loc.Items(3));
            Assert.DoesNotContain(ops, o => HasArabic(o.Label));
            Assert.DoesNotContain(ops, o => HasArabic(o.Subtitle ?? ""));
        }
        finally
        {
            Loc.Current = Language.Arabic;
        }
    }

    [Fact]
    public void Arabic_labels_are_translated()
    {
        Loc.Current = Language.Arabic;
        var ops = FileAction.Core.Operations.Catalog.Build();
        Assert.Equal("ضغط", ops.Single(o => o.Id == "image.compress").Label);
        Assert.Equal("قصّ", ops.Single(o => o.Id == "video.trim").Label);
        Assert.DoesNotContain(ops, o => string.IsNullOrWhiteSpace(o.Label));
    }

    /// <summary>Arabic counts differently: zero, one, two, few (3-10) then many.</summary>
    [Theory]
    [InlineData(0, "لا ملفات")]
    [InlineData(1, "ملف واحد")]
    [InlineData(2, "ملفان")]
    [InlineData(3, "3 ملفات")]
    [InlineData(10, "10 ملفات")]
    [InlineData(11, "11 ملفًا")]
    [InlineData(111, "111 ملفًا")]
    public void Arabic_plurals(int n, string expected)
    {
        Loc.Current = Language.Arabic;
        Assert.Equal(expected, Loc.Items(n));
    }

    [Theory]
    [InlineData(0, "0 items")]
    [InlineData(1, "1 item")]
    [InlineData(5, "5 items")]
    public void English_plurals(int n, string expected)
    {
        Loc.Current = Language.English;
        Assert.Equal(expected, Loc.Items(n));
    }

    [Fact]
    public void Arabic_is_right_to_left()
    {
        Loc.Current = Language.Arabic;
        Assert.True(Loc.IsRtl);
        Assert.Equal("النص", Loc.T("text", "النص"));
        // A latin fragment is isolated so a path is not reordered inside Arabic text.
        Assert.Equal("⁦C:\\Photos\\a.jpg⁩", Loc.Ltr("C:\\Photos\\a.jpg"));

        Loc.Current = Language.English;
        Assert.False(Loc.IsRtl);
        Assert.Equal("text", Loc.T("text", "النص"));
        Assert.Equal("C:\\Photos\\a.jpg", Loc.Ltr("C:\\Photos\\a.jpg"));
    }

    [Fact]
    public void Native_names_are_written_in_their_own_language()
    {
        Assert.Equal("العربية", Loc.NativeName(Language.Arabic));
        Assert.Equal("English", Loc.NativeName(Language.English));
    }
}