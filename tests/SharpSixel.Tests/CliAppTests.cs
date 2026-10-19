using System.Text;
using SharpSixel.Cli;
using SharpSixel.Skia;

namespace SharpSixel.Tests;

public sealed class CliAppTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "sixel-cli-" + Guid.NewGuid().ToString("N"));
    private bool _terminalSupportsSixel;
    private bool _outputIsTerminal = true;
    private int _detectCalls;
    private TerminalSize? _terminalSize;
    private int _sizeQueries;

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
        var env = new CliEnvironment(
            new MemoryStream(stdin ?? Array.Empty<byte>()),
            stdout,
            output,
            error,
            () =>
            {
                _detectCalls++;
                return _terminalSupportsSixel;
            },
            () =>
            {
                _sizeQueries++;
                return _terminalSize;
            },
            () => _outputIsTerminal);
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

    private SixelImage EncodeAndDecode(params string[] extraArgs)
    {
        string input = WriteBmp("in.bmp");
        Result result = Run(null, new[] { "encode", input }.Concat(extraArgs).ToArray());
        Assert.Equal(0, result.ExitCode);
        return new SixelDecoder().Decode(result.Stdout);
    }

    [Theory]
    [InlineData("--width", "2", 2, 3)]
    [InlineData("--height", "3", 2, 3)]
    [InlineData("--width", "100", 4, 6)]
    public void Encode_WidthOrHeight_ShrinksKeepingAspectRatio(string option, string value, int width, int height)
    {
        SixelImage decoded = EncodeAndDecode(option, value);

        Assert.Equal(width, decoded.Width);
        Assert.Equal(height, decoded.Height);
    }

    private string WriteGradientBmp()
    {
        var image = new SixelImage(32, 6);
        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 6; y++)
            {
                image.SetPixel(x, y, new SixelColor((byte)(x * 8), (byte)(x * 8), (byte)(x * 8)));
            }
        }

        string path = PathOf("gradient2.bmp");
        File.WriteAllBytes(path, BmpCodec.Encode(image));
        return path;
    }

    [Theory]
    [InlineData("bayer")]
    [InlineData("ordered")]
    [InlineData("floyd")]
    [InlineData("floyd-steinberg")]
    public void Encode_DitherMode_DiffersFromPlainReduction(string mode)
    {
        string input = WriteGradientBmp();
        byte[] plain = Run(null, "encode", input, "--colors", "2").Stdout;

        Result result = Run(null, "encode", input, "--colors", "2", "--dither", mode);

        Assert.Equal(0, result.ExitCode);
        Assert.NotEqual(plain, result.Stdout);
    }

    [Fact]
    public void Encode_DitherNone_EqualsPlainReduction()
    {
        string input = WriteGradientBmp();

        byte[] plain = Run(null, "encode", input, "--colors", "2").Stdout;
        byte[] none = Run(null, "encode", input, "--colors", "2", "--dither", "none").Stdout;

        Assert.Equal(plain, none);
    }

    [Fact]
    public void Encode_BayerAndFloyd_ProduceDifferentOutput()
    {
        string input = WriteGradientBmp();

        byte[] bayer = Run(null, "encode", input, "--colors", "2", "--dither", "bayer").Stdout;
        byte[] floyd = Run(null, "encode", input, "--colors", "2", "--dither", "floyd").Stdout;

        Assert.NotEqual(bayer, floyd);
    }

    [Fact]
    public void Encode_BareDitherBeforeOtherOption_DefaultsToFloydSteinberg()
    {
        string input = WriteGradientBmp();

        byte[] bare = Run(null, "encode", input, "--dither", "--colors", "2").Stdout;
        byte[] floyd = Run(null, "encode", input, "--colors", "2", "--dither", "floyd").Stdout;

        Assert.Equal(floyd, bare);
    }

    [Fact]
    public void Encode_UpscaleOption_EnlargesToLimit()
    {
        SixelImage decoded = EncodeAndDecode("--width", "8", "--upscale");

        Assert.Equal(8, decoded.Width);
        Assert.Equal(12, decoded.Height);
    }

    [Fact]
    public void Encode_Fit_UsesTerminalSize()
    {
        _terminalSize = new TerminalSize(2, 100, false);

        SixelImage decoded = EncodeAndDecode("--fit");

        Assert.Equal(2, decoded.Width);
        Assert.Equal(3, decoded.Height);
    }

    [Fact]
    public void Encode_FitWithExplicitWidth_ExplicitAxisWins()
    {
        _terminalSize = new TerminalSize(2, 100, false);

        SixelImage decoded = EncodeAndDecode("--fit", "--width", "4");

        Assert.Equal(4, decoded.Width);
        Assert.Equal(6, decoded.Height);
    }

    [Fact]
    public void Encode_FitWithBothLimits_DoesNotQueryTerminal()
    {
        _terminalSize = new TerminalSize(1, 1, false);

        SixelImage decoded = EncodeAndDecode("--fit", "--width", "2", "--height", "3");

        Assert.Equal(0, _sizeQueries);
        Assert.Equal(2, decoded.Width);
    }

    [Fact]
    public void Encode_FitWithoutTerminal_ReturnsExitCode1()
    {
        _terminalSize = null;

        Result result = Run(null, "encode", WriteBmp("in.bmp"), "--fit");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("--fit", result.Error);
    }

    [Fact]
    public void Encode_FitWithEstimatedSize_WarnsOnStderr()
    {
        _terminalSize = new TerminalSize(2, 100, true);

        Result result = Run(null, "encode", WriteBmp("in.bmp"), "--fit");

        Assert.Equal(0, result.ExitCode);
        Assert.StartsWith("warning:", result.Error);
    }

    [Fact]
    public void Encode_WithoutSizeOptions_KeepsOriginalSize()
    {
        SixelImage decoded = EncodeAndDecode();

        Assert.Equal(4, decoded.Width);
        Assert.Equal(6, decoded.Height);
        Assert.Equal(0, _sizeQueries);
    }

    [Fact]
    public void Encode_DitherOption_ReducesColorsWithErrorDiffusion()
    {
        var image = new SixelImage(32, 6);
        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 6; y++)
            {
                image.SetPixel(x, y, new SixelColor((byte)(x * 8), (byte)(x * 8), (byte)(x * 8)));
            }
        }

        string input = PathOf("gradient.bmp");
        File.WriteAllBytes(input, BmpCodec.Encode(image));

        SixelImage plain = new SixelDecoder().Decode(Run(null, "encode", input, "--colors", "2").Stdout);
        Result dithered = Run(null, "encode", input, "--colors", "2", "--dither");

        Assert.Equal(0, dithered.ExitCode);
        SixelImage ditheredImage = new SixelDecoder().Decode(dithered.Stdout);
        Assert.NotEqual(plain.Pixels, ditheredImage.Pixels);
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

    [Theory]
    [InlineData(true, 0, "supports")]
    [InlineData(false, 1, "does not support")]
    public void Check_ReportsTerminalCapabilityViaExitCode(bool supported, int expectedExit, string expectedText)
    {
        _terminalSupportsSixel = supported;

        Result result = Run(null, "check");

        Assert.Equal(expectedExit, result.ExitCode);
        Assert.Contains(expectedText, result.Out);
    }

    [Fact]
    public void Check_Supported_DisplaysTestImageAfterTheMessage()
    {
        _terminalSupportsSixel = true;

        Result result = Run(null, "check");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("supports sixel", result.Out);
        Assert.StartsWith("\u001bP", Encoding.ASCII.GetString(result.Stdout));
        Assert.Equal((byte)'\n', result.Stdout[^1]);
        SixelImage shown = new SixelDecoder().Decode(result.Stdout);
        Assert.Equal(TestImage.Width, shown.Width);
        Assert.Equal(TestImage.Height, shown.Height);
    }

    [Fact]
    public void Check_Unsupported_DisplaysNoImage()
    {
        _terminalSupportsSixel = false;

        Result result = Run(null, "check");

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.Stdout);
    }

    [Fact]
    public void Check_WithArgument_ReturnsExitCode2()
    {
        Assert.Equal(2, Run(null, "check", "x").ExitCode);
    }

    [Fact]
    public void ConsoleTerminal_WithRedirectedStreams_ReportsNoSixel()
    {
        // Test hosts redirect stdin/stdout, so no query may be sent and the call must not block.
        Assert.False(ConsoleTerminal.DetectSixel());
    }

    private string WriteSixel(string name)
    {
        string path = PathOf(name);
        File.WriteAllBytes(path, new SixelEncoder().EncodeToBytes(RedImage()));
        return path;
    }

    private static byte[] WithNewLine(byte[] bytes) => bytes.Concat(new[] { (byte)'\n' }).ToArray();

    [Fact]
    public void Show_WithoutOutput_DisplaysOnlyAndEndsWithNewLine()
    {
        _terminalSupportsSixel = true;

        Result result = Run(null, "encode", WriteBmp("in.bmp"), "--show");

        Assert.Equal(0, result.ExitCode);
        string text = Encoding.ASCII.GetString(result.Stdout);
        Assert.StartsWith("\u001bP", text);
        Assert.EndsWith("\u001b\\\n", text);
        Assert.Equal(string.Empty, result.Error);
        Assert.Equal(new[] { "in.bmp" }, Directory.GetFiles(_dir).Select(Path.GetFileName));
    }

    [Fact]
    public void Show_WithOutput_SavesAndDisplays()
    {
        _terminalSupportsSixel = true;
        string output = PathOf("saved.six");

        Result result = Run(null, "encode", WriteBmp("in.bmp"), "-o", output, "--show");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(WithNewLine(File.ReadAllBytes(output)), result.Stdout);
    }

    [Fact]
    public void Output_WithoutShow_OnlySavesAndDoesNotDisplay()
    {
        string output = PathOf("saved.six");

        Result result = Run(null, "encode", WriteBmp("in.bmp"), "-o", output);

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Stdout);
        Assert.True(File.Exists(output));
        Assert.Equal(0, _detectCalls);
    }

    [Fact]
    public void Show_SixelInput_IsDisplayedUnchanged()
    {
        _terminalSupportsSixel = true;
        string input = WriteSixel("in.six");

        Result result = Run(null, "encode", input, "--show");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(WithNewLine(File.ReadAllBytes(input)), result.Stdout);
    }

    [Fact]
    public void Show_SixelInputWithOutput_SavesIdenticalCopyAndDisplays()
    {
        _terminalSupportsSixel = true;
        string input = WriteSixel("in.six");
        string output = PathOf("copy.six");

        Result result = Run(null, "encode", input, "-o", output, "--show");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(File.ReadAllBytes(input), File.ReadAllBytes(output));
        Assert.Equal(WithNewLine(File.ReadAllBytes(input)), result.Stdout);
    }

    [Fact]
    public void SixelInput_WithSizeOption_IsDecodedScaledAndReencoded()
    {
        _terminalSupportsSixel = true;

        Result result = Run(null, "encode", WriteSixel("in.six"), "--show", "--width", "2");

        Assert.Equal(0, result.ExitCode);
        SixelImage shown = new SixelDecoder().Decode(result.Stdout);
        Assert.Equal(2, shown.Width);
        Assert.Equal(3, shown.Height);
    }

    [Fact]
    public void SixelInput_WithoutShowAndOutput_IsWrittenToStdoutUnchanged()
    {
        string input = WriteSixel("in.six");

        Result result = Run(null, "encode", input);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(File.ReadAllBytes(input), result.Stdout);
    }

    [Fact]
    public void Show_WithRedirectedOutput_ReturnsExitCode1AndWritesNothing()
    {
        _outputIsTerminal = false;
        string output = PathOf("saved.six");

        Result result = Run(null, "encode", WriteBmp("in.bmp"), "-o", output, "--show");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("--show", result.Error);
        Assert.Empty(result.Stdout);
        Assert.False(File.Exists(output));
    }

    [Fact]
    public void Show_TerminalWithoutSixelSupport_WarnsButStillDisplays()
    {
        _terminalSupportsSixel = false;

        Result result = Run(null, "encode", WriteBmp("in.bmp"), "--show");

        Assert.Equal(0, result.ExitCode);
        Assert.StartsWith("warning:", result.Error);
        Assert.NotEmpty(result.Stdout);
    }

    [Fact]
    public void Show_ChecksTerminalOnce_ButNotWhenReadingFromStdin()
    {
        _terminalSupportsSixel = true;
        Run(null, "encode", WriteBmp("in.bmp"), "--show");
        Assert.Equal(1, _detectCalls);

        Result viaStdin = Run(BmpCodec.Encode(RedImage()), "encode", "-", "--show");

        Assert.Equal(0, viaStdin.ExitCode);
        Assert.Equal(1, _detectCalls);
    }

    [Fact]
    public void Show_WithDecode_ReturnsExitCode2()
    {
        Result result = Run(null, "decode", WriteSixel("in.six"), "-o", PathOf("out.png"), "--show");

        Assert.Equal(2, result.ExitCode);
    }

    [Fact]
    public void Show_InvalidSixelData_ReturnsExitCode1()
    {
        _terminalSupportsSixel = true;
        string input = PathOf("empty.six");
        File.WriteAllText(input, "\u001bPq\u001b\\");

        Result result = Run(null, "encode", input, "--show");

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.Stdout);
    }

    [Fact]
    public void Show_PlainTextFile_IsNeitherImageNorSixel_ReturnsExitCode1()
    {
        _terminalSupportsSixel = true;
        string input = PathOf("note.txt");
        File.WriteAllText(input, "hello world, this is just text");

        Result result = Run(null, "encode", input, "--show");

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.Stdout);
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
    [InlineData("encode", "x", "--width", "0")]
    [InlineData("encode", "x", "--width", "abc")]
    [InlineData("encode", "x", "--height", "-1")]
    [InlineData("encode", "x", "--height")]
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
