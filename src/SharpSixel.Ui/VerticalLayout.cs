using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SharpSixel.Ui;

/// <summary>
/// Stacks widgets on top of each other, left aligned, <see cref="Spacing"/> empty rows apart. The rows are the sum of the rows
/// of the children, the width is that of the widest child. A horizontal <see cref="Rule"/> without width fills that width. The
/// number of rows must not grow beyond the rows reserved in a <see cref="LiveRegion"/>; children should keep their row count between
/// frames, otherwise the following children move. A layout may contain layouts, but not itself; as a child of a
/// <see cref="HorizontalLayout"/> it forms a column.
/// </summary>
public sealed class VerticalLayout : Widget
{
    private static readonly WidgetEnvironment SixelProbe = new WidgetEnvironment(true);
    private static readonly WidgetEnvironment AsciiProbe = new WidgetEnvironment(false);
    private readonly Collection<Widget> _children;
    private int _spacing;

    /// <summary>Creates an empty layout.</summary>
    public VerticalLayout()
    {
        _children = new LayoutChildren(this);
    }

    /// <summary>The widgets from top to bottom (null entries are rejected).</summary>
    public IList<Widget> Children => _children;

    /// <summary>Empty rows between two children. Default 0.</summary>
    public int Spacing
    {
        get => _spacing;
        set => _spacing = Guard.AtLeast(value, 0, nameof(value));
    }

    /// <summary>Adds a widget at the bottom.</summary>
    public void Add(Widget child) => _children.Add(child);

    /// <inheritdoc />
    protected override int SixelRows => RowsFor(SixelProbe);

    /// <inheritdoc />
    protected override int AsciiRows => RowsFor(AsciiProbe);

    /// <inheritdoc />
    protected override int MeasureColumns(WidgetEnvironment environment)
    {
        Place(environment, out int width);
        return width;
    }

    /// <inheritdoc />
    protected override string RenderAscii(WidgetEnvironment environment)
    {
        var lines = new List<string>();
        List<Slot> slots = Place(environment, out _);
        for (int i = 0; i < slots.Count; i++)
        {
            Slot slot = slots[i];
            string[] childLines = slot.Widget.Render(slot.Environment).Split('\n');
            for (int row = 0; row < slot.Rows; row++)
            {
                lines.Add(row < childLines.Length ? childLines[row] : string.Empty);
            }

            if (i < slots.Count - 1)
            {
                for (int gap = 0; gap < _spacing; gap++)
                {
                    lines.Add(string.Empty);
                }
            }
        }

        return string.Join("\n", lines);
    }

    /// <inheritdoc />
    protected override string RenderSixel(WidgetEnvironment environment)
    {
        var sb = new StringBuilder();
        int cursor = 0;
        foreach (Slot slot in Place(environment, out _))
        {
            // Every child frame saves the cursor where it starts and ends there again.
            sb.Append(AnsiCodes.Down(slot.RowOffset - cursor)).Append(slot.Widget.Render(slot.Environment));
            cursor = slot.RowOffset;
        }

        // Back to the origin; the last child frame overwrote the saved cursor position.
        return sb.Append(AnsiCodes.Up(cursor)).Append(AnsiCodes.SaveCursor).ToString();
    }

    private int RowsFor(WidgetEnvironment probe)
    {
        int rows = 0;
        foreach (Widget child in _children)
        {
            rows += child.GetRows(probe);
        }

        rows += _spacing * Math.Max(0, _children.Count - 1);
        return Math.Max(1, rows);
    }

    private List<Slot> Place(WidgetEnvironment environment, out int width)
    {
        // Children that fill the width get the width of the others.
        width = 0;
        WidgetEnvironment measure = environment.WithSpace(environment.Columns, 0);
        foreach (Widget child in _children)
        {
            if (!child.FillsWidth)
            {
                width = Math.Max(width, child.GetColumns(measure));
            }
        }

        var slots = new List<Slot>();
        int fillWidth = width;
        int rowOffset = 0;
        foreach (Widget child in _children)
        {
            int columns = child.FillsWidth && fillWidth > 0 ? fillWidth : environment.Columns;
            WidgetEnvironment childEnvironment = environment.WithSpace(columns, 0);
            int rows = child.GetRows(childEnvironment);
            width = Math.Max(width, child.GetColumns(childEnvironment));
            slots.Add(new Slot(child, childEnvironment, rowOffset, rows));
            rowOffset += rows + _spacing;
        }

        return slots;
    }

    private sealed class Slot
    {
        public Slot(Widget widget, WidgetEnvironment environment, int rowOffset, int rows)
        {
            Widget = widget;
            Environment = environment;
            RowOffset = rowOffset;
            Rows = rows;
        }

        public Widget Widget { get; }

        public WidgetEnvironment Environment { get; }

        public int RowOffset { get; }

        public int Rows { get; }
    }
}
