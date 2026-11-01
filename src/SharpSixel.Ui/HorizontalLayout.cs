using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SharpSixel.Ui;

/// <summary>
/// Places widgets side by side (top aligned), <see cref="Spacing"/> cells apart. Each child is drawn with its own frame, shifted
/// to its column; the width of a child is <see cref="Widget.GetColumns"/>, and a child may use the terminal width that is left
/// of its column. The rows are those of the highest child; the number of rows must not grow beyond the rows reserved in a
/// <see cref="LiveRegion"/>. A child with a fixed width (<see cref="Add(Widget, int)"/>) keeps its column even if its content changes, and is
/// offered exactly that width; other children should keep their width between frames, otherwise the following children move.
/// A <see cref="Rule"/> with a vertical orientation and no height stretches to the rows of the layout.
/// A layout may contain layouts, but not itself.
/// </summary>
public sealed class HorizontalLayout : Widget
{
    private static readonly WidgetEnvironment SixelProbe = new WidgetEnvironment(true);
    private static readonly WidgetEnvironment AsciiProbe = new WidgetEnvironment(false);
    private readonly Collection<Widget> _children;
    private readonly Dictionary<Widget, int> _widths = new Dictionary<Widget, int>();
    private int _spacing = 2;

    /// <summary>Creates an empty layout.</summary>
    public HorizontalLayout()
    {
        _children = new LayoutChildren(this, child => _widths.Remove(child));
    }

    /// <summary>The widgets from left to right (null entries are rejected).</summary>
    public IList<Widget> Children => _children;

    /// <summary>Free cells between two children. Default 2.</summary>
    public int Spacing
    {
        get => _spacing;
        set => _spacing = Guard.AtLeast(value, 0, nameof(value));
    }

    /// <summary>Adds a widget at the right end.</summary>
    public void Add(Widget child) => _children.Add(child);

    /// <summary>
    /// Adds a widget at the right end with a fixed width in cells (at least 1): the child keeps its column when its content changes,
    /// is offered this width (a <see cref="Rule"/> without width fills it) and should not draw wider.
    /// </summary>
    public void Add(Widget child, int width)
    {
        Guard.AtLeast(width, 1, nameof(width));
        _children.Add(child);
        _widths[child] = width;
    }

    /// <inheritdoc />
    protected override int SixelRows => RowsFor(SixelProbe);

    /// <inheritdoc />
    protected override int AsciiRows => RowsFor(AsciiProbe);

    /// <inheritdoc />
    protected override int MeasureColumns(WidgetEnvironment environment)
    {
        List<Slot> slots = Place(environment, 0);
        return slots.Count == 0 ? 0 : slots[slots.Count - 1].Offset + slots[slots.Count - 1].Width;
    }

    /// <inheritdoc />
    protected override string RenderAscii(WidgetEnvironment environment)
    {
        int rows = Math.Max(AsciiRows, environment.Rows);
        var lines = new StringBuilder[rows];
        for (int row = 0; row < rows; row++)
        {
            lines[row] = new StringBuilder();
        }

        List<Slot> slots = Place(environment, rows);
        for (int i = 0; i < slots.Count; i++)
        {
            Slot slot = slots[i];
            string[] childLines = slot.Widget.Render(slot.Environment).Split('\n');
            int gap = i < slots.Count - 1 ? _spacing : 0;
            for (int row = 0; row < rows; row++)
            {
                string line = row < childLines.Length ? childLines[row] : string.Empty;
                lines[row].Append(line).Append(' ', Math.Max(0, slot.Width - TextFit.VisibleLength(line)) + gap);
            }
        }

        var result = new string[rows];
        for (int row = 0; row < rows; row++)
        {
            result[row] = lines[row] + (environment.UseAnsi ? AnsiCodes.EraseLineRest : string.Empty);
        }

        return string.Join("\n", result);
    }

    /// <inheritdoc />
    protected override string RenderSixel(WidgetEnvironment environment)
    {
        var sb = new StringBuilder();
        int cursor = 0;
        foreach (Slot slot in Place(environment, Math.Max(SixelRows, environment.Rows)))
        {
            // Every child frame saves the cursor where it starts and ends there again.
            sb.Append(AnsiCodes.Forward(slot.Offset - cursor)).Append(slot.Widget.Render(slot.Environment));
            cursor = slot.Offset;
        }

        // Back to the origin; the last child frame overwrote the saved cursor position.
        return sb.Append(AnsiCodes.Back(cursor)).Append(AnsiCodes.SaveCursor).ToString();
    }

    private int RowsFor(WidgetEnvironment probe)
    {
        int rows = 1;
        foreach (Widget child in _children)
        {
            rows = Math.Max(rows, child.GetRows(probe));
        }

        return rows;
    }

    private List<Slot> Place(WidgetEnvironment environment, int rows)
    {
        var slots = new List<Slot>();
        int offset = 0;
        foreach (Widget child in _children)
        {
            int available = environment.Columns > 0 ? Math.Max(1, environment.Columns - offset) : 0;
            bool fixedWidth = _widths.TryGetValue(child, out int wanted);
            if (fixedWidth)
            {
                available = available > 0 ? Math.Min(available, wanted) : wanted;
            }

            WidgetEnvironment childEnvironment = environment.WithSpace(available, rows);
            int width = fixedWidth ? wanted : child.GetColumns(childEnvironment);
            slots.Add(new Slot(child, childEnvironment, offset, width));
            offset += width + _spacing;
        }

        return slots;
    }

    private sealed class Slot
    {
        public Slot(Widget widget, WidgetEnvironment environment, int offset, int width)
        {
            Widget = widget;
            Environment = environment;
            Offset = offset;
            Width = width;
        }

        public Widget Widget { get; }

        public WidgetEnvironment Environment { get; }

        public int Offset { get; }

        public int Width { get; }
    }
}
