using System;
using System.Globalization;

namespace SharpSixel.Ui;

/// <summary>Look of a <see cref="Clock"/>: size, colors and time format.</summary>
public sealed class ClockStyle
{
    private int _heightRows = 3;
    private int _lineThickness = 2;
    private string _timeFormat = "HH:mm:ss";

    /// <summary>Rows the sixel graphic occupies (1-20); the clock face is square. Default 3.</summary>
    public int HeightRows
    {
        get => _heightRows;
        set => _heightRows = Guard.InRange(value, 1, 20, nameof(value));
    }

    /// <summary>Thickness of the rim in pixels (1-4). Default 2. Sixel only.</summary>
    public int LineThickness
    {
        get => _lineThickness;
        set => _lineThickness = Guard.InRange(value, 1, 4, nameof(value));
    }

    /// <summary>True (default) to draw the second hand. Sixel only; the digital text is defined by <see cref="TimeFormat"/>.</summary>
    public bool ShowSecondHand { get; set; } = true;

    /// <summary>
    /// .NET date and time format of the digital time text (invariant culture). Default <c>HH:mm:ss</c>.
    /// A format that is not valid is rejected with a <see cref="FormatException"/>.
    /// </summary>
    public string TimeFormat
    {
        get => _timeFormat;
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length == 0)
            {
                throw new ArgumentException("The format must not be empty.", nameof(value));
            }

            _ = DateTime.MinValue.ToString(value, CultureInfo.InvariantCulture);
            _timeFormat = value;
        }
    }

    /// <summary>Color of the rim. Sixel only.</summary>
    public SixelColor BorderColor { get; set; } = new SixelColor(128, 128, 128);

    /// <summary>Fill of the clock face. Transparent (default, alpha below 128) keeps the terminal background. Sixel only.</summary>
    public SixelColor FaceColor { get; set; } = SixelColor.Transparent;

    /// <summary>Color of the 12 hour marks. Sixel only.</summary>
    public SixelColor TickColor { get; set; } = new SixelColor(128, 128, 128);

    /// <summary>Color of the hour and minute hands. Sixel only.</summary>
    public SixelColor HandColor { get; set; } = new SixelColor(52, 152, 219);

    /// <summary>Color of the second hand. Sixel only.</summary>
    public SixelColor SecondHandColor { get; set; } = new SixelColor(231, 76, 60);

    /// <summary>Text color of the digital time. Alpha below 128 (default) keeps the terminal text color.</summary>
    public SixelColor TextColor { get; set; } = SixelColor.Transparent;

    /// <summary>
    /// Background of the sixel graphic. Transparent (default, alpha below 128) keeps the terminal background;
    /// the covered cells are erased before each frame. Sixel only.
    /// </summary>
    public SixelColor BackgroundColor { get; set; } = SixelColor.Transparent;
}
