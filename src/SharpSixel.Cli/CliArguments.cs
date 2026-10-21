using System.Globalization;

namespace SharpSixel.Cli;

internal sealed class UsageException : Exception
{
    public UsageException(string message)
        : base(message)
    {
    }
}

internal sealed class CliArguments
{
    public string? Command { get; private set; }

    public string? Input { get; private set; }

    public string? Output { get; private set; }

    public int Colors { get; private set; } = 256;

    public SixelDithering Dithering { get; private set; }

    public int? Width { get; private set; }

    public int? Height { get; private set; }

    /// <summary>Maximum width in terminal character cells (alternative to <see cref="Width"/>).</summary>
    public int? Columns { get; private set; }

    /// <summary>Maximum height in terminal character cells (alternative to <see cref="Height"/>).</summary>
    public int? Rows { get; private set; }

    public bool Fit { get; private set; }

    public bool Upscale { get; private set; }

    public bool Show { get; private set; }

    public bool ColorsSpecified { get; private set; }

    public bool DitherSpecified { get; private set; }

    /// <summary>True if any option changes the image (size, colors, dithering), so sixel input cannot be passed through.</summary>
    public bool HasProcessingOptions =>
        Width != null || Height != null || Columns != null || Rows != null || Fit || ColorsSpecified || DitherSpecified;

    public bool Help { get; private set; }

    public bool Version { get; private set; }

    public static CliArguments Parse(string[] args)
    {
        var result = new CliArguments();
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "-h":
                case "--help":
                    result.Help = true;
                    break;
                case "--version":
                    result.Version = true;
                    break;
                case "-o":
                case "--output":
                    result.Output = RequireValue(args, ref i, arg);
                    break;
                case "--width":
                    result.Width = ParsePositive(RequireValue(args, ref i, arg), arg);
                    break;
                case "--height":
                    result.Height = ParsePositive(RequireValue(args, ref i, arg), arg);
                    break;
                case "--cols":
                    result.Columns = ParsePositive(RequireValue(args, ref i, arg), arg);
                    break;
                case "--rows":
                    result.Rows = ParsePositive(RequireValue(args, ref i, arg), arg);
                    break;
                case "--fit":
                    result.Fit = true;
                    break;
                case "--upscale":
                    result.Upscale = true;
                    break;
                case "--dither":
                    result.Dithering = ParseDithering(args, ref i);
                    result.DitherSpecified = true;
                    break;
                case "--show":
                    result.Show = true;
                    break;
                case "--colors":
                    result.Colors = ParseColors(RequireValue(args, ref i, arg));
                    result.ColorsSpecified = true;
                    break;
                default:
                    result.AddPositional(arg);
                    break;
            }
        }

        if (result.Width != null && result.Columns != null)
        {
            throw new UsageException("--width and --cols both limit the width; use only one");
        }

        if (result.Height != null && result.Rows != null)
        {
            throw new UsageException("--height and --rows both limit the height; use only one");
        }

        return result;
    }

    private void AddPositional(string arg)
    {
        if (arg.Length > 1 && arg[0] == '-')
        {
            throw new UsageException($"unknown option '{arg}'");
        }

        if (Command == null)
        {
            Command = arg == "encode" || arg == "decode" || arg == "check"
                ? arg
                : throw new UsageException($"unknown command '{arg}'");
        }
        else if (Input == null)
        {
            Input = arg;
        }
        else
        {
            throw new UsageException($"unexpected argument '{arg}'");
        }
    }

    private static string RequireValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            throw new UsageException($"option '{option}' requires a value");
        }

        return args[++index];
    }

    /// <summary>Reads the optional mode after --dither; a bare --dither selects Floyd-Steinberg.</summary>
    private static SixelDithering ParseDithering(string[] args, ref int index)
    {
        if (index + 1 < args.Length && TryParseDitherMode(args[index + 1], out SixelDithering mode))
        {
            index++;
            return mode;
        }

        return SixelDithering.FloydSteinberg;
    }

    private static bool TryParseDitherMode(string name, out SixelDithering mode)
    {
        switch (name)
        {
            case "none":
                mode = SixelDithering.None;
                return true;
            case "floyd":
            case "floyd-steinberg":
                mode = SixelDithering.FloydSteinberg;
                return true;
            case "bayer":
            case "ordered":
                mode = SixelDithering.Ordered;
                return true;
            default:
                mode = SixelDithering.None;
                return false;
        }
    }

    private static int ParsePositive(string value, string option)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int number) || number < 1)
        {
            throw new UsageException($"{option} must be a positive integer");
        }

        return number;
    }

    private static int ParseColors(string value)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int colors)
            || colors < 2 || colors > 256)
        {
            throw new UsageException("--colors must be an integer between 2 and 256");
        }

        return colors;
    }
}
