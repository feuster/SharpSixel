using System.Collections.Generic;

namespace SharpSixel.Ui;

/// <summary>Shape of the sixel marker in front of a prompt row.</summary>
internal enum MarkerShape
{
    /// <summary>No marker; the text starts at column 0 in sixel mode.</summary>
    None,

    /// <summary>Circle (ring, with an inner dot if filled).</summary>
    Radio,

    /// <summary>Rounded square (outline, solid if filled).</summary>
    Check,

    /// <summary>Solid circle.</summary>
    Dot,
}

/// <summary>Rounded face drawn behind a part of a row in sixel mode (a dialog button); the label is drawn on it as text.</summary>
internal sealed class PromptChip
{
    /// <summary>Index of the first cell of the face in <see cref="PromptRow.Text"/> (cells after the text start).</summary>
    public int Start { get; set; }

    /// <summary>Width of the face in cells.</summary>
    public int Length { get; set; }

    /// <summary>Index of the label in <see cref="PromptRow.Text"/>.</summary>
    public int LabelStart { get; set; }

    public int LabelLength { get; set; }

    public SixelColor Color { get; set; }

    public SixelColor TextColor { get; set; }
}

/// <summary>One row of a <see cref="PromptView"/>: ASCII prefix, text and the sixel marker.</summary>
internal sealed class PromptRow
{
    private string _asciiPrefix = string.Empty;
    private string _text = string.Empty;

    /// <summary>Text in front of <see cref="Text"/> in ASCII mode (pointer and marker).</summary>
    public string AsciiPrefix
    {
        get => _asciiPrefix;
        set => _asciiPrefix = Clean(value);
    }

    /// <summary>Single-line text; control characters are replaced by spaces.</summary>
    public string Text
    {
        get => _text;
        set => _text = Clean(value);
    }

    public MarkerShape Shape { get; set; }

    public SixelColor MarkerColor { get; set; }

    public bool Filled { get; set; }

    /// <summary>Text color (ANSI); null keeps the terminal color.</summary>
    public SixelColor? TextColor { get; set; }

    /// <summary>True to start the text at the text column of marker rows in sixel mode even without a marker.</summary>
    public bool Indent { get; set; }

    /// <summary>Range of <see cref="Text"/> drawn in reverse video (ANSI only), for example the selected button.</summary>
    public int HighlightStart { get; set; }

    public int HighlightLength { get; set; }

    public SixelColor HighlightColor { get; set; }

    /// <summary>True to show an input caret after the text.</summary>
    public bool Caret { get; set; }

    /// <summary>True to draw a highlight bar behind the row (the current item; sixel only, see <see cref="PromptStyle.HighlightColor"/>).</summary>
    public bool Bar { get; set; }

    /// <summary>Faces drawn behind parts of the row in sixel mode; the row text then only serves the ASCII mode.</summary>
    public List<PromptChip> Chips { get; } = new List<PromptChip>();

    /// <summary>Replaces control characters (line breaks, escape) by spaces so a row always stays one terminal line.</summary>
    public static string Clean(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        char[] chars = value!.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (char.IsControl(chars[i]))
            {
                chars[i] = ' ';
            }
        }

        return new string(chars);
    }
}
