using SharpSixel;
using SharpSixel.Ui;
using SharpSixelUI;

namespace SharpSixelUI.Tests;

public class UiCliArgumentsTests
{
    private static UiCliArguments Parse(params string[] args)
    {
        Assert.True(UiCliArguments.TryParse(args, out UiCliArguments result, out string error), error);
        return result;
    }

    private static string Fail(params string[] args)
    {
        Assert.False(UiCliArguments.TryParse(args, out _, out string error));
        return error;
    }

    [Fact]
    public void Defaults_AreSet()
    {
        UiCliArguments a = Parse("progress");
        Assert.Equal("progress", a.Command);
        Assert.Equal(RenderMode.Auto, a.Mode);
        Assert.Equal(ProgressTextMode.Percent, a.Text);
        Assert.Equal(5, a.DurationSeconds);
        Assert.Null(a.Caption);
        Assert.Null(a.Width);
        Assert.False(a.NoAnsi);
        Assert.False(a.Help);
    }

    [Fact]
    public void AllOptions_AreParsed()
    {
        UiCliArguments a = Parse(
            "demo", "--mode", "ASCII", "--caption", "Hi", "--text", "fraction", "--duration", "2.5", "--width", "20",
            "--rows", "2", "--radius", "4", "--fill", "#FF0000", "--track", "00ff00", "--border", "#11223344",
            "--background", "transparent", "--active", "#010203", "--inactive", "#040506", "--no-ansi");
        Assert.Equal("demo", a.Command);
        Assert.Equal(RenderMode.Ascii, a.Mode);
        Assert.Equal("Hi", a.Caption);
        Assert.Equal(ProgressTextMode.Fraction, a.Text);
        Assert.Equal(2.5, a.DurationSeconds);
        Assert.Equal(20, a.Width);
        Assert.Equal(2, a.Rows);
        Assert.Equal(4, a.Radius);
        Assert.Equal(new SixelColor(255, 0, 0), a.Fill);
        Assert.Equal(new SixelColor(0, 255, 0), a.Track);
        Assert.Equal(new SixelColor(0x11, 0x22, 0x33, 0x44), a.Border);
        Assert.Equal(0, a.Background!.Value.A);
        Assert.Equal(new SixelColor(1, 2, 3), a.Active);
        Assert.Equal(new SixelColor(4, 5, 6), a.Inactive);
        Assert.True(a.NoAnsi);
    }

    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    public void HelpFlags_AreRecognized(string flag) => Assert.True(Parse(flag).Help);

    [Theory]
    [InlineData("auto", RenderMode.Auto)]
    [InlineData("sixel", RenderMode.Sixel)]
    [InlineData("ascii", RenderMode.Ascii)]
    public void Modes_AreParsed(string value, RenderMode expected) =>
        Assert.Equal(expected, Parse("progress", "--mode", value).Mode);

    [Theory]
    [InlineData("none", ProgressTextMode.None)]
    [InlineData("percent", ProgressTextMode.Percent)]
    [InlineData("fraction", ProgressTextMode.Fraction)]
    public void TextModes_AreParsed(string value, ProgressTextMode expected) =>
        Assert.Equal(expected, Parse("progress", "--text", value).Text);

    [Fact]
    public void InlineValues_AreAccepted()
    {
        UiCliArguments a = Parse("progress", "--background=#0A2347", "--width=20", "--caption=A=B");
        Assert.Equal(new SixelColor(0x0A, 0x23, 0x47), a.Background);
        Assert.Equal(20, a.Width);
        Assert.Equal("A=B", a.Caption);
        Assert.Contains("Unknown option --help", Fail("--help=x"));
        Assert.Contains("--width", Fail("progress", "--width=0"));
    }

    [Fact]
    public void MissingColorValue_HintsAtPowerShellComment()
    {
        Assert.Contains("PowerShell", Fail("progress", "--background"));
        Assert.DoesNotContain("PowerShell", Fail("progress", "--width"));
    }

    [Fact]
    public void Errors_AreReported()
    {
        Assert.Contains("Unknown option --bogus", Fail("progress", "--bogus"));
        Assert.Contains("Missing value for --mode", Fail("progress", "--mode"));
        Assert.Contains("--mode", Fail("progress", "--mode", "x"));
        Assert.Contains("--text", Fail("progress", "--text", "x"));
        Assert.Contains("--fill", Fail("progress", "--fill", "#12345"));
        Assert.Contains("--fill", Fail("progress", "--fill", "#GGGGGG"));
        Assert.Contains("--duration", Fail("progress", "--duration", "0"));
        Assert.Contains("--duration", Fail("progress", "--duration", "abc"));
        Assert.Contains("--duration", Fail("progress", "--duration", "4000"));
        Assert.Contains("--width", Fail("progress", "--width", "0"));
        Assert.Contains("--width", Fail("progress", "--width", "-3"));
        Assert.Contains("--radius", Fail("progress", "--radius", "x"));
        Assert.Contains("Unexpected argument spinner", Fail("progress", "spinner"));
    }
}
