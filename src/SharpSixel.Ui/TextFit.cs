namespace SharpSixel.Ui;

/// <summary>Cuts text to a cell width and measures it.</summary>
internal static class TextFit
{
    private const string Ellipsis = "\u2026";

    /// <summary>Returns the text unchanged if it fits, else cut to <paramref name="width"/> cells ending with an ellipsis.</summary>
    public static string Clip(string text, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        if (text.Length <= width)
        {
            return text;
        }

        return width > 1 ? text.Substring(0, width - 1) + Ellipsis : text.Substring(0, 1);
    }

    /// <summary>Number of cells of a text, not counting ANSI escape sequences (<c>ESC [ ... final byte</c>).</summary>
    public static int VisibleLength(string text)
    {
        int length = 0;
        for (int i = 0; i < text.Length; i++)
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
                length++;
            }
        }

        return length;
    }
}
