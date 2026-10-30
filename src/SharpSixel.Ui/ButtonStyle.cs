namespace SharpSixel.Ui;

/// <summary>Look of a <see cref="Button"/>: size, colors per state and ASCII characters.</summary>
public sealed class ButtonStyle
{
    private int _padding = 1;
    private int _minWidth;
    private int _heightRows = 1;
    private int _cornerRadius = 4;

    /// <summary>
    /// Free cells between the label and the button edge on each side, in addition to the edge cell itself
    /// (ASCII: bracket plus state marker). At least 1. Default 1.
    /// </summary>
    public int Padding
    {
        get => _padding;
        set => _padding = Guard.AtLeast(value, 1, nameof(value));
    }

    /// <summary>Minimum button width in cells (0 = fit to the label). Default 0.</summary>
    public int MinWidth
    {
        get => _minWidth;
        set => _minWidth = Guard.AtLeast(value, 0, nameof(value));
    }

    /// <summary>Rows the sixel graphic occupies (1-10). Default 1. ASCII always uses one row.</summary>
    public int HeightRows
    {
        get => _heightRows;
        set => _heightRows = Guard.InRange(value, 1, 10, nameof(value));
    }

    /// <summary>Corner radius in pixels (0 = square). Default 4. Sixel only.</summary>
    public int CornerRadius
    {
        get => _cornerRadius;
        set => _cornerRadius = Guard.AtLeast(value, 0, nameof(value));
    }

    /// <summary>Face color of the normal state.</summary>
    public SixelColor NormalColor { get; set; } = new SixelColor(64, 64, 64);

    /// <summary>Face color of the focused state (also the ASCII color).</summary>
    public SixelColor FocusedColor { get; set; } = new SixelColor(52, 152, 219);

    /// <summary>Face color of the pressed state (also the ASCII color).</summary>
    public SixelColor PressedColor { get; set; } = new SixelColor(41, 128, 185);

    /// <summary>Face color of the disabled state.</summary>
    public SixelColor DisabledColor { get; set; } = new SixelColor(40, 40, 40);

    /// <summary>Label color of enabled buttons (sixel; ANSI text drawn on the face).</summary>
    public SixelColor TextColor { get; set; } = new SixelColor(255, 255, 255);

    /// <summary>Label color of disabled buttons (also the ASCII color).</summary>
    public SixelColor DisabledTextColor { get; set; } = new SixelColor(128, 128, 128);

    /// <summary>
    /// Background of the sixel graphic. Transparent (default, alpha below 128) keeps the terminal background;
    /// the covered cells are erased before each frame. Sixel only.
    /// </summary>
    public SixelColor BackgroundColor { get; set; } = SixelColor.Transparent;

    /// <summary>ASCII left edge character. Default '['.</summary>
    public char AsciiBorderLeft { get; set; } = '[';

    /// <summary>ASCII right edge character. Default ']'.</summary>
    public char AsciiBorderRight { get; set; } = ']';

    /// <summary>ASCII marker left of the label in the focused state. Default '&gt;'.</summary>
    public char AsciiFocusLeft { get; set; } = '>';

    /// <summary>ASCII marker right of the label in the focused state. Default '&lt;'.</summary>
    public char AsciiFocusRight { get; set; } = '<';

    /// <summary>ASCII marker on both sides of the label in the pressed state. Default '#'.</summary>
    public char AsciiPressedMarker { get; set; } = '#';

    /// <summary>ASCII marker on both sides of the label in the disabled state. Default '-'.</summary>
    public char AsciiDisabledMarker { get; set; } = '-';

    internal SixelColor FaceColor(ButtonState state)
    {
        switch (state)
        {
            case ButtonState.Focused: return FocusedColor;
            case ButtonState.Pressed: return PressedColor;
            case ButtonState.Disabled: return DisabledColor;
            default: return NormalColor;
        }
    }
}
