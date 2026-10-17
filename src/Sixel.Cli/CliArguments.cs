using System.Globalization;

namespace Sixel.Cli;

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

    public bool Dither { get; private set; }

    public int? Width { get; private set; }

    public int? Height { get; private set; }

    public bool Fit { get; private set; }

    public bool Upscale { get; private set; }

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
                case "--fit":
                    result.Fit = true;
                    break;
                case "--upscale":
                    result.Upscale = true;
                    break;
                case "--dither":
                    result.Dither = true;
                    break;
                case "--colors":
                    result.Colors = ParseColors(RequireValue(args, ref i, arg));
                    break;
                default:
                    result.AddPositional(arg);
                    break;
            }
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
