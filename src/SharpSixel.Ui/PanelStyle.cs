namespace SharpSixel.Ui;

/// <summary>Look of a <see cref="Panel"/>: frame, colors and ASCII characters.</summary>
public sealed class PanelStyle
{
    private int _padding = 1;
    private int _lineThickness = 1;
    private int _cornerRadius = 4;

    /// <summary>Free cells left and right of the text inside the frame. Default 1.</summary>
    public int Padding
    {
        get => _padding;
        set => _padding = Guard.AtLeast(value, 0, nameof(value));
    }

    /// <summary>Thickness of the frame line in pixels (1-4). Default 1. Sixel only.</summary>
    public int LineThickness
    {
        get => _lineThickness;
        set => _lineThickness = Guard.InRange(value, 1, 4, nameof(value));
    }

    /// <summary>Corner radius of the frame in pixels (0 = square). Default 4. Sixel only.</summary>
    public int CornerRadius
    {
        get => _cornerRadius;
        set => _cornerRadius = Guard.AtLeast(value, 0, nameof(value));
    }

    /// <summary>Color of the frame (line in sixel mode, ANSI color of the frame characters in ASCII mode). Alpha below 128 draws no color.</summary>
    public SixelColor BorderColor { get; set; } = new SixelColor(128, 128, 128);

    /// <summary>Text color of the title. Alpha below 128 keeps the terminal text color.</summary>
    public SixelColor TitleColor { get; set; } = new SixelColor(52, 152, 219);

    /// <summary>
    /// Background inside the frame. Transparent (default, alpha below 128) keeps the terminal background.
    /// Sixel only; the covered cells are erased before each frame.
    /// </summary>
    public SixelColor BackgroundColor { get; set; } = SixelColor.Transparent;

    /// <summary>ASCII character of the horizontal edges. Default '-'.</summary>
    public char AsciiHorizontal { get; set; } = '-';

    /// <summary>ASCII character of the vertical edges. Default '|'.</summary>
    public char AsciiVertical { get; set; } = '|';

    /// <summary>ASCII character of the corners. Default '+'.</summary>
    public char AsciiCorner { get; set; } = '+';
}
