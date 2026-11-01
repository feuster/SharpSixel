namespace SharpSixel.Ui;

/// <summary>Look of a <see cref="CheckBox"/> and a <see cref="RadioGroup"/>: colors and ASCII markers.</summary>
public sealed class ToggleStyle
{
    private string _asciiCheckOn = "[x]";
    private string _asciiCheckOff = "[ ]";
    private string _asciiRadioOn = "(*)";
    private string _asciiRadioOff = "( )";

    /// <summary>Color of checked or selected markers and of the text of the focused item.</summary>
    public SixelColor ActiveColor { get; set; } = new SixelColor(52, 152, 219);

    /// <summary>Color of unchecked or unselected markers.</summary>
    public SixelColor InactiveColor { get; set; } = new SixelColor(149, 165, 166);

    /// <summary>Color of marker and text of disabled widgets.</summary>
    public SixelColor DisabledColor { get; set; } = new SixelColor(128, 128, 128);

    /// <summary>Color of the bar behind the focused item. Transparent (alpha 0) turns the bar off. Sixel only.</summary>
    public SixelColor HighlightColor { get; set; } = new SixelColor(38, 52, 70);

    /// <summary>
    /// Background of the sixel graphic. Transparent (default) keeps the terminal background; the holes of the markers
    /// are punched transparent. Sixel only.
    /// </summary>
    public SixelColor BackgroundColor { get; set; } = SixelColor.Transparent;

    /// <summary>ASCII character in front of the focused item. Default '&gt;'.</summary>
    public char AsciiPointer { get; set; } = '>';

    /// <summary>ASCII marker of a checked check box. Default <c>[x]</c>.</summary>
    public string AsciiCheckOn
    {
        get => _asciiCheckOn;
        set => _asciiCheckOn = Guard.NotEmpty(value, nameof(value));
    }

    /// <summary>ASCII marker of an unchecked check box. Default <c>[ ]</c>.</summary>
    public string AsciiCheckOff
    {
        get => _asciiCheckOff;
        set => _asciiCheckOff = Guard.NotEmpty(value, nameof(value));
    }

    /// <summary>ASCII marker of the selected radio option. Default <c>(*)</c>.</summary>
    public string AsciiRadioOn
    {
        get => _asciiRadioOn;
        set => _asciiRadioOn = Guard.NotEmpty(value, nameof(value));
    }

    /// <summary>ASCII marker of the other radio options. Default <c>( )</c>.</summary>
    public string AsciiRadioOff
    {
        get => _asciiRadioOff;
        set => _asciiRadioOff = Guard.NotEmpty(value, nameof(value));
    }

    internal PromptStyle ToPromptStyle() =>
        new PromptStyle { ShowFrame = false, BackgroundColor = BackgroundColor, HighlightColor = HighlightColor };

    internal PromptRow CreateRow(string label, MarkerShape shape, bool on, bool focused, bool enabled)
    {
        string marker = shape == MarkerShape.Check ? (on ? AsciiCheckOn : AsciiCheckOff) : (on ? AsciiRadioOn : AsciiRadioOff);
        bool active = enabled && focused;
        return new PromptRow
        {
            AsciiPrefix = (active ? AsciiPointer : ' ') + " " + marker + " ",
            Text = label,
            Shape = shape,
            Filled = on,
            MarkerColor = !enabled ? DisabledColor : (on || focused ? ActiveColor : InactiveColor),
            TextColor = !enabled ? DisabledColor : (focused ? (SixelColor?)ActiveColor : null),
            Bar = active,
        };
    }
}
