using System;
using System.Globalization;

namespace SharpSixel.Ui;

/// <summary>
/// Measures and cuts text in terminal cells. East Asian wide characters and emoji occupy two cells, combining marks and
/// format characters (zero width joiner, variation selectors) none, everything else one. The width table is an approximation of
/// the Unicode East Asian Width property (wide and fullwidth blocks plus the emoji blocks); a terminal that draws a character with
/// another width, or a ZWJ emoji sequence as one glyph, is not modelled.
/// </summary>
internal static class TextFit
{
    private const string Ellipsis = "\u2026";

    // Inclusive ranges of code points that occupy two cells.
    private static readonly int[] WideRanges =
    {
        0x1100, 0x115F, 0x231A, 0x231B, 0x2329, 0x232A, 0x23E9, 0x23EC, 0x23F0, 0x23F0, 0x23F3, 0x23F3, 0x25FD, 0x25FE,
        0x2614, 0x2615, 0x2648, 0x2653, 0x267F, 0x267F, 0x2693, 0x2693, 0x26A1, 0x26A1, 0x26AA, 0x26AB, 0x26BD, 0x26BE,
        0x26C4, 0x26C5, 0x26CE, 0x26CE, 0x26D4, 0x26D4, 0x26EA, 0x26EA, 0x26F2, 0x26F3, 0x26F5, 0x26F5, 0x26FA, 0x26FA,
        0x26FD, 0x26FD, 0x2705, 0x2705, 0x270A, 0x270B, 0x2728, 0x2728, 0x274C, 0x274C, 0x274E, 0x274E, 0x2753, 0x2755,
        0x2757, 0x2757, 0x2795, 0x2797, 0x27B0, 0x27B0, 0x27BF, 0x27BF, 0x2B1B, 0x2B1C, 0x2B50, 0x2B50, 0x2B55, 0x2B55,
        0x2E80, 0x303E, 0x3041, 0x33FF, 0x3400, 0x4DBF, 0x4E00, 0xA4CF, 0xA960, 0xA97F, 0xAC00, 0xD7A3, 0xF900, 0xFAFF,
        0xFE10, 0xFE19, 0xFE30, 0xFE6F, 0xFF00, 0xFF60, 0xFFE0, 0xFFE6, 0x1B000, 0x1B2FF, 0x1F004, 0x1F004, 0x1F0CF, 0x1F0CF,
        0x1F18E, 0x1F18E, 0x1F191, 0x1F19A, 0x1F200, 0x1F251, 0x1F300, 0x1F64F, 0x1F680, 0x1F6FF, 0x1F900, 0x1F9FF,
        0x1FA70, 0x1FAFF, 0x20000, 0x2FFFD, 0x30000, 0x3FFFD,
    };

    /// <summary>Number of terminal cells of a text without escape sequences (see <see cref="VisibleLength"/> for text with ANSI codes).</summary>
    public static int Width(string text)
    {
        int width = 0;
        for (int i = 0; i < text.Length; i += CodeUnits(text, i))
        {
            width += CellsAt(text, i);
        }

        return width;
    }

    /// <summary>The longest start of <paramref name="text"/> that fits into <paramref name="cells"/> cells (never splits a character).</summary>
    public static string Take(string text, int cells)
    {
        int width = 0;
        int i = 0;
        while (i < text.Length)
        {
            int next = CellsAt(text, i);
            if (width + next > cells)
            {
                break;
            }

            width += next;
            i += CodeUnits(text, i);
        }

        return text.Substring(0, i);
    }

    /// <summary>The longest end of <paramref name="text"/> that fits into <paramref name="cells"/> cells (never splits a character).</summary>
    public static string TakeLast(string text, int cells)
    {
        int width = 0;
        int start = text.Length;
        while (start > 0)
        {
            int previous = start - 1;
            if (previous > 0 && char.IsLowSurrogate(text[previous]) && char.IsHighSurrogate(text[previous - 1]))
            {
                previous--;
            }

            int next = CellsAt(text, previous);
            if (width + next > cells)
            {
                break;
            }

            width += next;
            start = previous;
        }

        return text.Substring(start);
    }

    /// <summary>Returns the text unchanged if it fits, else cut to <paramref name="width"/> cells ending with an ellipsis.</summary>
    public static string Clip(string text, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        if (Width(text) <= width)
        {
            return text;
        }

        return width > 1 ? Take(text, width - 1) + Ellipsis : Take(text, 1);
    }

    /// <summary>Number of cells of a text, not counting ANSI escape sequences (<c>ESC [ ... final byte</c>).</summary>
    public static int VisibleLength(string text)
    {
        int length = 0;
        for (int i = 0; i < text.Length; i += CodeUnits(text, i))
        {
            if (text[i] == '\u001b' && i + 1 < text.Length && text[i + 1] == '[')
            {
                i += 2;
                while (i < text.Length && (text[i] < '@' || text[i] > '~'))
                {
                    i++;
                }
            }
            else
            {
                length += CellsAt(text, i);
            }
        }

        return length;
    }

    /// <summary>The text padded with spaces on the right to <paramref name="cells"/> cells (never cut).</summary>
    public static string PadRight(string text, int cells) => text + new string(' ', Math.Max(0, cells - Width(text)));

    private static int CodeUnits(string text, int index) =>
        index + 1 < text.Length && char.IsHighSurrogate(text[index]) && char.IsLowSurrogate(text[index + 1]) ? 2 : 1;

    private static int CellsAt(string text, int index)
    {
        char first = text[index];
        if (first < 0x20 || (first >= 0x7F && first < 0xA0))
        {
            return 0;
        }

        if (first < 0x300)
        {
            return 1;
        }

        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(text, index);
        if (category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.EnclosingMark || category == UnicodeCategory.Format)
        {
            return 0;
        }

        int codePoint = CodeUnits(text, index) == 2 ? char.ConvertToUtf32(text[index], text[index + 1]) : first;
        if (codePoint >= 0x1F3FB && codePoint <= 0x1F3FF)
        {
            return 0; // skin tone modifier: part of the emoji before it
        }

        for (int i = 0; i < WideRanges.Length; i += 2)
        {
            if (codePoint >= WideRanges[i] && codePoint <= WideRanges[i + 1])
            {
                return 2;
            }
        }

        return 1;
    }
}
