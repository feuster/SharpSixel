namespace Sixel;

/// <summary>Capabilities and geometry reported by a terminal; a value of 0 means "not reported".</summary>
public sealed class SixelTerminalInfo
{
    /// <summary>Creates the info object.</summary>
    public SixelTerminalInfo(bool supportsSixel, int textAreaWidth, int textAreaHeight, int cellWidth, int cellHeight)
    {
        SupportsSixel = supportsSixel;
        TextAreaWidth = textAreaWidth;
        TextAreaHeight = textAreaHeight;
        CellWidth = cellWidth;
        CellHeight = cellHeight;
    }

    /// <summary>True if the terminal lists sixel graphics in its device attributes.</summary>
    public bool SupportsSixel { get; }

    /// <summary>Width of the text area in pixels, or 0.</summary>
    public int TextAreaWidth { get; }

    /// <summary>Height of the text area in pixels, or 0.</summary>
    public int TextAreaHeight { get; }

    /// <summary>Width of one character cell in pixels, or 0.</summary>
    public int CellWidth { get; }

    /// <summary>Height of one character cell in pixels, or 0.</summary>
    public int CellHeight { get; }

    /// <summary>True if the text area size in pixels is known.</summary>
    public bool HasTextAreaSize => TextAreaWidth > 0 && TextAreaHeight > 0;

    /// <summary>True if the cell size in pixels is known.</summary>
    public bool HasCellSize => CellWidth > 0 && CellHeight > 0;
}
