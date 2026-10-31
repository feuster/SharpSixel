namespace SharpSixel.Ui;

/// <summary>Cuts text to a cell width.</summary>
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
}
