using System;

namespace SharpSixel.Ui;

/// <summary>Look of an <see cref="ImageView"/>: frame, background and the ASCII fallback characters.</summary>
public sealed class ImageViewStyle
{
    private int _borderThickness = 2;
    private string _asciiRamp = " .:-=+*#%@";

    /// <summary>True to draw a frame around the view. Default false.</summary>
    public bool ShowBorder { get; set; }

    /// <summary>Color of the frame (sixel and ANSI ASCII).</summary>
    public SixelColor BorderColor { get; set; } = new SixelColor(149, 165, 166);

    /// <summary>Frame thickness in pixels (at least 1). Sixel only; the ASCII frame is one character wide. Default 2.</summary>
    public int BorderThickness
    {
        get => _borderThickness;
        set => _borderThickness = Guard.AtLeast(value, 1, nameof(value));
    }

    /// <summary>
    /// Background of the view inside the frame, independent of the terminal background. Transparent (default, alpha 0)
    /// keeps the terminal background; semi-transparent image pixels are blended onto an opaque background. Sixel only.
    /// </summary>
    public SixelColor BackgroundColor { get; set; } = SixelColor.Transparent;

    /// <summary>ASCII frame corner character. Default '+'.</summary>
    public char AsciiCorner { get; set; } = '+';

    /// <summary>ASCII frame character of the top and bottom edge. Default '-'.</summary>
    public char AsciiHorizontal { get; set; } = '-';

    /// <summary>ASCII frame character of the left and right edge. Default '|'.</summary>
    public char AsciiVertical { get; set; } = '|';

    /// <summary>Characters from dark to bright; one image pixel becomes one cell with the character of its brightness. Default <c> .:-=+*#%@</c>.</summary>
    public string AsciiRamp
    {
        get => _asciiRamp;
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length == 0)
            {
                throw new ArgumentException("The ramp must not be empty.", nameof(value));
            }

            _asciiRamp = value;
        }
    }
}
