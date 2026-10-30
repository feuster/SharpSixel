using SharpSixel;
using SharpSixel.Osc;

namespace SharpSixel.Tests;

public class TerminalControlTests
{
    private static string Capture(Action<StringWriter> action)
    {
        var writer = new StringWriter();
        action(writer);
        return writer.ToString();
    }

    [Theory]
    [InlineData(CursorStyle.Default, "\u001b[0 q")]
    [InlineData(CursorStyle.BoxBlinking, "\u001b[1 q")]
    [InlineData(CursorStyle.BoxStatic, "\u001b[2 q")]
    [InlineData(CursorStyle.UnderscoreBlinking, "\u001b[3 q")]
    [InlineData(CursorStyle.UnderscoreStatic, "\u001b[4 q")]
    [InlineData(CursorStyle.BarBlinking, "\u001b[5 q")]
    [InlineData(CursorStyle.BarStatic, "\u001b[6 q")]
    public void SetCursorStyle_WritesDecscusr(CursorStyle style, string expected)
    {
        Assert.Equal(expected, Capture(w => TerminalControl.SetCursorStyle(style, w)));
    }

    [Fact]
    public void SetCursorStyle_UndefinedValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TerminalControl.SetCursorStyle((CursorStyle)7, new StringWriter()));
    }

    [Fact]
    public void SetTitle_WritesOsc0()
    {
        Assert.Equal("\u001b]0;My tool\u0007", Capture(w => TerminalControl.SetTitle("My tool", w)));
    }

    [Fact]
    public void SetTitle_RemovesControlCharacters()
    {
        Assert.Equal("\u001b]0;abcd\u0007", Capture(w => TerminalControl.SetTitle("a\u001bb\u0007c\u009cd", w)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SetTitle_NullOrEmpty_Throws(string? title)
    {
        Assert.Throws<ArgumentException>(() => TerminalControl.SetTitle(title!, new StringWriter()));
    }

    [Fact]
    public void SetColor_SixelColor_WritesUppercaseHexAndIgnoresAlpha()
    {
        var color = new SixelColor(255, 128, 0, 10);
        Assert.Equal("\u001b]10;#FF8000\u0007", Capture(w => TerminalControl.SetForegroundColor(color, w)));
        Assert.Equal("\u001b]11;#FF8000\u0007", Capture(w => TerminalControl.SetBackgroundColor(color, w)));
        Assert.Equal("\u001b]12;#FF8000\u0007", Capture(w => TerminalControl.SetCursorColor(color, w)));
    }

    [Fact]
    public void SetColor_String_WritesNormalizedHex()
    {
        Assert.Equal("\u001b]10;#102030\u0007", Capture(w => TerminalControl.SetForegroundColor("#102030", w)));
        Assert.Equal("\u001b]11;#AABBCC\u0007", Capture(w => TerminalControl.SetBackgroundColor("#abc", w)));
        Assert.Equal("\u001b]12;#FF4040\u0007", Capture(w => TerminalControl.SetCursorColor("#ff4040", w)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("FF8000")]
    [InlineData("#12")]
    [InlineData("#12345")]
    [InlineData("#GGGGGG")]
    [InlineData("#12345678")]
    [InlineData("#+12")]
    [InlineData("red")]
    public void SetColor_InvalidString_Throws(string? color)
    {
        var writer = new StringWriter();
        Assert.Throws<ArgumentException>(() => TerminalControl.SetForegroundColor(color!, writer));
        Assert.Throws<ArgumentException>(() => TerminalControl.SetBackgroundColor(color!, writer));
        Assert.Throws<ArgumentException>(() => TerminalControl.SetCursorColor(color!, writer));
        Assert.Equal(string.Empty, writer.ToString());
    }

    [Fact]
    public void ResetColors_WriteOsc110To112()
    {
        Assert.Equal("\u001b]110\u0007", Capture(w => TerminalControl.ResetForegroundColor(w)));
        Assert.Equal("\u001b]111\u0007", Capture(w => TerminalControl.ResetBackgroundColor(w)));
        Assert.Equal("\u001b]112\u0007", Capture(w => TerminalControl.ResetCursorColor(w)));
    }

    [Fact]
    public void Reset_ResetsColorsAndCursorStyle()
    {
        // A remembered title (Windows) may add a title sequence in front, so only the tail is exact.
        string output = Capture(w => TerminalControl.Reset(w));
        Assert.EndsWith("\u001b]110\u0007\u001b]111\u0007\u001b]112\u0007\u001b[0 q", output, StringComparison.Ordinal);
    }
}
