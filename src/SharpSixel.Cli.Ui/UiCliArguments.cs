using SharpSixel;
using SharpSixel.Ui;

namespace SharpSixelUI;

/// <summary>Parsed command line of SharpSixelUI.</summary>
internal sealed class UiCliArguments
{
    private static readonly Dictionary<string, Func<UiCliArguments, string, string?>> ValueOptions =
        new Dictionary<string, Func<UiCliArguments, string, string?>>(StringComparer.Ordinal)
        {
            ["--mode"] = (a, v) => ParseMode(a, v),
            ["--caption"] = (a, v) => { a.Caption = v; return null; },
            ["--file"] = (a, v) => { a.ImagePath = v; return null; },
            ["--dir"] = (a, v) => { a.StartDirectory = v; return null; },
            ["--filter"] = (a, v) => { a.Filter = v; return null; },
            ["--text"] = (a, v) => ParseText(a, v),
            ["--duration"] = (a, v) => ParseDuration(a, v),
            ["--width"] = (a, v) => ParseInt(v, out int n, 1) ?? Set(() => a.Width = n),
            ["--rows"] = (a, v) => ParseInt(v, out int n, 1) ?? Set(() => a.Rows = n),
            ["--radius"] = (a, v) => ParseInt(v, out int n, 0) ?? Set(() => a.Radius = n),
            ["--fill"] = (a, v) => ParseColor(v, out SixelColor c) ?? Set(() => a.Fill = c),
            ["--track"] = (a, v) => ParseColor(v, out SixelColor c) ?? Set(() => a.Track = c),
            ["--border"] = (a, v) => ParseColor(v, out SixelColor c) ?? Set(() => a.Border = c),
            ["--background"] = (a, v) => ParseColor(v, out SixelColor c) ?? Set(() => a.Background = c),
            ["--active"] = (a, v) => ParseColor(v, out SixelColor c) ?? Set(() => a.Active = c),
            ["--inactive"] = (a, v) => ParseColor(v, out SixelColor c) ?? Set(() => a.Inactive = c),
        };

    private static readonly HashSet<string> ColorOptions = new HashSet<string>(StringComparer.Ordinal)
    {
        "--fill", "--track", "--border", "--background", "--active", "--inactive",
    };

    private const string ColorHint =
        " In PowerShell an unquoted #RRGGBB starts a comment: quote it, omit the '#' or use --option=#RRGGBB.";

    /// <summary>progress, spinner or demo; null if none was given.</summary>
    public string? Command { get; private set; }

    public bool Help { get; private set; }

    public bool NoAnsi { get; private set; }

    public bool NoMouse { get; private set; }

    public bool Click { get; private set; }

    public RenderMode Mode { get; private set; } = RenderMode.Auto;

    public string? Caption { get; private set; }

    /// <summary>Image file of the image command (BMP or sixel); null shows a generated demo image.</summary>
    public string? ImagePath { get; private set; }

    /// <summary>Start directory of the open and save commands; null = the current directory.</summary>
    public string? StartDirectory { get; private set; }

    /// <summary>File filter of the open and save commands, for example <c>*.bmp;*.sixel</c>; null lists all files.</summary>
    public string? Filter { get; private set; }

    public ProgressTextMode Text { get; private set; } = ProgressTextMode.Percent;

    public double DurationSeconds { get; private set; } = 5;

    public int? Width { get; private set; }

    public int? Rows { get; private set; }

    public int? Radius { get; private set; }

    public SixelColor? Fill { get; private set; }

    public SixelColor? Track { get; private set; }

    public SixelColor? Border { get; private set; }

    public SixelColor? Background { get; private set; }

    public SixelColor? Active { get; private set; }

    public SixelColor? Inactive { get; private set; }

    public static bool TryParse(string[] args, out UiCliArguments result, out string error)
    {
        result = new UiCliArguments();
        error = string.Empty;
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            string? inlineValue = null;
            int equals = arg.IndexOf('=');
            if (arg.StartsWith("--", StringComparison.Ordinal) && equals > 0)
            {
                inlineValue = arg.Substring(equals + 1);
                arg = arg.Substring(0, equals);
            }

            if (inlineValue == null && (arg == "-h" || arg == "--help"))
            {
                result.Help = true;
            }
            else if (inlineValue == null && arg == "--no-ansi")
            {
                result.NoAnsi = true;
            }
            else if (inlineValue == null && arg == "--no-mouse")
            {
                result.NoMouse = true;
            }
            else if (inlineValue == null && arg == "--click")
            {
                result.Click = true;
            }
            else if (ValueOptions.TryGetValue(arg, out Func<UiCliArguments, string, string?>? apply))
            {
                if (inlineValue == null && i + 1 >= args.Length)
                {
                    error = "Missing value for " + arg + "." + (ColorOptions.Contains(arg) ? ColorHint : string.Empty);
                    return false;
                }

                string? problem = apply(result, inlineValue ?? args[++i]);
                if (problem != null)
                {
                    error = arg + ": " + problem;
                    return false;
                }
            }
            else if (arg.StartsWith("-", StringComparison.Ordinal))
            {
                error = "Unknown option " + arg + ".";
                return false;
            }
            else if (result.Command == null)
            {
                result.Command = arg;
            }
            else
            {
                error = "Unexpected argument " + arg + ".";
                return false;
            }
        }

        return true;
    }

    private static string? Set(Action assign)
    {
        assign();
        return null;
    }

    private static string? ParseMode(UiCliArguments a, string value)
    {
        switch (value.ToLowerInvariant())
        {
            case "auto": a.Mode = RenderMode.Auto; return null;
            case "sixel": a.Mode = RenderMode.Sixel; return null;
            case "ascii": a.Mode = RenderMode.Ascii; return null;
            default: return "expected auto, sixel or ascii.";
        }
    }

    private static string? ParseText(UiCliArguments a, string value)
    {
        switch (value.ToLowerInvariant())
        {
            case "none": a.Text = ProgressTextMode.None; return null;
            case "percent": a.Text = ProgressTextMode.Percent; return null;
            case "fraction": a.Text = ProgressTextMode.Fraction; return null;
            default: return "expected none, percent or fraction.";
        }
    }

    private static string? ParseDuration(UiCliArguments a, string value)
    {
        if (!double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double seconds)
            || !(seconds > 0) || seconds > 3600)
        {
            return "expected seconds between 0 and 3600.";
        }

        a.DurationSeconds = seconds;
        return null;
    }

    private static string? ParseInt(string value, out int number, int min)
    {
        if (!int.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out number)
            || number < min)
        {
            return "expected an integer >= " + min + ".";
        }

        return null;
    }

    /// <summary>Accepts RRGGBB or RRGGBBAA (optional leading '#') and the word "transparent".</summary>
    private static string? ParseColor(string value, out SixelColor color)
    {
        color = SixelColor.Transparent;
        if (value.Equals("transparent", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string hex = value.StartsWith("#", StringComparison.Ordinal) ? value.Substring(1) : value;
        if ((hex.Length != 6 && hex.Length != 8)
            || !uint.TryParse(hex, System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.InvariantCulture, out uint rgba))
        {
            return "expected #RRGGBB, #RRGGBBAA or transparent.";
        }

        if (hex.Length == 6)
        {
            rgba = (rgba << 8) | 0xFF;
        }

        color = new SixelColor((byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8), (byte)rgba);
        return null;
    }
}
