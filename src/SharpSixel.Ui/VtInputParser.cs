using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace SharpSixel.Ui;

/// <summary>
/// Turns the characters a terminal sends on standard input (VT mode) into <see cref="InputEvent"/>s: printable keys,
/// Enter, Tab, Backspace, Escape, arrows, Home/End, PageUp/PageDown, Delete, Ctrl+letter, SGR mouse reports
/// (<c>ESC [ &lt; b ; x ; y M/m</c>, enabled with mode 1000 and 1006) and cursor position reports (<c>ESC [ row ; col R</c>).
/// Unknown sequences are skipped. I/O-agnostic: the characters come from the supplied functions.
/// </summary>
public sealed class VtInputParser
{
    private const int Escape = 0x1B;
    private const int Delete = 0x7F;
    private readonly Func<int> _readChar;
    private readonly Func<int> _readCharShort;

    /// <summary>Creates a parser.</summary>
    /// <param name="readChar">Blocking read of the next character; returns -1 at the end of the input.</param>
    /// <param name="readCharShort">Read of the next character that gives up after a short time (a few dozen milliseconds) and returns -1;
    /// used inside escape sequences to tell a lone Escape key from the start of a sequence.</param>
    public VtInputParser(Func<int> readChar, Func<int> readCharShort)
    {
        _readChar = readChar ?? throw new ArgumentNullException(nameof(readChar));
        _readCharShort = readCharShort ?? throw new ArgumentNullException(nameof(readCharShort));
    }

    /// <summary>Reads the next event; blocks until one is complete.</summary>
    /// <exception cref="EndOfStreamException">The input ended.</exception>
    public InputEvent Read()
    {
        while (true)
        {
            int c = _readChar();
            if (c < 0)
            {
                throw new EndOfStreamException("The input ended.");
            }

            InputEvent? result = c == Escape ? ReadEscape() : FromChar((char)c);
            if (result != null)
            {
                return result;
            }
        }
    }

    private static InputEvent Key(ConsoleKey key, char keyChar = '\0', bool control = false) =>
        InputEvent.FromKey(new ConsoleKeyInfo(keyChar, key, false, false, control));

    private static InputEvent? FromChar(char c)
    {
        switch (c)
        {
            case '\r':
            case '\n':
                return Key(ConsoleKey.Enter, '\r');
            case '\t':
                return Key(ConsoleKey.Tab, '\t');
            case '\b':
            case (char)Delete:
                return Key(ConsoleKey.Backspace, '\b');
            case ' ':
                return Key(ConsoleKey.Spacebar, ' ');
        }

        if (c < ' ')
        {
            return Key((ConsoleKey)('A' + c - 1), c, control: true);
        }

        return Key(0, c);
    }

    private InputEvent? ReadEscape()
    {
        int next = _readCharShort();
        if (next == '[')
        {
            return ReadCsi();
        }

        if (next == 'O')
        {
            return FromFinal((char)Math.Max(0, _readCharShort()), string.Empty);
        }

        return Key(ConsoleKey.Escape, (char)Escape);
    }

    private InputEvent? ReadCsi()
    {
        var parameters = new StringBuilder();
        while (true)
        {
            int c = _readCharShort();
            if (c < 0)
            {
                return Key(ConsoleKey.Escape, (char)Escape);
            }

            if (c >= 0x40 && c <= 0x7E)
            {
                return FromFinal((char)c, parameters.ToString());
            }

            parameters.Append((char)c);
        }
    }

    private static InputEvent? FromFinal(char final, string parameters)
    {
        switch (final)
        {
            case 'A': return Key(ConsoleKey.UpArrow);
            case 'B': return Key(ConsoleKey.DownArrow);
            case 'C': return Key(ConsoleKey.RightArrow);
            case 'D': return Key(ConsoleKey.LeftArrow);
            case 'H': return Key(ConsoleKey.Home);
            case 'F': return Key(ConsoleKey.End);
            case 'Z': return Key(ConsoleKey.Tab, '\t');
            case '~': return FromTilde(parameters);
            case 'R': return FromCursorReport(parameters);
            case 'M':
            case 'm': return FromMouse(parameters, final == 'M');
            default: return null;
        }
    }

    private static InputEvent? FromTilde(string parameters)
    {
        string[] parts = parameters.Split(';');
        switch (parts[0])
        {
            case "1":
            case "7": return Key(ConsoleKey.Home);
            case "3": return Key(ConsoleKey.Delete);
            case "4":
            case "8": return Key(ConsoleKey.End);
            case "5": return Key(ConsoleKey.PageUp);
            case "6": return Key(ConsoleKey.PageDown);
            default: return null;
        }
    }

    private static InputEvent? FromCursorReport(string parameters)
    {
        string[] parts = parameters.Split(';');
        if (parts.Length == 2 && TryParse(parts[0], out int row) && TryParse(parts[1], out int column))
        {
            return InputEvent.FromCursorPosition(row - 1, column - 1);
        }

        return null;
    }

    private static InputEvent? FromMouse(string parameters, bool press)
    {
        if (!parameters.StartsWith("<", StringComparison.Ordinal))
        {
            return null;
        }

        string[] parts = parameters.Substring(1).Split(';');
        if (parts.Length != 3 || !TryParse(parts[0], out int code) || !TryParse(parts[1], out int x) || !TryParse(parts[2], out int y)
            || (code & 32) != 0)
        {
            return null;
        }

        MouseButton button;
        if ((code & 64) != 0)
        {
            button = (code & 1) == 0 ? MouseButton.WheelUp : MouseButton.WheelDown;
        }
        else
        {
            switch (code & 3)
            {
                case 0: button = MouseButton.Left; break;
                case 1: button = MouseButton.Middle; break;
                case 2: button = MouseButton.Right; break;
                default: return null;
            }
        }

        return InputEvent.FromMouse(new MouseEvent(x - 1, y - 1, button, press));
    }

    private static bool TryParse(string text, out int value) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
}
