using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class TextFitTests
{
    [Theory]
    [InlineData("", 0)]
    [InlineData("abc", 3)]
    [InlineData("日本語", 6)]
    [InlineData("한글", 4)]
    [InlineData("ＡＢ", 4)]
    [InlineData("a\u0301", 1)]
    [InlineData("\U0001F600", 2)]
    [InlineData("\U0001F44D\U0001F3FD", 2)]
    [InlineData("a\u200Bb", 2)]
    [InlineData("é", 1)]
    public void Width_CountsCells(string text, int expected) => Assert.Equal(expected, TextFit.Width(text));

    [Fact]
    public void VisibleLength_IgnoresAnsiAndCountsWideCharacters() =>
        Assert.Equal(4, TextFit.VisibleLength("\u001b[31m日\u001b[0m\u001b[Kab"));

    [Theory]
    [InlineData("日本語", 5, "日本")]
    [InlineData("日本語", 1, "")]
    [InlineData("日本語", 6, "日本語")]
    [InlineData("a\U0001F600b", 2, "a")]
    [InlineData("a\U0001F600b", 3, "a\U0001F600")]
    [InlineData("abc", 0, "")]
    public void Take_NeverSplitsACharacter(string text, int cells, string expected) =>
        Assert.Equal(expected, TextFit.Take(text, cells));

    [Theory]
    [InlineData("日本語", 5, "本語")]
    [InlineData("日本語", 3, "語")]
    [InlineData("ab\U0001F600", 2, "\U0001F600")]
    [InlineData("ab\U0001F600", 1, "")]
    [InlineData("abc", 5, "abc")]
    public void TakeLast_NeverSplitsACharacter(string text, int cells, string expected) =>
        Assert.Equal(expected, TextFit.TakeLast(text, cells));

    [Fact]
    public void Clip_CutsByCellsAndAddsEllipsis()
    {
        Assert.Equal("日本語\u2026", TextFit.Clip("日本語テキスト", 7));
        Assert.Equal("日本語", TextFit.Clip("日本語", 6));
        Assert.Equal(string.Empty, TextFit.Clip("日本語", 1));
        Assert.Equal("ab\u2026", TextFit.Clip("abcdef", 3));
    }

    [Fact]
    public void PadRight_PadsByCells() => Assert.Equal("日  ", TextFit.PadRight("日", 4));
}