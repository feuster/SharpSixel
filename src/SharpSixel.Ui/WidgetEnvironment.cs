using System;

namespace SharpSixel.Ui;

/// <summary>Immutable rendering context of widgets: sixel or ASCII, cell geometry, ANSI usage.</summary>
public sealed class WidgetEnvironment
{
    /// <summary>Fallback cell width in pixels if the terminal does not report it.</summary>
    public const int DefaultCellWidth = 10;

    /// <summary>Fallback cell height in pixels if the terminal does not report it.</summary>
    public const int DefaultCellHeight = 20;

    /// <summary>Creates an environment.</summary>
    /// <param name="useSixel">True to draw sixel graphics, false for ASCII.</param>
    /// <param name="cellWidth">Character cell width in pixels.</param>
    /// <param name="cellHeight">Character cell height in pixels.</param>
    /// <param name="useAnsi">True if ANSI sequences (colors, erase) may be used in ASCII frames.</param>
    /// <param name="columns">Width of the terminal in character cells; 0 = unknown (widgets are not limited).</param>
    public WidgetEnvironment(bool useSixel, int cellWidth = DefaultCellWidth, int cellHeight = DefaultCellHeight, bool useAnsi = true, int columns = 0)
    {
        UseSixel = useSixel;
        CellWidth = Guard.AtLeast(cellWidth, 1, nameof(cellWidth));
        CellHeight = Guard.AtLeast(cellHeight, 1, nameof(cellHeight));
        UseAnsi = useAnsi;
        Columns = Guard.AtLeast(columns, 0, nameof(columns));
    }

    /// <summary>True if widgets are drawn as sixel graphics.</summary>
    public bool UseSixel { get; }

    /// <summary>Character cell width in pixels.</summary>
    public int CellWidth { get; }

    /// <summary>Character cell height in pixels.</summary>
    public int CellHeight { get; }

    /// <summary>True if ANSI sequences (colors, erase) may be used in ASCII frames.</summary>
    public bool UseAnsi { get; }

    /// <summary>
    /// Width of the terminal in character cells, or 0 if unknown. Widgets with a flexible width (table, panel, tree,
    /// bar chart, progress bar) shrink to fit; 0 means no limit.
    /// </summary>
    public int Columns { get; }

    /// <summary>
    /// Pixel height of a sixel graphic spanning <paramref name="rows"/> rows: the largest multiple of 6 that fits,
    /// because sixel data is built in bands of 6 pixels and a partial band would spill into an extra row.
    /// </summary>
    public int GetImageHeight(int rows) => Math.Max(6, rows * CellHeight / 6 * 6);

    /// <summary>Rows of the layout this widget is placed in, 0 if none (lets a vertical <see cref="Rule"/> stretch).</summary>
    internal int Rows { get; private set; }

    /// <summary>Copy of this environment for a widget placed inside a layout: the terminal width and the rows left to it.</summary>
    internal WidgetEnvironment WithSpace(int columns, int rows) =>
        new WidgetEnvironment(UseSixel, CellWidth, CellHeight, UseAnsi, columns) { Rows = rows };

    /// <summary>Creates an environment from a render mode and the queried terminal capabilities.</summary>
    /// <param name="mode">Requested mode; <see cref="RenderMode.Auto"/> follows <paramref name="terminal"/>.</param>
    /// <param name="terminal">Result of <see cref="SixelTerminal.Query"/>, or null if unknown (then ASCII in Auto mode).</param>
    /// <param name="useAnsi">True if ANSI sequences may be used in ASCII frames.</param>
    /// <param name="columns">Terminal width in cells; 0 derives it from the text area and cell size in <paramref name="terminal"/> (unknown if not reported).</param>
    public static WidgetEnvironment Create(RenderMode mode, SixelTerminalInfo? terminal, bool useAnsi = true, int columns = 0)
    {
        bool useSixel;
        switch (mode)
        {
            case RenderMode.Auto:
                useSixel = terminal != null && terminal.SupportsSixel;
                break;
            case RenderMode.Sixel:
                useSixel = true;
                break;
            case RenderMode.Ascii:
                useSixel = false;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }

        bool hasCell = terminal != null && terminal.HasCellSize;
        if (columns <= 0 && hasCell && terminal!.HasTextAreaSize)
        {
            columns = terminal.TextAreaWidth / terminal.CellWidth;
        }
        return new WidgetEnvironment(
            useSixel,
            hasCell ? terminal!.CellWidth : DefaultCellWidth,
            hasCell ? terminal!.CellHeight : DefaultCellHeight,
            useAnsi,
            Math.Max(0, columns));
    }
}
