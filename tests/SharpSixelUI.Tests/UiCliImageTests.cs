using SharpSixel;
using SharpSixelUI;

namespace SharpSixelUI.Tests;

public class UiCliImageTests
{
    private sealed class Nop : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private static (int Code, string Output, string Error) Execute(string[] args, bool terminal = false, bool sixel = false)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var info = new SixelTerminalInfo(sixel, 0, 0, 10, 20);
        var env = new UiCliEnvironment(
            output, error, () => terminal, () => info, () => new Nop(), _ => { }, () => false, null, () => new Nop());
        int code = UiCliApp.Run(args, env);
        return (code, output.ToString(), error.ToString());
    }

    private static string WhiteBmp(string path)
    {
        var image = new SixelImage(1, 1);
        image.SetPixel(0, 0, new SixelColor(255, 255, 255));
        File.WriteAllBytes(path, BmpCodec.Encode(image));
        return path;
    }

    private static string[] Lines(string text) => text.TrimEnd('\r', '\n').Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

    [Fact]
    public void Image_NotATerminal_PrintsAsciiDemoImageOfRequestedSize()
    {
        var run = Execute(new[] { "image", "--width", "20", "--rows", "5" });
        Assert.Equal(0, run.Code);
        string[] lines = Lines(run.Output);
        Assert.Equal(5, lines.Length);
        Assert.All(lines, line => Assert.Equal(20, line.Length));
        Assert.Contains(lines, line => line.Trim().Length > 0);
        Assert.DoesNotContain("\u001b", run.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Image_Border_AddsFrame()
    {
        var run = Execute(new[] { "image", "--width", "20", "--rows", "5", "--border", "ff0000" });
        string[] lines = Lines(run.Output);
        Assert.Equal("+" + new string('-', 18) + "+", lines[0]);
        Assert.Equal(lines[0], lines[4]);
        Assert.StartsWith("|", lines[2]);
    }

    [Fact]
    public void Image_File_IsFittedIntoView()
    {
        string path = WhiteBmp(Path.GetTempFileName());
        try
        {
            var run = Execute(new[] { "image", "--file", path, "--width", "20", "--rows", "5" });
            Assert.Equal(0, run.Code);
            string expected = new string(' ', 5) + new string('@', 10) + new string(' ', 5);
            Assert.All(Lines(run.Output), line => Assert.Equal(expected, line));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Image_MissingOrUnsupportedFile_ReportsError()
    {
        var missing = Execute(new[] { "image", "--file", Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid() + ".bmp") });
        Assert.Equal(1, missing.Code);
        Assert.NotEqual(string.Empty, missing.Error);
        Assert.Equal(string.Empty, missing.Output);

        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "not an image");
            var unsupported = Execute(new[] { "image", "--file", path });
            Assert.Equal(1, unsupported.Code);
            Assert.Contains("Unsupported image format", unsupported.Error);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Image_TerminalWithSixel_DrawsGraphic()
    {
        var run = Execute(new[] { "image" }, terminal: true, sixel: true);
        Assert.Equal(0, run.Code);
        Assert.Contains("\u001bP", run.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Help_MentionsImage()
    {
        Assert.Contains("image", Execute(new[] { "--help" }).Output);
    }
}
