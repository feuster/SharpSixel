using System;
using System.IO;
using System.Text;

namespace SharpSixel.Ui;

/// <summary>
/// Reserves terminal rows and redraws a widget in place. Uses ANSI cursor save/restore (DECSC/DECRC) plus relative
/// moves, so it works on any TextWriter and does not depend on absolute buffer coordinates. The rows are reserved
/// up front, so a scrolling terminal cannot invalidate the saved position. In sixel mode one guard row is reserved
/// below the region: terminals move the cursor below a sixel image, which would scroll the screen (and shift the
/// saved position) if the region ends on the last screen row.
/// </summary>
public sealed class LiveRegion : IDisposable
{
    private readonly TextWriter _writer;
    private readonly WidgetEnvironment _environment;
    private int _rows;
    private bool _active;
    private bool _mouse;
    private int _originRow;
    private int _originColumn;

    /// <summary>Creates a live region on the given output.</summary>
    public LiveRegion(TextWriter writer, WidgetEnvironment environment)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    private int GuardRows => _environment.UseSixel ? 1 : 0;

    /// <summary>Reserves <paramref name="rows"/> rows at the cursor position (column 0) and remembers the origin.</summary>
    public void Begin(int rows)
    {
        if (_active)
        {
            throw new InvalidOperationException("The region is already active.");
        }

        _rows = Guard.AtLeast(rows, 1, nameof(rows));
        int reserved = _rows + GuardRows;
        _writer.Write("\r" + new string('\n', reserved - 1) + AnsiCodes.Up(reserved - 1) + AnsiCodes.SaveCursor);
        _writer.Flush();
        _active = true;
    }

    /// <summary>
    /// Turns on mouse reporting (SGR mode 1000/1006) and asks the terminal where the region starts, so mouse positions can be
    /// mapped with <see cref="TryGetLocal"/>. Call it after <see cref="Begin"/> and before drawing; <see cref="End"/> turns the
    /// reporting off again. Needs input in VT mode (see <see cref="VtInputParser"/>): the terminal answers with a cursor
    /// position report that is read from <paramref name="readInput"/> (other events are skipped meanwhile), so this blocks until
    /// the terminal answers. The origin is valid as long as the screen does not scroll or resize.
    /// </summary>
    /// <param name="readInput">Source of the input events.</param>
    public void EnableMouse(Func<InputEvent> readInput)
    {
        if (readInput == null)
        {
            throw new ArgumentNullException(nameof(readInput));
        }

        if (!_active)
        {
            throw new InvalidOperationException("Call Begin first.");
        }

        if (_mouse)
        {
            return;
        }

        _mouse = true;
        _writer.Write(AnsiCodes.MouseOn + AnsiCodes.CursorPositionRequest);
        _writer.Flush();
        InputEvent report;
        do
        {
            report = readInput();
        }
        while (report.Kind != InputKind.CursorPosition);

        _originRow = report.Row;
        _originColumn = report.Column;
    }

    /// <summary>
    /// Converts a mouse position to coordinates inside the region (column and row relative to its origin).
    /// False if mouse reporting is not enabled or the position is outside the reserved rows.
    /// </summary>
    public bool TryGetLocal(MouseEvent mouse, out int column, out int row)
    {
        if (mouse == null)
        {
            throw new ArgumentNullException(nameof(mouse));
        }

        column = mouse.Column - _originColumn;
        row = mouse.Row - _originRow;
        return _active && _mouse && column >= 0 && row >= 0 && row < _rows;
    }

    /// <summary>Draws the current frame of the widget over the previous one.</summary>
    public void Update(Widget widget)
    {
        if (widget == null)
        {
            throw new ArgumentNullException(nameof(widget));
        }

        if (!_active)
        {
            throw new InvalidOperationException("Call Begin first.");
        }

        if (widget.GetRows(_environment) > _rows)
        {
            throw new ArgumentException("The widget needs more rows than reserved.", nameof(widget));
        }

        string frame = widget.Render(_environment);
        if (!_environment.UseSixel && widget.GetRows(_environment) > 1)
        {
            frame = PlaceLines(frame);
        }

        _writer.Write(AnsiCodes.RestoreCursor + frame + AnsiCodes.RestoreCursor);
        _writer.Flush();
    }

    /// <summary>Positions each line of a multi-row ASCII frame relative to the saved origin and erases the rest of its row.</summary>
    private static string PlaceLines(string frame)
    {
        string[] lines = frame.Split('\n');
        var sb = new StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            sb.Append(AnsiCodes.RestoreCursor).Append(AnsiCodes.Down(i)).Append(lines[i]).Append(AnsiCodes.EraseLineRest);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Ends the region. Without <paramref name="clear"/> the cursor moves to the line below the region and the last frame
    /// stays; with it the region rows are erased and the cursor stays at the region origin (the widget disappears).
    /// Safe to call repeatedly.
    /// </summary>
    /// <param name="clear">True to remove the widget from the screen.</param>
    public void End(bool clear = false)
    {
        if (!_active)
        {
            return;
        }

        string tail;
        if (_mouse)
        {
            _writer.Write(AnsiCodes.MouseOff);
            _mouse = false;
        }

        if (clear)
        {
            var sb = new StringBuilder(AnsiCodes.RestoreCursor);
            for (int row = 0; row < _rows; row++)
            {
                sb.Append(AnsiCodes.Down(row)).Append(AnsiCodes.EraseLine).Append(AnsiCodes.RestoreCursor);
            }

            tail = sb.Append('\r').ToString();
        }
        else
        {
            tail = AnsiCodes.RestoreCursor + (GuardRows > 0
                ? AnsiCodes.Down(_rows) + "\r"
                : AnsiCodes.Down(_rows - 1) + "\r\n");
        }

        _writer.Write(tail);
        _writer.Flush();
        _active = false;
    }

    /// <inheritdoc />
    public void Dispose() => End();
}
