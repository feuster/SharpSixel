using System;

namespace SharpSixel.Ui;

/// <summary>Look of the prompts (<see cref="Dialog"/>, <see cref="SelectionPrompt{T}"/>, <see cref="MultiSelectionPrompt{T}"/>, <see cref="TextPrompt"/>, the file dialogs): colors, frame and ASCII markers.</summary>
public sealed class PromptStyle
{
    private string _asciiRadioOn = "(*)";
    private string _asciiRadioOff = "( )";
    private string _asciiCheckOn = "[x]";
    private string _asciiCheckOff = "[ ]";
    private int _cornerRadius = 6;

    /// <summary>Color of the current item, selected markers and the text prompt marker (also used for ANSI text).</summary>
    public SixelColor ActiveColor { get; set; } = new SixelColor(52, 152, 219);

    /// <summary>Color of unselected markers and of the instruction line.</summary>
    public SixelColor InactiveColor { get; set; } = new SixelColor(149, 165, 166);

    /// <summary>Color of validation messages and their marker.</summary>
    public SixelColor ErrorColor { get; set; } = new SixelColor(231, 76, 60);

    /// <summary>
    /// Background of the sixel graphic. Transparent (default, alpha 0) keeps the terminal background; holes of markers and of
    /// the frame are punched transparent. An opaque color turns the prompt into a filled panel: the whole interior is filled
    /// and all text is drawn on this color. Sixel only.
    /// </summary>
    public SixelColor BackgroundColor { get; set; } = SixelColor.Transparent;

    /// <summary>True (default) to draw a rounded frame around the prompt (two more rows and two more columns). Sixel only.</summary>
    public bool ShowFrame { get; set; } = true;

    /// <summary>Color of the frame. Sixel only.</summary>
    public SixelColor FrameColor { get; set; } = new SixelColor(84, 96, 110);

    /// <summary>
    /// Color of the bar behind the current row of the selection prompts. Transparent (alpha 0) turns the bar off. Sixel only.
    /// </summary>
    public SixelColor HighlightColor { get; set; } = new SixelColor(38, 52, 70);

    /// <summary>Face color of the dialog buttons that are not selected (the selected one uses <see cref="ActiveColor"/>). Sixel only.</summary>
    public SixelColor ButtonColor { get; set; } = new SixelColor(64, 64, 64);

    /// <summary>Label color of the dialog buttons. Sixel only.</summary>
    public SixelColor ButtonTextColor { get; set; } = new SixelColor(255, 255, 255);

    /// <summary>Corner radius in pixels of the frame and the dialog buttons (0 = square). Default 6. Sixel only.</summary>
    public int CornerRadius
    {
        get => _cornerRadius;
        set => _cornerRadius = Guard.AtLeast(value, 0, nameof(value));
    }

    /// <summary>ASCII character in front of the current item. Default '&gt;'.</summary>
    public char AsciiPointer { get; set; } = '>';

    /// <summary>ASCII marker of the current item of a selection prompt. Default <c>(*)</c>.</summary>
    public string AsciiRadioOn
    {
        get => _asciiRadioOn;
        set => _asciiRadioOn = NotEmpty(value);
    }

    /// <summary>ASCII marker of the other items of a selection prompt. Default <c>( )</c>.</summary>
    public string AsciiRadioOff
    {
        get => _asciiRadioOff;
        set => _asciiRadioOff = NotEmpty(value);
    }

    /// <summary>ASCII marker of a selected item of a multi-selection prompt. Default <c>[x]</c>.</summary>
    public string AsciiCheckOn
    {
        get => _asciiCheckOn;
        set => _asciiCheckOn = NotEmpty(value);
    }

    /// <summary>ASCII marker of an unselected item of a multi-selection prompt. Default <c>[ ]</c>.</summary>
    public string AsciiCheckOff
    {
        get => _asciiCheckOff;
        set => _asciiCheckOff = NotEmpty(value);
    }

    internal string AsciiPrefix(bool pointer, string marker) => (pointer ? AsciiPointer : ' ') + " " + marker + " ";

    private static string NotEmpty(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("The marker must not be empty.", nameof(value));
        }

        return value;
    }
}
