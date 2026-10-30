using System;

namespace SharpSixel.Ui;

/// <summary>Look of a <see cref="BarChart"/>: size, colors and ASCII characters.</summary>
public sealed class BarChartStyle
{
    private int _width = 40;
    private int _barHeightRows = 1;
    private double _barThickness = 0.6;
    private int _cornerRadius = 3;
    private SixelColor[] _palette =
    {
        new SixelColor(46, 204, 113),
        new SixelColor(52, 152, 219),
        new SixelColor(230, 126, 34),
        new SixelColor(231, 76, 60),
        new SixelColor(155, 89, 182),
        new SixelColor(241, 196, 15),
        new SixelColor(26, 188, 156),
        new SixelColor(149, 165, 166),
    };

    /// <summary>Width of the bar area in terminal cells (a full-scale bar is this wide). Default 40.</summary>
    public int Width
    {
        get => _width;
        set => _width = Guard.AtLeast(value, 1, nameof(value));
    }

    /// <summary>Rows each bar occupies in the sixel graphic (1-10). Default 1. ASCII always uses one row per bar.</summary>
    public int BarHeightRows
    {
        get => _barHeightRows;
        set => _barHeightRows = Guard.InRange(value, 1, 10, nameof(value));
    }

    /// <summary>Bar thickness as fraction of the row band of a bar (0.1-1). Default 0.6. Sixel only.</summary>
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

    /// <summary>Colors for items without own color, used in order and repeated (at least one). A copy is stored.</summary>
    public SixelColor[] Palette
    {
        get => (SixelColor[])_palette.Clone();
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length == 0)
            {
                throw new ArgumentException("The palette must contain at least one color.", nameof(value));
            }

            _palette = (SixelColor[])value.Clone();
        }
    }

    /// <summary>Color of the unused part of each bar. Transparent (default, alpha 0) draws no track. Sixel only.</summary>
    public SixelColor TrackColor { get; set; } = SixelColor.Transparent;

    /// <summary>
    /// Background of the sixel graphic. Transparent (default, alpha below 128) keeps the terminal background;
    /// the covered cells are erased before each frame. Sixel only.
    /// </summary>
    public SixelColor BackgroundColor { get; set; } = SixelColor.Transparent;

    /// <summary>ASCII character of the bar. Default '#'.</summary>
    public char AsciiFill { get; set; } = '#';

    /// <summary>ASCII character of the unused part of the bar area. Default ' '.</summary>
    public char AsciiEmpty { get; set; } = ' ';

    internal SixelColor PaletteColorAt(int index) => _palette[index % _palette.Length];
}
