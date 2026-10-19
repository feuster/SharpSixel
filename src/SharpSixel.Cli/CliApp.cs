using SharpSixel.Skia;
using SkiaSharp;

namespace SharpSixel.Cli;

/// <summary>Command line front end: <c>encode</c> images to sixel and <c>decode</c> sixel to images.</summary>
internal static class CliApp
{
    private const string StdioName = "-";

    private const string Usage = """
        Usage:
          SharpSixel encode <input> [-o <file>] [--show] [--colors <2-256>] [--dither [floyd|bayer|none]]
                       [--width <px>] [--height <px>] [--fit] [--upscale]
          SharpSixel decode <input> -o <file>
          SharpSixel check

        <input>  file path, or '-' for standard input.
        encode   input: BMP, PNG, JPEG (and other Skia formats) or a sixel file; output: sixel (default: stdout).
                 A sixel file (header ESC P ... q) is passed through unchanged unless size/color options are given.
        decode   input: sixel; output format by extension: .bmp, .png, .jpg, .jpeg.
        check    asks the terminal whether it supports sixel; exit code 0 = yes, 1 = no/unknown.
                 On success a small test image is displayed.

        Options:
          -o, --output <file>   output file
          --show                display the sixel in the terminal (stdout must be a terminal).
                                Without -o the sixel is only displayed, with -o it is saved and displayed.
          --colors <n>          maximum palette colors for encode (default 256)
          --dither [mode]       dithering for encode when colors are reduced: floyd (default if no mode
                                is given, Floyd-Steinberg), bayer (ordered 8x8) or none
          --width <px>          fit the image into this width (keeps aspect ratio)
          --height <px>         fit the image into this height (keeps aspect ratio)
          --fit                 fit into the terminal's drawable area (needs a terminal on stdin/stdout);
                                --width/--height take precedence per axis
          --upscale             also enlarge small images to the given limits (default: shrink only)
          -h, --help            show this help
          --version             show version
        """;

    public static int Run(string[] args, CliEnvironment env)
    {
        try
        {
            CliArguments arguments = CliArguments.Parse(args);
            if (arguments.Help)
            {
                env.Output.WriteLine(Usage);
                return 0;
            }

            if (arguments.Version)
            {
                env.Output.WriteLine(typeof(CliApp).Assembly.GetName().Version?.ToString(3));
                return 0;
            }

            if (arguments.Command == "check")
            {
                return arguments.Input == null
                    ? Check(env)
                    : throw new UsageException("'check' takes no arguments");
            }

            if (arguments.Command == null || arguments.Input == null)
            {
                throw new UsageException("a command and an input are required");
            }

            if (arguments.Command == "encode")
            {
                Encode(arguments, env);
            }
            else
            {
                Decode(arguments, env);
            }

            return 0;
        }
        catch (UsageException e)
        {
            env.Error.WriteLine($"error: {e.Message}");
            env.Error.WriteLine("Try 'SharpSixel --help'.");
            return 2;
        }
        catch (Exception e) when (e is SixelException or IOException or UnauthorizedAccessException)
        {
            env.Error.WriteLine($"error: {e.Message}");
            return 1;
        }
    }

    private static int Check(CliEnvironment env)
    {
        bool supported = env.DetectSixelTerminal();
        env.Output.WriteLine(supported
            ? "Terminal supports sixel graphics."
            : "Terminal does not support sixel graphics (or did not answer).");
        env.Output.Flush();

        if (supported)
        {
            // Visual confirmation: if the pattern shows up, sixel display really works.
            byte[] testImage = new SixelEncoder().EncodeToBytes(TestImage.Create());
            WriteSixelToStdout(testImage, endWithNewLine: true, env);
        }

        return supported ? 0 : 1;
    }

    private static void Encode(CliArguments arguments, CliEnvironment env)
    {
        if (arguments.Show)
        {
            RequireTerminalOutput(env);
        }

        byte[] data = ReadInput(arguments.Input!, env);
        if (arguments.Show && arguments.Input != StdioName)
        {
            WarnIfSixelUnsupported(env);
        }

        byte[] sixel = CreateSixel(data, arguments, env);

        bool saveToFile = arguments.Output != null && arguments.Output != StdioName;
        if (saveToFile)
        {
            File.WriteAllBytes(arguments.Output!, sixel);
        }

        if (!saveToFile || arguments.Show)
        {
            WriteSixelToStdout(sixel, arguments.Show, env);
        }
    }

    /// <summary>
    /// Turns the input into sixel: images are encoded; a sixel file is validated and passed through unchanged unless
    /// options that change the image (size, colors, dithering) are given, in which case it is decoded and re-encoded.
    /// </summary>
    private static byte[] CreateSixel(byte[] data, CliArguments arguments, CliEnvironment env)
    {
        SixelImage image;
        if (!BmpCodec.IsBmp(data) && SixelDecoder.LooksLikeSixel(data))
        {
            image = new SixelDecoder().Decode(data);
            if (!arguments.HasProcessingOptions)
            {
                return data;
            }
        }
        else
        {
            image = BmpCodec.IsBmp(data) ? BmpCodec.Decode(data) : SkiaImageCodec.Decode(data);
        }

        image = FitImage(image, arguments, env);
        var encoder = new SixelEncoder(new SixelEncoderOptions
        {
            MaxColors = arguments.Colors,
            Dithering = arguments.Dithering,
        });
        return encoder.EncodeToBytes(image);
    }

    private static void RequireTerminalOutput(CliEnvironment env)
    {
        if (!env.IsOutputTerminal())
        {
            throw new SixelException("--show needs standard output to be a terminal; do not redirect it.");
        }
    }

    private static void WarnIfSixelUnsupported(CliEnvironment env)
    {
        if (!env.DetectSixelTerminal())
        {
            env.Error.WriteLine("warning: the terminal does not report sixel support; the image may not be displayed correctly.");
        }
    }

    /// <summary>Writes the sixel to stdout; for display a final line break keeps the prompt on its own line.</summary>
    private static void WriteSixelToStdout(byte[] sixel, bool endWithNewLine, CliEnvironment env)
    {
        env.Stdout.Write(sixel, 0, sixel.Length);
        if (endWithNewLine)
        {
            env.Stdout.WriteByte((byte)'\n');
        }

        env.Stdout.Flush();
    }

    private static SixelImage FitImage(SixelImage image, CliArguments arguments, CliEnvironment env)
    {
        int? maxWidth = arguments.Width;
        int? maxHeight = arguments.Height;
        if (arguments.Fit && (maxWidth == null || maxHeight == null))
        {
            TerminalSize size = env.QueryTerminalSize()
                                ?? throw new SixelException("--fit needs a terminal on stdin and stdout; use --width/--height instead.");
            if (size.Estimated)
            {
                env.Error.WriteLine("warning: terminal did not report its pixel size; assuming 10x20 pixel cells.");
            }

            maxWidth ??= size.Width;
            maxHeight ??= size.Height;
        }

        return ImageScaler.Fit(image, maxWidth, maxHeight, arguments.Upscale);
    }

    private static void Decode(CliArguments arguments, CliEnvironment env)
    {
        if (arguments.Show)
        {
            throw new UsageException("--show is only supported by encode");
        }

        if (arguments.Output == null)
        {
            throw new UsageException("decode requires an output file (-o)");
        }

        Func<SixelImage, byte[]> writer = SelectImageWriter(arguments.Output);
        SixelImage image = new SixelDecoder().Decode(ReadInput(arguments.Input!, env));
        WriteOutput(arguments.Output, writer(image), env);
    }

    private static Func<SixelImage, byte[]> SelectImageWriter(string path)
    {
        switch (Path.GetExtension(path).ToLowerInvariant())
        {
            case ".bmp":
                return BmpCodec.Encode;
            case ".png":
                return image => SkiaImageCodec.Encode(image, SKEncodedImageFormat.Png);
            case ".jpg":
            case ".jpeg":
                return image => SkiaImageCodec.Encode(image, SKEncodedImageFormat.Jpeg);
            default:
                throw new UsageException("output extension must be .bmp, .png, .jpg or .jpeg");
        }
    }

    private static byte[] ReadInput(string input, CliEnvironment env)
    {
        if (input != StdioName)
        {
            return File.ReadAllBytes(input);
        }

        using var buffer = new MemoryStream();
        env.Stdin.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static void WriteOutput(string? output, byte[] bytes, CliEnvironment env)
    {
        if (output == null || output == StdioName)
        {
            env.Stdout.Write(bytes, 0, bytes.Length);
            env.Stdout.Flush();
        }
        else
        {
            File.WriteAllBytes(output, bytes);
        }
    }
}
