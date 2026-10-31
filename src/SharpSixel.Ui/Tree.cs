using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SharpSixel.Ui;

/// <summary>
/// Hierarchical list of <see cref="TreeNode"/>s with guide lines and an optional title as root. Sixel: the guides are drawn
/// as thin pixel lines in their own cells, title and node texts are terminal text. ASCII: guides of <c>| + ` -</c>.
/// Both modes use the same rows: the title (if any), then one row per visible node (children of collapsed nodes are
/// hidden). The number of rows must not grow beyond the rows reserved in a <see cref="LiveRegion"/>
/// (<see cref="Widget.GetRows"/> at <see cref="LiveRegion.Begin"/>). Text is assumed to occupy one cell per char and
/// should fit on one terminal row.
/// </summary>
public sealed class Tree : Widget
{
    private const int ColorAlphaLimit = 128;
    private const int GuideCells = 4;
    private readonly Collection<TreeNode> _nodes = new TreeNode.NodeCollection();

    /// <summary>Creates a tree with the default style.</summary>
    public Tree()
        : this(new TreeStyle())
    {
    }

    /// <summary>Creates a tree with the given style.</summary>
    public Tree(TreeStyle style)
    {
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    /// <summary>Look of the tree; may be changed between frames.</summary>
    public TreeStyle Style { get; }

    /// <summary>Optional title shown as root row above the nodes. Without a title the top-level nodes start at the left edge.</summary>
    public string? Title { get; set; }

    /// <summary>The top-level nodes (null nodes are rejected).</summary>
    public IList<TreeNode> Nodes => _nodes;

    /// <summary>Adds a top-level node and returns it.</summary>
    public TreeNode AddNode(string text)
    {
        var node = new TreeNode(text);
        _nodes.Add(node);
        return node;
    }

    /// <inheritdoc />
    protected override int SixelRows => Math.Max(1, BuildLayout(0).TotalRows);

    /// <inheritdoc />
    protected override int AsciiRows => Math.Max(1, BuildLayout(0).TotalRows);

    /// <inheritdoc />
    protected override string RenderAscii(WidgetEnvironment environment)
    {
        Layout layout = BuildLayout(environment.Columns);
        bool ansi = environment.UseAnsi;
        var lines = new List<string>();
        if (layout.HasTitle)
        {
            lines.Add(Tint(layout.Title, Style.TitleColor, ansi));
        }

        foreach (Row row in layout.Rows)
        {
            lines.Add(AsciiGuides(row, ansi) + row.Text);
        }

        return string.Join("\n", lines);
    }

    /// <inheritdoc />
    protected override string RenderSixel(WidgetEnvironment environment)
    {
        Layout layout = BuildLayout(environment.Columns);
        int rows = Math.Max(1, layout.TotalRows);
        var sb = new StringBuilder(AnsiCodes.SaveCursor);
        for (int row = 0; row < rows; row++)
        {
            sb.Append(AnsiCodes.Down(row)).Append(AnsiCodes.EraseLineRest).Append(AnsiCodes.RestoreCursor);
        }

        if (layout.MaxColumns > 0)
        {
            sb.Append(new SixelEncoder().Encode(CreateImage(environment))).Append(AnsiCodes.RestoreCursor);
        }

        int offset = layout.HasTitle ? 1 : 0;
        if (layout.HasTitle)
        {
            sb.Append(Paint(layout.Title, Style.TitleColor)).Append(AnsiCodes.RestoreCursor);
        }

        for (int i = 0; i < layout.Rows.Count; i++)
        {
            Row row = layout.Rows[i];
            sb.Append(AnsiCodes.Down(offset + i)).Append(AnsiCodes.Forward(row.Columns * GuideCells)).Append(row.Text)
                .Append(AnsiCodes.RestoreCursor);
        }

        return sb.ToString();
    }

    internal SixelImage CreateImage(WidgetEnvironment environment)
    {
        Layout layout = BuildLayout(environment.Columns);
        int offset = layout.HasTitle ? 1 : 0;
        int rows = Math.Max(1, layout.TotalRows);
        int cellWidth = environment.CellWidth;
        int width = Math.Max(1, layout.MaxColumns * GuideCells) * cellWidth;
        int height = environment.GetImageHeight(rows);
        var image = new SixelImage(width, height);
        SixelColor color = Style.GuideColor;
        if (color.A == 0)
        {
            return image;
        }

        double band = (double)height / rows;
        int thickness = Style.LineThickness;
        int half = thickness / 2;
        int LineX(int column) => (column * GuideCells * cellWidth) + (cellWidth / 2) - half;
        int RowEdge(double row) => (int)Math.Round(row * band, MidpointRounding.AwayFromZero);

        for (int i = 0; i < layout.Rows.Count; i++)
        {
            Row row = layout.Rows[i];
            int top = RowEdge(offset + i);
            int bottom = RowEdge(offset + i + 1);
            int center = RowEdge(offset + i + 0.5) - half;
            for (int column = 0; column < row.Continues.Length; column++)
            {
                if (row.Continues[column])
                {
                    Raster.FillRoundedRect(image, LineX(column), top, thickness, bottom - top, 0, color);
                }
            }

            if (row.Columns == 0)
            {
                continue;
            }

            int x = LineX(row.Columns - 1);
            int verticalEnd = row.IsLast ? center + thickness : bottom;
            Raster.FillRoundedRect(image, x, top, thickness, verticalEnd - top, 0, color);
            int hookEnd = ((row.Columns - 1) * GuideCells + GuideCells - 1) * cellWidth;
            Raster.FillRoundedRect(image, x, center, hookEnd - x, thickness, 0, color);
        }

        return image;
    }

    private static string Tint(string text, SixelColor color, bool ansi) =>
        color.A >= ColorAlphaLimit ? AnsiCodes.Colorize(text, color, ansi) : text;

    private static string Paint(string text, SixelColor foreground) =>
        foreground.A >= ColorAlphaLimit ? AnsiCodes.Foreground(foreground) + text + AnsiCodes.Reset : text;

    private string AsciiGuides(Row row, bool ansi)
    {
        if (row.Columns == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (bool continues in row.Continues)
        {
            sb.Append(continues ? Style.AsciiVertical : ' ').Append(' ', GuideCells - 1);
        }

        sb.Append(row.IsLast ? Style.AsciiLastBranch : Style.AsciiBranch).Append(Style.AsciiHorizontal, GuideCells - 2).Append(' ');
        return Tint(sb.ToString(), Style.GuideColor, ansi);
    }

    private string Label(TreeNode node)
    {
        string text = PromptRow.Clean(node.Text);
        return node.Nodes.Count > 0 && !node.Expanded ? text + PromptRow.Clean(Style.CollapsedSuffix) : text;
    }

    private void Collect(IList<TreeNode> nodes, int depth, List<bool> ancestorsHaveLater, bool hasTitle, List<Row> rows)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            TreeNode node = nodes[i];
            bool last = i == nodes.Count - 1;
            int columns = depth + (hasTitle ? 1 : 0);
            var continues = new bool[Math.Max(0, columns - 1)];
            for (int column = 0; column < continues.Length; column++)
            {
                continues[column] = ancestorsHaveLater[column + (hasTitle ? 0 : 1)];
            }

            rows.Add(new Row(Label(node), columns, last, continues));
            if (node.Expanded && node.Nodes.Count > 0)
            {
                ancestorsHaveLater.Add(!last);
                Collect(node.Nodes, depth + 1, ancestorsHaveLater, hasTitle, rows);
                ancestorsHaveLater.RemoveAt(ancestorsHaveLater.Count - 1);
            }
        }
    }

    private Layout BuildLayout(int limit)
    {
        string title = PromptRow.Clean(Title);
        var rows = new List<Row>();
        Collect(_nodes, 0, new List<bool>(), title.Length > 0, rows);
        int maxColumns = 0;
        foreach (Row row in rows)
        {
            maxColumns = Math.Max(maxColumns, row.Columns);
        }

        if (limit > 0)
        {
            title = TextFit.Clip(title, limit);
            for (int i = 0; i < rows.Count; i++)
            {
                Row row = rows[i];
                string text = TextFit.Clip(row.Text, Math.Max(0, limit - (row.Columns * GuideCells)));
                if (!string.Equals(text, row.Text, StringComparison.Ordinal))
                {
                    rows[i] = new Row(text, row.Columns, row.IsLast, row.Continues);
                }
            }
        }

        return new Layout(title, rows, maxColumns);
    }

    private sealed class Row
    {
        public Row(string text, int columns, bool isLast, bool[] continues)
        {
            Text = text;
            Columns = columns;
            IsLast = isLast;
            Continues = continues;
        }

        public string Text { get; }

        /// <summary>Number of guide columns in front of the text (4 cells each); 0 for top-level nodes without title.</summary>
        public int Columns { get; }

        public bool IsLast { get; }

        /// <summary>For each guide column before the node's own: true if a vertical guide passes through this row.</summary>
        public bool[] Continues { get; }
    }

    private sealed class Layout
    {
        public Layout(string title, List<Row> rows, int maxColumns)
        {
            Title = title;
            Rows = rows;
            MaxColumns = maxColumns;
        }

        public string Title { get; }

        public bool HasTitle => Title.Length > 0;

        public List<Row> Rows { get; }

        public int MaxColumns { get; }

        public int TotalRows => (HasTitle ? 1 : 0) + Rows.Count;
    }
}
