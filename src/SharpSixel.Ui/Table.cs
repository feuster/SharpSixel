using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SharpSixel.Ui;

/// <summary>
/// Table with column headers and text cells, optional caption on top. Sixel: the grid is drawn as thin pixel lines,
/// headers and cell texts are terminal text (header and stripe backgrounds via ANSI). ASCII: grid of <c>+ - |</c>.
/// Both modes use the same rows: caption, top rule, header, header rule, one row per data row, bottom rule. The number of
/// lines must not grow beyond the rows reserved in a <see cref="LiveRegion"/> (<see cref="Widget.GetRows"/> at
/// <see cref="LiveRegion.Begin"/>); fixed <see cref="TableColumn.Width"/> values keep the size stable while cells change.
/// Text is assumed to occupy one cell per char.
/// </summary>
public sealed class Table : Widget
{
    private const int ColorAlphaLimit = 128;
    private const string Ellipsis = "\u2026";
    private readonly Collection<TableColumn> _columns = new ColumnCollection();
    private readonly Collection<string[]> _rows = new RowCollection();

    /// <summary>Creates a table with the default style.</summary>
    public Table()
        : this(new TableStyle())
    {
    }

    /// <summary>Creates a table with the given style.</summary>
    public Table(TableStyle style)
    {
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    /// <summary>Look of the table; may be changed between frames.</summary>
    public TableStyle Style { get; }

    /// <summary>Optional caption shown on a row above the table.</summary>
    public string? Caption { get; set; }

    /// <summary>The columns (null columns are rejected).</summary>
    public IList<TableColumn> Columns => _columns;

    /// <summary>The data rows: one text per column (missing cells are empty, extra cells are ignored; null rows are rejected).</summary>
    public IList<string[]> Rows => _rows;

    /// <summary>Adds a column and returns it.</summary>
    public TableColumn AddColumn(string header, TableAlignment alignment = TableAlignment.Left, int? width = null)
    {
        var column = new TableColumn(header, alignment, width);
        _columns.Add(column);
        return column;
    }

    /// <summary>Adds a row and returns it (change its cells later to update the table).</summary>
    public string[] AddRow(params string[] cells)
    {
        if (cells == null)
        {
            throw new ArgumentNullException(nameof(cells));
        }

        _rows.Add(cells);
        return cells;
    }

    private int CaptionRows => string.IsNullOrEmpty(Caption) ? 0 : 1;

    /// <inheritdoc />
    protected override int SixelRows => Math.Max(1, BuildLayout(0).Lines.Count);

    /// <inheritdoc />
    protected override int AsciiRows => Math.Max(1, BuildLayout(0).Lines.Count);

    /// <inheritdoc />
    protected override string RenderAscii(WidgetEnvironment environment)
    {
        Layout layout = BuildLayout(environment.Columns);
        var lines = new List<string>();
        foreach (Line line in layout.Lines)
        {
            switch (line.Kind)
            {
                case LineKind.Caption:
                    lines.Add(Caption!);
                    break;
                case LineKind.Header:
                case LineKind.Row:
                    lines.Add(AsciiTextLine(layout, line, environment.UseAnsi));
                    break;
                default:
                    lines.Add(AsciiRule(layout, environment.UseAnsi));
                    break;
            }
        }

        return string.Join("\n", lines);
    }

    /// <inheritdoc />
    protected override string RenderSixel(WidgetEnvironment environment)
    {
        Layout layout = BuildLayout(environment.Columns);
        int first = CaptionRows;
        int tableRows = layout.Lines.Count - first;
        var sb = new StringBuilder(AnsiCodes.SaveCursor);
        if (first > 0)
        {
            sb.Append(Caption).Append(AnsiCodes.EraseLineRest).Append(AnsiCodes.RestoreCursor);
        }

        if (tableRows > 0)
        {
            for (int row = first; row < layout.Lines.Count; row++)
            {
                sb.Append(AnsiCodes.Down(row)).Append(AnsiCodes.EraseChars(layout.Total)).Append(AnsiCodes.Forward(layout.Total))
                    .Append(AnsiCodes.EraseLineRest).Append(AnsiCodes.RestoreCursor);
            }

            sb.Append(AnsiCodes.Down(first)).Append(new SixelEncoder().Encode(CreateImage(environment))).Append(AnsiCodes.RestoreCursor);
            for (int row = first; row < layout.Lines.Count; row++)
            {
                Line line = layout.Lines[row];
                if (line.Kind != LineKind.Header && line.Kind != LineKind.Row)
                {
                    continue;
                }

                bool header = line.Kind == LineKind.Header;
                SixelColor foreground = header ? Style.HeaderTextColor : SixelColor.Transparent;
                SixelColor background = header ? Style.HeaderBackgroundColor
                    : (line.Index % 2 == 1 ? Style.StripeColor : SixelColor.Transparent);
                for (int i = 0; i < _columns.Count; i++)
                {
                    sb.Append(AnsiCodes.Down(row)).Append(AnsiCodes.Forward(layout.Starts[i]))
                        .Append(Paint(Cell(layout, line, i), foreground, background)).Append(AnsiCodes.RestoreCursor);
                }
            }
        }

        return sb.Append(AnsiCodes.RestoreCursor).ToString();
    }

    internal SixelImage CreateImage(WidgetEnvironment environment)
    {
        Layout layout = BuildLayout(environment.Columns);
        int first = CaptionRows;
        int rows = Math.Max(1, layout.Lines.Count - first);
        int cellWidth = environment.CellWidth;
        int width = Math.Max(1, layout.Total) * cellWidth;
        int height = environment.GetImageHeight(rows);
        var image = new SixelImage(width, height);
        SixelColor color = Style.BorderColor;
        if (color.A == 0 || layout.Total == 0 || layout.Lines.Count <= first)
        {
            return image;
        }

        double band = (double)height / rows;
        int thickness = Style.LineThickness;
        int half = thickness / 2;
        bool border = Style.ShowBorder;
        int RowCenter(int row) => (int)Math.Round((row + 0.5) * band, MidpointRounding.AwayFromZero);
        int ColumnCenter(int column) => (int)Math.Round((column + 0.5) * cellWidth, MidpointRounding.AwayFromZero);

        int top = border ? RowCenter(0) - half : 0;
        int bottom = border ? RowCenter(rows - 1) - half + thickness : height;
        foreach (int separator in layout.Separators)
        {
            Raster.FillRoundedRect(image, ColumnCenter(separator) - half, top, thickness, bottom - top, 0, color);
        }

        int left = border ? ColumnCenter(layout.Separators[0]) - half : 0;
        int right = border ? ColumnCenter(layout.Separators[layout.Separators.Length - 1]) - half + thickness : width;
        for (int row = 0; row < rows; row++)
        {
            if (IsRule(layout.Lines[first + row].Kind))
            {
                Raster.FillRoundedRect(image, left, RowCenter(row) - half, right - left, thickness, 0, color);
            }
        }

        return image;
    }

    private static bool IsRule(LineKind kind) =>
        kind == LineKind.TopRule || kind == LineKind.HeaderRule || kind == LineKind.RowRule || kind == LineKind.BottomRule;

    private static string Tint(string text, SixelColor color, bool ansi) =>
        color.A >= ColorAlphaLimit ? AnsiCodes.Colorize(text, color, ansi) : text;

    private static string Paint(string text, SixelColor foreground, SixelColor background)
    {
        string prefix = (foreground.A >= ColorAlphaLimit ? AnsiCodes.Foreground(foreground) : string.Empty)
            + (background.A >= ColorAlphaLimit ? AnsiCodes.Background(background) : string.Empty);
        return prefix.Length == 0 ? text : prefix + text + AnsiCodes.Reset;
    }

    private string AsciiRule(Layout layout, bool ansi)
    {
        char[] cells = new string(Style.AsciiHorizontal, layout.Total).ToCharArray();
        foreach (int separator in layout.Separators)
        {
            cells[separator] = Style.AsciiCorner;
        }

        return Tint(new string(cells), Style.BorderColor, ansi);
    }

    private string AsciiTextLine(Layout layout, Line line, bool ansi)
    {
        bool header = line.Kind == LineKind.Header;
        string vertical = Tint(Style.AsciiVertical.ToString(), Style.BorderColor, ansi);
        var sb = new StringBuilder();
        if (Style.ShowBorder)
        {
            sb.Append(vertical);
        }

        for (int i = 0; i < _columns.Count; i++)
        {
            string cell = Cell(layout, line, i);
            sb.Append(header ? Tint(cell, Style.HeaderTextColor, ansi) : cell);
            if (i < _columns.Count - 1 && Style.ShowColumnSeparators)
            {
                sb.Append(vertical);
            }
        }

        if (Style.ShowBorder)
        {
            sb.Append(vertical);
        }

        return sb.ToString();
    }

    private string Cell(Layout layout, Line line, int column)
    {
        string text = line.Kind == LineKind.Header ? PromptRow.Clean(_columns[column].Header) : CellText(_rows[line.Index], column);
        int width = layout.Widths[column];
        if (text.Length > width)
        {
            text = width > 1 ? text.Substring(0, width - 1) + Ellipsis : text.Substring(0, width);
        }

        int gap = width - text.Length;
        int before = 0;
        switch (_columns[column].Alignment)
        {
            case TableAlignment.Right:
                before = gap;
                break;
            case TableAlignment.Center:
                before = gap / 2;
                break;
        }

        string padding = new string(' ', Style.Padding);
        return padding + new string(' ', before) + text + new string(' ', gap - before) + padding;
    }

    private static string CellText(string[] row, int column) =>
        column < row.Length ? PromptRow.Clean(row[column]) : string.Empty;

    private int ContentWidth(int column)
    {
        TableColumn definition = _columns[column];
        if (definition.Width.HasValue)
        {
            return definition.Width.Value;
        }

        int width = 1;
        if (Style.ShowHeader)
        {
            width = Math.Max(width, PromptRow.Clean(definition.Header).Length);
        }

        foreach (string[] row in _rows)
        {
            width = Math.Max(width, CellText(row, column).Length);
        }

        return width;
    }

    /// <summary>Column widths; if <paramref name="maxColumns"/> is positive the widest columns shrink until the table fits.</summary>
    private int[] FitWidths(int count, bool border, int maxColumns)
    {
        var widths = new int[count];
        int total = 0;
        for (int i = 0; i < count; i++)
        {
            widths[i] = ContentWidth(i);
            total += widths[i];
        }

        if (maxColumns <= 0 || count == 0)
        {
            return widths;
        }

        total += (border ? 2 : 0) + (count * 2 * Style.Padding) + (Style.ShowColumnSeparators ? count - 1 : 0);
        while (total > maxColumns)
        {
            int widest = 0;
            for (int i = 1; i < count; i++)
            {
                if (widths[i] > widths[widest])
                {
                    widest = i;
                }
            }

            if (widths[widest] <= 1)
            {
                break;
            }

            widths[widest]--;
            total--;
        }

        return widths;
    }

    private Layout BuildLayout(int maxColumns)
    {
        int count = _columns.Count;
        bool border = count > 0 && Style.ShowBorder;
        int[] widths = FitWidths(count, border, maxColumns);
        var layout = new Layout { Widths = new int[count], Starts = new int[count] };
        var separators = new List<int>();
        int position = 0;
        if (border)
        {
            separators.Add(position++);
        }

        for (int i = 0; i < count; i++)
        {
            layout.Widths[i] = widths[i];
            layout.Starts[i] = position;
            position += layout.Widths[i] + (2 * Style.Padding);
            if (i < count - 1 && Style.ShowColumnSeparators)
            {
                separators.Add(position++);
            }
        }

        if (border)
        {
            separators.Add(position++);
        }

        layout.Separators = separators.ToArray();
        layout.Total = position;

        if (CaptionRows > 0)
        {
            layout.Lines.Add(new Line(LineKind.Caption, 0));
        }

        if (count > 0)
        {
            if (border)
            {
                layout.Lines.Add(new Line(LineKind.TopRule, 0));
            }

            if (Style.ShowHeader)
            {
                layout.Lines.Add(new Line(LineKind.Header, 0));
                layout.Lines.Add(new Line(LineKind.HeaderRule, 0));
            }

            for (int row = 0; row < _rows.Count; row++)
            {
                if (row > 0 && Style.ShowRowSeparators)
                {
                    layout.Lines.Add(new Line(LineKind.RowRule, row));
                }

                layout.Lines.Add(new Line(LineKind.Row, row));
            }

            if (border)
            {
                layout.Lines.Add(new Line(LineKind.BottomRule, 0));
            }
        }

        return layout;
    }

    private enum LineKind
    {
        Caption,
        TopRule,
        Header,
        HeaderRule,
        Row,
        RowRule,
        BottomRule,
    }

    private readonly struct Line
    {
        public Line(LineKind kind, int index)
        {
            Kind = kind;
            Index = index;
        }

        public LineKind Kind { get; }

        public int Index { get; }
    }

    private sealed class Layout
    {
        public int[] Widths { get; set; } = new int[0];

        /// <summary>Cell offset of the padded cell of each column.</summary>
        public int[] Starts { get; set; } = new int[0];

        /// <summary>Cell offsets of the vertical lines (frame and column separators).</summary>
        public int[] Separators { get; set; } = new int[0];

        public int Total { get; set; }

        public List<Line> Lines { get; } = new List<Line>();
    }

    private sealed class ColumnCollection : Collection<TableColumn>
    {
        protected override void InsertItem(int index, TableColumn item) => base.InsertItem(index, Check(item));

        protected override void SetItem(int index, TableColumn item) => base.SetItem(index, Check(item));

        private static TableColumn Check(TableColumn item) => item ?? throw new ArgumentNullException(nameof(item));
    }

    private sealed class RowCollection : Collection<string[]>
    {
        protected override void InsertItem(int index, string[] item) => base.InsertItem(index, Check(item));

        protected override void SetItem(int index, string[] item) => base.SetItem(index, Check(item));

        private static string[] Check(string[] item) => item ?? throw new ArgumentNullException(nameof(item));
    }
}
