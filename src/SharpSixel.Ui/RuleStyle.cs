namespace SharpSixel.Ui;

/// <summary>Look of a <see cref="Rule"/>: line, title color and ASCII characters.</summary>
public sealed class RuleStyle
{
    private int _lineThickness = 1;

    /// <summary>Thickness of the line in pixels (1-4). Default 1. Sixel only.</summary>
    public int LineThickness
    {
        get => _lineThickness;
        set => _lineThickness = Guard.InRange(value, 1, 4, nameof(value));
    }

    /// <summary>Color of the line (ANSI color of the line characters in ASCII mode). Alpha below 128 draws no ANSI color.</summary>
    public SixelColor Color { get; set; } = new SixelColor(128, 128, 128);

    /// <summary>Text color of the title. Alpha below 128 keeps the terminal text color.</summary>
    public SixelColor TitleColor { get; set; } = new SixelColor(52, 152, 219);

    /// <summary>ASCII character of a horizontal rule. Default '-'.</summary>
    public char AsciiHorizontal { get; set; } = '-';

    /// <summary>ASCII character of a vertical rule. Default '|'.</summary>
    public char AsciiVertical { get; set; } = '|';
}
