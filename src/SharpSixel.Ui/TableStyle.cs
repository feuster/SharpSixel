namespace SharpSixel.Ui;

/// <summary>Look of a <see cref="Table"/>: grid, colors and ASCII characters.</summary>
public sealed class TableStyle
{
    private int _padding = 1;
    private int _lineThickness = 1;

    /// <summary>Free cells left and right of the text in each column. Default 1.</summary>
    public int Padding
    {
        get => _padding;
        set => _padding = Guard.AtLeast(value, 0, nameof(value));
    }

    /// <summary>Thickness of the grid lines in pixels (1-4). Default 1. Sixel only.</summary>
    public int LineThickness
    {
        get => _lineThickness;
        set => _lineThickness = Guard.InRange(value, 1, 4, nameof(value));
    }

    /// <summary>True (default) to draw a frame around the table.</summary>
    public bool ShowBorder { get; set; } = true;

    /// <summary>True (default) to show the column headers followed by a rule.</summary>
    public bool ShowHeader { get; set; } = true;

    /// <summary>True (default) to draw a vertical line between the columns.</summary>
    public bool ShowColumnSeparators { get; set; } = true;

    /// <summary>True to draw a horizontal rule between the rows. Default false.</summary>
    public bool ShowRowSeparators { get; set; }

    /// <summary>Color of the grid (lines in sixel mode, ANSI color of the grid characters in ASCII mode). Alpha below 128 draws no color.</summary>
    public SixelColor BorderColor { get; set; } = new SixelColor(128, 128, 128);

    /// <summary>Text color of the header. Alpha below 128 keeps the terminal text color.</summary>
    public SixelColor HeaderTextColor { get; set; } = new SixelColor(52, 152, 219);

    /// <summary>Background of the header row. Transparent (default, alpha below 128) keeps the terminal background. Sixel only.</summary>
    public SixelColor HeaderBackgroundColor { get; set; } = SixelColor.Transparent;

    /// <summary>Background of every second data row. Transparent (default, alpha below 128) draws no stripes. Sixel only.</summary>
    public SixelColor StripeColor { get; set; } = SixelColor.Transparent;

    /// <summary>ASCII character of horizontal rules. Default '-'.</summary>
    public char AsciiHorizontal { get; set; } = '-';

    /// <summary>ASCII character of vertical lines. Default '|'.</summary>
    public char AsciiVertical { get; set; } = '|';

    /// <summary>ASCII character where rules and vertical lines meet. Default '+'.</summary>
    public char AsciiCorner { get; set; } = '+';
}
