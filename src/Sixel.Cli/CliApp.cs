using Sixel.Skia;
using SkiaSharp;

namespace Sixel.Cli;

/// <summary>Command line front end: <c>encode</c> images to sixel and <c>decode</c> sixel to images.</summary>
internal static class CliApp
{
    private const string StdioName = "-";

    private const string Usage = """
        Usage:
          sixel encode <input> [-o <file>] [--colors <2-256>] [--dither]
          sixel decode <input> -o <file>
          sixel check

        <input>  file path, or '-' for standard input.
        encode   input: BMP, PNG, JPEG (and other Skia formats); output: sixel (default: stdout).
        decode   input: sixel; output format by extension: .bmp, .png, .jpg, .jpeg.
        check    asks the terminal whether it supports sixel; exit code 0 = yes, 1 = no/unknown.

        Options:
          -o, --output <file>   output file
          --colors <n>          maximum palette colors for encode (default 256)
          --dither              Floyd-Steinberg dithering for encode when colors are reduced
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
            env.Error.WriteLine("Try 'sixel --help'.");
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
        return supported ? 0 : 1;
    }

    private static void Encode(CliArguments arguments, CliEnvironment env)
    {
        byte[] data = ReadInput(arguments.Input!, env);
        SixelImage image = BmpCodec.IsBmp(data) ? BmpCodec.Decode(data) : SkiaImageCodec.Decode(data);
        var encoder = new SixelEncoder(new SixelEncoderOptions
        {
            MaxColors = arguments.Colors,
            Dithering = arguments.Dither ? SixelDithering.FloydSteinberg : SixelDithering.None,
        });
        WriteOutput(arguments.Output, encoder.EncodeToBytes(image), env);
    }

    private static void Decode(CliArguments arguments, CliEnvironment env)
    {
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
