namespace SharpSixel;

/// <summary>Capabilities and geometry reported by a terminal; a value of 0 means "not reported".</summary>
public sealed class SixelTerminalInfo
{
    /// <summary>Creates the info object.</summary>
    /// <param name="supportsSixel">True if the terminal lists sixel graphics in its device attributes.</param>
    /// <param name="textAreaWidth">Width of the text area in pixels, or 0.</param>
    /// <param name="textAreaHeight">Height of the text area in pixels, or 0.</param>
    /// <param name="cellWidth">Width of one character cell in pixels, or 0.</param>
    /// <param name="cellHeight">Height of one character cell in pixels, or 0.</param>
    /// <param name="colorRegisters">Number of sixel color registers, or 0.</param>
    /// <param name="graphicsWidth">Maximum sixel graphics width in pixels, or 0.</param>
    /// <param name="graphicsHeight">Maximum sixel graphics height in pixels, or 0.</param>
    /// <param name="sixelDisplayMode">State of DECSDM (mode 80).</param>
    /// <param name="privateColorRegisters">State of mode 1070.</param>
    public SixelTerminalInfo(
        bool supportsSixel,
        int textAreaWidth,
        int textAreaHeight,
        int cellWidth,
        int cellHeight,
        int colorRegisters = 0,
        int graphicsWidth = 0,
        int graphicsHeight = 0,
        TerminalModeState sixelDisplayMode = TerminalModeState.NotRecognized,
        TerminalModeState privateColorRegisters = TerminalModeState.NotRecognized)
    {
        SupportsSixel = supportsSixel;
        TextAreaWidth = textAreaWidth;
        TextAreaHeight = textAreaHeight;
        CellWidth = cellWidth;
        CellHeight = cellHeight;
        ColorRegisters = colorRegisters;
        GraphicsWidth = graphicsWidth;
        GraphicsHeight = graphicsHeight;
        SixelDisplayMode = sixelDisplayMode;
        PrivateColorRegisters = privateColorRegisters;
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

    /// <summary>Number of sixel color registers (XTSMGRAPHICS item 1), or 0 if the terminal did not report it.</summary>
    public int ColorRegisters { get; }

    /// <summary>Maximum width of a sixel graphic in pixels (XTSMGRAPHICS item 2), or 0.</summary>
    public int GraphicsWidth { get; }

    /// <summary>Maximum height of a sixel graphic in pixels (XTSMGRAPHICS item 2), or 0.</summary>
    public int GraphicsHeight { get; }

    /// <summary>
    /// State of the sixel display mode DECSDM (mode 80): <see cref="TerminalModeState.Set"/> means sixel scrolling is disabled
    /// (the cursor does not move with the graphic), <see cref="TerminalModeState.Reset"/> means it is enabled.
    /// </summary>
    public TerminalModeState SixelDisplayMode { get; }

    /// <summary>State of mode 1070: set means every graphic uses private color registers.</summary>
    public TerminalModeState PrivateColorRegisters { get; }

    /// <summary>True if the text area size in pixels is known.</summary>
    public bool HasTextAreaSize => TextAreaWidth > 0 && TextAreaHeight > 0;

    /// <summary>True if the cell size in pixels is known.</summary>
    public bool HasCellSize => CellWidth > 0 && CellHeight > 0;

    /// <summary>True if the number of color registers is known.</summary>
    public bool HasColorRegisters => ColorRegisters > 0;

    /// <summary>True if the maximum graphics size in pixels is known.</summary>
    public bool HasGraphicsSize => GraphicsWidth > 0 && GraphicsHeight > 0;
}