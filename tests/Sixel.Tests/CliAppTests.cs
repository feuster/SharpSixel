using System.Text;
using Sixel.Cli;
using Sixel.Skia;

namespace Sixel.Tests;

public sealed class CliAppTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "sixel-cli-" + Guid.NewGuid().ToString("N"));

    public CliAppTests()
    {
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        Directory.Delete(_dir, recursive: true);
    }

    private sealed record Result(int ExitCode, byte[] Stdout, string Out, string Error);

    private Result Run(byte[]? stdin, params string[] args)
    {
        var stdout = new MemoryStream();
        var output = new StringWriter();
        var error = new StringWriter();
        var env = new CliEnvironment(new MemoryStream(stdin ?? Array.Empty<byte>()), stdout, output, error);
        int code = CliApp.Run(args, env);
        return new Result(code, stdout.ToArray(), output.ToString(), error.ToString());
    }

    private string PathOf(string name) => Path.Combine(_dir, name);

    private static SixelImage RedImage()
    {
        var image = new SixelImage(4, 6);
        for (int i = 0; i < image.Pixels.Length; i += 4)
        {
            image.Pixels[i] = 255;
            image.Pixels[i + 3] = 255;
        }

        return image;
    }

    private string WriteBmp(string name)
    {
        string path = PathOf(name);
        File.WriteAllBytes(path, BmpCodec.Encode(RedImage()));
        return path;
    }

    [Fact]
    public void Encode_BmpToSixelFile_RoundTrips()
    {
        string input = WriteBmp("in.bmp");
        string output = PathOf("out.six");

        Result result = Run(null, "encode", input, "-o", output);

        Assert.Equal(0, result.ExitCode);
        SixelImage decoded = new SixelDecoder().Decode(File.ReadAllBytes(output));
        Assert.Equal(RedImage().Pixels, decoded.Pixels);
    }

    [Fact]
    public void Encode_WithoutOutput_WritesSixelToStdout()
    {
        Result result = Run(null, "encode", WriteBmp("in.bmp"));

        Assert.Equal(0, result.ExitCode);
        Assert.StartsWith("\u001bP", Encoding.ASCII.GetString(result.Stdout));
    }

    [Fact]
    public void Encode_FromStdin_Works()
    {
        Result result = Run(BmpCodec.Encode(RedImage()), "encode", "-");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(RedImage().Pixels, new SixelDecoder().Decode(result.Stdout).Pixels);
    }

    [Fact]
    public void Encode_PngInput_UsesSkia()
    {
        string input = PathOf("in.png");
        File.WriteAllBytes(input, SkiaImageCodec.Encode(RedImage()));

        Result result = Run(null, "encode", input, "--colors", "4");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(4, new SixelDecoder().Decode(result.Stdout).Width);
    }

    [Theory]
    [InlineData("out.bmp")]
    [InlineData("out.png")]
    [InlineData("out.jpg")]
    [InlineData("out.jpeg")]
    public void Decode_WritesImageFormatByExtension(string name)
    {
        string sixel = PathOf("in.six");
        File.WriteAllBytes(sixel, new SixelEncoder().EncodeToBytes(RedImage()));
        string output = PathOf(name);

        Result result = Run(null, "decode", sixel, "-o", output);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(output));
        SixelImage written = name.EndsWith(".bmp") ? BmpCodec.Decode(File.ReadAllBytes(output))
            : SkiaImageCodec.Decode(File.ReadAllBytes(output));
        Assert.Equal(4, written.Width);
        Assert.Equal(6, written.Height);
    }

    [Fact]
    public void Decode_BmpOutput_IsLossless()
    {
        byte[] sixel = new SixelEncoder().EncodeToBytes(RedImage());
        string output = PathOf("out.bmp");

        Result result = Run(sixel, "decode", "-", "-o", output);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(RedImage().Pixels, BmpCodec.Decode(File.ReadAllBytes(output)).Pixels);
    }

    [Fact]
    public void Help_PrintsUsageAndSucceeds()
    {
        Result result = Run(null, "--help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Usage:", result.Out);
    }

    [Fact]
    public void Version_PrintsVersion()
    {
        Result result = Run(null, "--version");

        Assert.Equal(0, result.ExitCode);
        Assert.Matches(@"^\d+\.\d+\.\d+", result.Out);
    }

    [Theory]
    [InlineData("encode")]
    [InlineData("frobnicate", "x")]
    [InlineData("encode", "x", "--bogus")]
    [InlineData("encode", "x", "y")]
    [InlineData("encode", "x", "--colors", "1")]
    [InlineData("encode", "x", "--colors", "abc")]
    [InlineData("encode", "x", "-o")]
    [InlineData("decode", "x")]
    public void InvalidUsage_ReturnsExitCode2(params string[] args)
    {
        Result result = Run(null, args);

        Assert.Equal(2, result.ExitCode);
        Assert.StartsWith("error:", result.Error);
    }

    [Fact]
    public void Decode_UnsupportedOutputExtension_ReturnsExitCode2()
    {
        Result result = Run(null, "decode", "x", "-o", PathOf("out.xyz"));

        Assert.Equal(2, result.ExitCode);
    }

    [Fact]
    public void Encode_MissingInputFile_ReturnsExitCode1()
    {
        Result result = Run(null, "encode", PathOf("missing.bmp"));

        Assert.Equal(1, result.ExitCode);
        Assert.StartsWith("error:", result.Error);
    }

    [Fact]
    public void Encode_UnsupportedImageData_ReturnsExitCode1()
    {
        string input = PathOf("bad.bin");
        File.WriteAllBytes(input, new byte[] { 1, 2, 3 });

        Result result = Run(null, "encode", input);

        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public void Decode_InputWithoutImage_ReturnsExitCode1()
    {
        string input = PathOf("empty.six");
        File.WriteAllText(input, "\u001bPq\u001b\\");

        Result result = Run(null, "decode", input, "-o", PathOf("out.bmp"));

        Assert.Equal(1, result.ExitCode);
    }
}
