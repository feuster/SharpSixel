using System;

namespace SharpSixel.Ui;

/// <summary>Look of a <see cref="Spinner"/>: size, segments, colors and ASCII frames.</summary>
public sealed class SpinnerStyle
{
    private int _heightRows = 1;
    private int _segmentCount = 8;
    private int _trailLength = 4;
    private double _dotScale = 0.25;
    private string[] _asciiFrames = { "|", "/", "-", "\\" };

    /// <summary>Rows the sixel graphic occupies (1-20); the graphic is square. Default 1.</summary>
    public int HeightRows
    {
        get => _heightRows;
        set => _heightRows = Guard.InRange(value, 1, 20, nameof(value));
    }

    /// <summary>Number of dots on the ring (3-24). Default 8. Sixel only.</summary>
    public int SegmentCount
    {
        get => _segmentCount;
        set => _segmentCount = Guard.InRange(value, 3, 24, nameof(value));
    }

    /// <summary>Number of dots (head included) that fade from active to inactive color (1-24). Default 4. Sixel only.</summary>
    public int TrailLength
    {
        get => _trailLength;
        set => _trailLength = Guard.InRange(value, 1, 24, nameof(value));
    }

    /// <summary>Dot radius as fraction of the graphic's half size (0.1-0.5). Default 0.25. Sixel only.</summary>
    public double DotScale
    {
        get => _dotScale;
        set => _dotScale = Guard.InRange(value, 0.1, 0.5, nameof(value));
    }

    /// <summary>Color of the head dot (and of the ASCII frame).</summary>
    public SixelColor ActiveColor { get; set; } = new SixelColor(52, 152, 219);

    /// <summary>Color of dots outside the trail.</summary>
    public SixelColor InactiveColor { get; set; } = new SixelColor(64, 64, 64);

    /// <summary>
    /// Background of the sixel graphic. Transparent (default, alpha below 128) keeps the terminal background;
    /// the covered cells are erased before each frame. Sixel only.
    /// </summary>
    public SixelColor BackgroundColor { get; set; } = SixelColor.Transparent;

    /// <summary>ASCII animation frames (at least one, none empty). Default <c>| / - \</c>. A copy is stored.</summary>
    public string[] AsciiFrames
    {
        get => (string[])_asciiFrames.Clone();
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length == 0 || Array.Exists(value, string.IsNullOrEmpty))
            {
                throw new ArgumentException("Frames must not be empty.", nameof(value));
            }

            _asciiFrames = (string[])value.Clone();
        }
    }

    internal int AsciiFrameCount => _asciiFrames.Length;

    internal string AsciiFrameAt(int index) => _asciiFrames[index % _asciiFrames.Length];
}
