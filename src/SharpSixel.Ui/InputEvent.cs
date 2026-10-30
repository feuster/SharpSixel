using System;

namespace SharpSixel.Ui;

/// <summary>Mouse button or wheel direction of a <see cref="MouseEvent"/>.</summary>
public enum MouseButton
{
    /// <summary>Left button.</summary>
    Left,

    /// <summary>Middle button.</summary>
    Middle,

    /// <summary>Right button.</summary>
    Right,

    /// <summary>Wheel turned up.</summary>
    WheelUp,

    /// <summary>Wheel turned down.</summary>
    WheelDown,
}

/// <summary>A mouse report of the terminal: position in cells (0-based, relative to the screen) and button.</summary>
public sealed class MouseEvent
{
    /// <summary>Creates a mouse event.</summary>
    /// <param name="column">Column, 0-based.</param>
    /// <param name="row">Row, 0-based.</param>
    /// <param name="button">Button or wheel direction.</param>
    /// <param name="isPress">True for a press (and wheel steps), false for a release.</param>
    public MouseEvent(int column, int row, MouseButton button, bool isPress)
    {
        Column = column;
        Row = row;
        Button = button;
        IsPress = isPress;
    }

    /// <summary>Column, 0-based.</summary>
    public int Column { get; }

    /// <summary>Row, 0-based.</summary>
    public int Row { get; }

    /// <summary>Button or wheel direction.</summary>
    public MouseButton Button { get; }

    /// <summary>True for a press (and wheel steps), false for a release.</summary>
    public bool IsPress { get; }
}

/// <summary>Kind of an <see cref="InputEvent"/>.</summary>
public enum InputKind
{
    /// <summary>A key press, see <see cref="InputEvent.Key"/>.</summary>
    Key,

    /// <summary>A mouse report, see <see cref="InputEvent.Mouse"/>.</summary>
    Mouse,

    /// <summary>The answer to a cursor position request, see <see cref="InputEvent.Row"/> and <see cref="InputEvent.Column"/>.</summary>
    CursorPosition,
}

/// <summary>One input of the terminal: a key, a mouse report or a cursor position report.</summary>
public sealed class InputEvent
{
    private InputEvent(InputKind kind, ConsoleKeyInfo key, MouseEvent? mouse, int row, int column)
    {
        Kind = kind;
        Key = key;
        Mouse = mouse;
        Row = row;
        Column = column;
    }

    /// <summary>What this event is.</summary>
    public InputKind Kind { get; }

    /// <summary>The key (<see cref="InputKind.Key"/> only).</summary>
    public ConsoleKeyInfo Key { get; }

    /// <summary>The mouse report (<see cref="InputKind.Mouse"/> only).</summary>
    public MouseEvent? Mouse { get; }

    /// <summary>Row of the cursor, 0-based (<see cref="InputKind.CursorPosition"/> only).</summary>
    public int Row { get; }

    /// <summary>Column of the cursor, 0-based (<see cref="InputKind.CursorPosition"/> only).</summary>
    public int Column { get; }

    /// <summary>Creates a key event.</summary>
    public static InputEvent FromKey(ConsoleKeyInfo key) => new InputEvent(InputKind.Key, key, null, 0, 0);

    /// <summary>Creates a mouse event.</summary>
    public static InputEvent FromMouse(MouseEvent mouse) =>
        new InputEvent(InputKind.Mouse, default(ConsoleKeyInfo), mouse ?? throw new ArgumentNullException(nameof(mouse)), 0, 0);

    /// <summary>Creates a cursor position report (0-based row and column).</summary>
    public static InputEvent FromCursorPosition(int row, int column) =>
        new InputEvent(InputKind.CursorPosition, default(ConsoleKeyInfo), null, row, column);
}
