using System;

namespace SharpSixel.Ui;

/// <summary>Look of a <see cref="Tree"/>: guide lines, colors and ASCII characters.</summary>
public sealed class TreeStyle
{
    private int _lineThickness = 1;
    private string _collapsedSuffix = " [+]";

    /// <summary>Thickness of the guide lines in pixels (1-4). Default 1. Sixel only.</summary>
    public int LineThickness
    {
        get => _lineThickness;
        set => _lineThickness = Guard.InRange(value, 1, 4, nameof(value));
    }

    /// <summary>Text appended to a collapsed node that has children. Default <c>" [+]"</c>; empty for none.</summary>
    public string CollapsedSuffix
    {
        get => _collapsedSuffix;
        set => _collapsedSuffix = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Color of the guide lines (pixel lines in sixel mode, ANSI color of the guide characters in ASCII mode). Alpha below 128 draws no color.</summary>
    public SixelColor GuideColor { get; set; } = new SixelColor(128, 128, 128);

    /// <summary>Text color of the title. Alpha below 128 keeps the terminal text color.</summary>
    public SixelColor TitleColor { get; set; } = new SixelColor(52, 152, 219);

    /// <summary>ASCII character of vertical guides. Default '|'.</summary>
    public char AsciiVertical { get; set; } = '|';

    /// <summary>ASCII character in front of a node that has following siblings. Default '+'.</summary>
    public char AsciiBranch { get; set; } = '+';

    /// <summary>ASCII character in front of the last node of a level. Default '`'.</summary>
    public char AsciiLastBranch { get; set; } = '`';

    /// <summary>ASCII character of horizontal guides. Default '-'.</summary>
    public char AsciiHorizontal { get; set; } = '-';
}
