using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SharpSixel.Ui;

/// <summary>
/// Places widgets side by side (top aligned), <see cref="Spacing"/> cells apart. Each child is drawn with its own frame, shifted
/// to its column; the width of a child is <see cref="Widget.GetColumns"/>, and a child may use the terminal width that is left
/// of its column. The rows are those of the highest child; the number of rows must not grow beyond the rows reserved in a
/// <see cref="LiveRegion"/>. Children should keep their width between frames, otherwise the following children move.
/// A layout may contain layouts, but not itself.
/// </summary>
public sealed class HorizontalLayout : Widget
{
    private static readonly WidgetEnvironment SixelProbe = new WidgetEnvironment(true);
    private static readonly WidgetEnvironment AsciiProbe = new WidgetEnvironment(false);
    private readonly Collection<Widget> _children;
    private int _spacing = 2;

    /// <summary>Creates an empty layout.</summary>
    public HorizontalLayout()
    {
        _children = new ChildCollection(this);
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

    /// <inheritdoc />
    protected override int SixelRows => RowsFor(SixelProbe);

    /// <inheritdoc />
    protected override int AsciiRows => RowsFor(AsciiProbe);

    /// <inheritdoc />
    protected override int MeasureColumns(WidgetEnvironment environment)
    {
        List<Slot> slots = Place(environment);
        return slots.Count == 0 ? 0 : slots[slots.Count - 1].Offset + slots[slots.Count - 1].Width;
    }

    /// <inheritdoc />
    protected override string RenderAscii(WidgetEnvironment environment)
    {
        int rows = AsciiRows;
        var lines = new StringBuilder[rows];
        for (int row = 0; row < rows; row++)
        {
            lines[row] = new StringBuilder();
        }

        List<Slot> slots = Place(environment);
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
        foreach (Slot slot in Place(environment))
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

    private List<Slot> Place(WidgetEnvironment environment)
    {
        var slots = new List<Slot>();
        int offset = 0;
        foreach (Widget child in _children)
        {
            int available = environment.Columns > 0 ? Math.Max(1, environment.Columns - offset) : 0;
            WidgetEnvironment childEnvironment = environment.WithColumns(available);
            int width = child.GetColumns(childEnvironment);
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

    private sealed class ChildCollection : Collection<Widget>
    {
        private readonly HorizontalLayout _owner;

        public ChildCollection(HorizontalLayout owner) => _owner = owner;

        protected override void InsertItem(int index, Widget item) => base.InsertItem(index, Check(item));

        protected override void SetItem(int index, Widget item) => base.SetItem(index, Check(item));

        private Widget Check(Widget item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (ReferenceEquals(item, _owner))
            {
                throw new ArgumentException("A layout cannot contain itself.", nameof(item));
            }

            return item;
        }
    }
}
