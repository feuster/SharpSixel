namespace SharpSixel.Ui;

/// <summary>Look of a <see cref="ProgressBar"/>: size, colors, border and ASCII characters.</summary>
public sealed class ProgressBarStyle
{
    /// <summary>Minimum bar width in cells (two border characters plus one inner cell).</summary>
    public const int MinWidth = 3;

    private int _width = 30;
    private int _heightRows = 1;
    private double _barThickness = 0.6;
    private int _cornerRadius = 3;
    private int _borderThickness = 1;
    private double _indeterminateSegment = 0.3;
    private int _indeterminateSteps = 30;

    /// <summary>Bar width in terminal cells (including the ASCII borders). Default 30.</summary>
    public int Width
    {
        get => _width;
        set => _width = Guard.AtLeast(value, MinWidth, nameof(value));
    }

    /// <summary>Rows the sixel graphic occupies (1-20). Default 1.</summary>
    public int HeightRows
    {
        get => _heightRows;
        set => _heightRows = Guard.InRange(value, 1, 20, nameof(value));
    }

    /// <summary>Bar thickness as fraction of the occupied height (0.1-1). Default 0.6. Sixel only.</summary>
    public double BarThickness
    {
        get => _barThickness;
        set => _barThickness = Guard.InRange(value, 0.1, 1.0, nameof(value));
    }

    /// <summary>Corner radius in pixels (0 = square). Default 3. Sixel only.</summary>
    public int CornerRadius
    {
        get => _cornerRadius;
        set => _cornerRadius = Guard.AtLeast(value, 0, nameof(value));
    }

    /// <summary>Border thickness in pixels (0 = no border). Default 1. Sixel only.</summary>
    public int BorderThickness
    {
        get => _borderThickness;
        set => _borderThickness = Guard.AtLeast(value, 0, nameof(value));
    }

    /// <summary>Length of the moving segment of an indeterminate bar as fraction of the bar (0.05-0.9). Default 0.3.</summary>
    public double IndeterminateSegment
    {
        get => _indeterminateSegment;
        set => _indeterminateSegment = Guard.InRange(value, 0.05, 0.9, nameof(value));
    }

    /// <summary>
    /// Animation steps (calls of <see cref="ProgressBar.Advance"/>) the segment of an indeterminate bar needs for one way
    /// from end to end; it then moves back. Default 30.
    /// </summary>
    public int IndeterminateSteps
    {
        get => _indeterminateSteps;
        set => _indeterminateSteps = Guard.AtLeast(value, 1, nameof(value));
    }

    /// <summary>Color of the finished part.</summary>
    public SixelColor FillColor { get; set; } = new SixelColor(46, 204, 113);

    /// <summary>Color of the remaining part.</summary>
    public SixelColor TrackColor { get; set; } = new SixelColor(64, 64, 64);

    /// <summary>Border color.</summary>
    public SixelColor BorderColor { get; set; } = new SixelColor(176, 176, 176);

    /// <summary>
    /// Background of the sixel graphic. Transparent (default, alpha below 128) keeps the terminal background;
    /// the covered cells are erased before each frame. Sixel only.
    /// </summary>
    public SixelColor BackgroundColor { get; set; } = SixelColor.Transparent;

    /// <summary>ASCII left border character. Default '['.</summary>
    public char AsciiBorderLeft { get; set; } = '[';

    /// <summary>ASCII right border character. Default ']'.</summary>
    public char AsciiBorderRight { get; set; } = ']';

    /// <summary>ASCII character of the finished part. Default '#'.</summary>
    public char AsciiFill { get; set; } = '#';

    /// <summary>ASCII character of the remaining part. Default '-'.</summary>
    public char AsciiEmpty { get; set; } = '-';
}
