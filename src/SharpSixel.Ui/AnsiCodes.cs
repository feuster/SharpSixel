using System.Globalization;

namespace SharpSixel.Ui;

/// <summary>ANSI/VT control sequences used by widgets (cursor save/restore, relative moves, colors).</summary>
internal static class AnsiCodes
{
    public const string SaveCursor = "\u001b7";
    public const string RestoreCursor = "\u001b8";
    public const string EraseLineRest = "\u001b[K";
    public const string EraseLine = "\u001b[2K";
    public const string Reset = "\u001b[0m";
    public const string HideCursor = "\u001b[?25l";
    public const string ShowCursor = "\u001b[?25h";
    public const string ReverseOn = "\u001b[7m";
    public const string ReverseOff = "\u001b[27m";
    public const string MouseOn = "\u001b[?1000h\u001b[?1006h";
    public const string MouseOff = "\u001b[?1006l\u001b[?1000l";
    public const string CursorPositionRequest = "\u001b[6n";

    public static string Up(int count) => Csi(count, 'A');

    public static string Down(int count) => Csi(count, 'B');

    public static string Forward(int count) => Csi(count, 'C');

    public static string EraseChars(int count) => Csi(count, 'X');

    public static string Foreground(SixelColor color) =>
        "\u001b[38;2;" + color.R.ToString(CultureInfo.InvariantCulture) + ";"
        + color.G.ToString(CultureInfo.InvariantCulture) + ";"
        + color.B.ToString(CultureInfo.InvariantCulture) + "m";

    public static string Background(SixelColor color) =>
        "\u001b[48;2;" + color.R.ToString(CultureInfo.InvariantCulture) + ";"
        + color.G.ToString(CultureInfo.InvariantCulture) + ";"
        + color.B.ToString(CultureInfo.InvariantCulture) + "m";

    public static string Colorize(string text, SixelColor color, bool enabled) =>
        enabled && text.Length > 0 ? Foreground(color) + text + Reset : text;

    private static string Csi(int count, char final) =>
        count > 0 ? "\u001b[" + count.ToString(CultureInfo.InvariantCulture) + final : string.Empty;
}
