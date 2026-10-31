using System;
using System.Globalization;

namespace SharpSixel.Ui;

/// <summary>Look of a <see cref="DateDisplay"/>: size, colors and date format.</summary>
public sealed class DateDisplayStyle
{
    private int _heightRows = 3;
    private int _lineThickness = 2;
    private int _cornerRadius = 4;
    private string? _dateFormat;

    /// <summary>Rows the sixel graphic occupies (1-20); the calendar page is square. Default 3.</summary>
    public int HeightRows
    {
        get => _heightRows;
        set => _heightRows = Guard.InRange(value, 1, 20, nameof(value));
    }

    /// <summary>Thickness of the outline and the line below the top part in pixels (1-4). Default 2. Sixel only.</summary>
    public int LineThickness
    {
        get => _lineThickness;
        set => _lineThickness = Guard.InRange(value, 1, 4, nameof(value));
    }

    /// <summary>Corner radius of the calendar page in pixels (0 = square). Default 4. Sixel only.</summary>
    public int CornerRadius
    {
        get => _cornerRadius;
        set => _cornerRadius = Guard.AtLeast(value, 0, nameof(value));
    }

    /// <summary>
    /// Culture of the date text (weekday and month names, date order). Null (default) uses the current culture of the
    /// system.
    /// </summary>
    public CultureInfo? Culture { get; set; }

    /// <summary>
    /// .NET date and time format of the date text, applied with <see cref="Culture"/>. Null (default) = localized
    /// default: abbreviated weekday and short date of the culture (for example <c>Wed, 10/28/2026</c> or
    /// <c>Mi, 28.10.2026</c>). An empty or invalid format is rejected with an <see cref="ArgumentException"/> or a
    /// <see cref="FormatException"/>.
    /// </summary>
    public string? DateFormat
    {
        get => _dateFormat;
        set
        {
            if (value != null)
            {
                if (value.Length == 0)
                {
                    throw new ArgumentException("The format must not be empty.", nameof(value));
                }

                _ = DateTime.MinValue.ToString(value, CultureInfo.InvariantCulture);
            }

            _dateFormat = value;
        }
    }

    internal string Format(DateTime date)
    {
        CultureInfo culture = Culture ?? CultureInfo.CurrentCulture;
        return _dateFormat != null
            ? date.ToString(_dateFormat, culture)
            : date.ToString("ddd", culture).TrimEnd('.') + ", " + date.ToString("d", culture);
    }

    /// <summary>Color of the outline and the line below the top part. Sixel only.</summary>
    public SixelColor BorderColor { get; set; } = new SixelColor(128, 128, 128);

    /// <summary>Color of the two binder rings on top. Sixel only.</summary>
    public SixelColor AccentColor { get; set; } = new SixelColor(231, 76, 60);

    /// <summary>Color of the day number. Sixel only.</summary>
    public SixelColor DigitColor { get; set; } = new SixelColor(52, 152, 219);

    /// <summary>Fill of the calendar page. Transparent (default, alpha below 128) keeps the terminal background. Sixel only.</summary>
    public SixelColor PaperColor { get; set; } = SixelColor.Transparent;

    /// <summary>Text color of the date text. Alpha below 128 (default) keeps the terminal text color.</summary>
    public SixelColor TextColor { get; set; } = SixelColor.Transparent;

    /// <summary>
    /// Background of the sixel graphic. Transparent (default, alpha below 128) keeps the terminal background;
    /// the covered cells are erased before each frame. Sixel only.
    /// </summary>
    public SixelColor BackgroundColor { get; set; } = SixelColor.Transparent;
}
