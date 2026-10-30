using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SharpSixel.Ui;

/// <summary>
/// Message dialog: optional <see cref="Prompt{TResult}.Title"/>, a multi-line <see cref="Message"/> and a row of buttons
/// (<see cref="Buttons"/>, default a single "OK"). Left/Right/Tab (wrapping), Home and End move between the buttons, Enter
/// confirms; <see cref="Prompt{TResult}.Show(TextWriter, WidgetEnvironment, Func{ConsoleKeyInfo})"/> returns the index of the chosen button. In sixel mode the
/// buttons are rounded faces (the selected one in <see cref="PromptStyle.ActiveColor"/>); in ASCII mode the selected button is drawn as
/// <c>[ Label ]</c> (additionally in reverse video with ANSI); the title has a dot marker (sixel graphic). The optional
/// <see cref="Prompt{TResult}.Validator"/> can reject a button. Without ANSI/sixel support the buttons are numbered and
/// the user types the number (empty input = <see cref="DefaultButton"/>). Message lines and buttons should fit on one
/// terminal row.
/// </summary>
public sealed class Dialog : Prompt<int>
{
    private const int AsciiPrefixLength = 2;

    private readonly List<string> _buttons = new List<string> { "OK" };
    private int _defaultButton;
    private string[] _lines = new string[0];
    private int _cursor;
    private readonly List<int> _cellStarts = new List<int>();
    private readonly List<int> _cellLengths = new List<int>();

    /// <summary>Creates a dialog with the default style.</summary>
    public Dialog()
        : this(new PromptStyle())
    {
    }

    /// <summary>Creates a dialog with the given style.</summary>
    public Dialog(PromptStyle style)
        : base(style)
    {
    }

    /// <summary>Message text; line breaks (<c>\n</c>) start new rows. Null or empty = no message.</summary>
    public string? Message { get; set; }

    /// <summary>Button labels from left to right (at least one). Default a single "OK".</summary>
    public IList<string> Buttons => _buttons;

    /// <summary>Index of the initially selected button. Must be below the number of buttons. Default 0.</summary>
    public int DefaultButton
    {
        get => _defaultButton;
        set => _defaultButton = Guard.AtLeast(value, 0, nameof(value));
    }

    /// <summary>Replaces the buttons by <paramref name="buttons"/> and selects the first one.</summary>
    public void SetButtons(params string[] buttons)
    {
        if (buttons == null)
        {
            throw new ArgumentNullException(nameof(buttons));
        }

        _buttons.Clear();
        _buttons.AddRange(buttons);
        _defaultButton = 0;
    }

    internal override int RowCount => (HasTitle ? 1 : 0) + _lines.Length + 1 + 1;

    internal override int Result => _cursor;

    internal override string Summary => ButtonLabel(_cursor);

    internal override void Begin()
    {
        if (_buttons.Count == 0)
        {
            throw new InvalidOperationException("The dialog has no buttons.");
        }

        if (_defaultButton >= _buttons.Count)
        {
            throw new InvalidOperationException("DefaultButton is not a valid button index.");
        }

        _lines = string.IsNullOrEmpty(Message)
            ? new string[0]
            : Message!.Replace("\r\n", "\n").Split('\n');
        _cursor = _defaultButton;
    }

    internal override void BuildRows(List<PromptRow> rows)
    {
        if (HasTitle)
        {
            rows.Add(new PromptRow { Text = Title!, Shape = MarkerShape.Dot, MarkerColor = Style.ActiveColor });
        }

        foreach (string line in _lines)
        {
            rows.Add(new PromptRow { AsciiPrefix = "  ", Text = line, Indent = true });
        }

        rows.Add(ButtonRow());
        AddErrorRow(rows);
    }

    internal override bool OnKey(ConsoleKeyInfo key)
    {
        Error = null;
        int count = _buttons.Count;
        switch (key.Key)
        {
            case ConsoleKey.LeftArrow:
                _cursor = (_cursor + count - 1) % count;
                return false;
            case ConsoleKey.RightArrow:
            case ConsoleKey.Tab:
                _cursor = (_cursor + 1) % count;
                return false;
            case ConsoleKey.Home:
                _cursor = 0;
                return false;
            case ConsoleKey.End:
                _cursor = count - 1;
                return false;
            case ConsoleKey.Enter:
                Error = Check(_cursor);
                return Error == null;
            default:
                return false;
        }
    }

    internal override bool OnMouse(int column, int row, WidgetEnvironment environment)
    {
        Error = null;
        if (row != (HasTitle ? 1 : 0) + _lines.Length)
        {
            return false;
        }

        int offset = column - (environment.UseSixel ? PromptView.MarkerColumns + 1 : AsciiPrefixLength);
        for (int i = 0; i < _cellStarts.Count; i++)
        {
            if (offset >= _cellStarts[i] && offset < _cellStarts[i] + _cellLengths[i])
            {
                _cursor = i;
                Error = Check(_cursor);
                return Error == null;
            }
        }

        return false;
    }

    internal override int RunPlain(TextWriter writer, Func<ConsoleKeyInfo> readKey)
    {
        if (HasTitle)
        {
            writer.WriteLine(PromptRow.Clean(Title));
        }

        foreach (string line in _lines)
        {
            writer.WriteLine("  " + PromptRow.Clean(line));
        }

        for (int i = 0; i < _buttons.Count; i++)
        {
            writer.WriteLine("  " + (i + 1).ToString(CultureInfo.InvariantCulture) + ") " + ButtonLabel(i));
        }

        string range = "1-" + _buttons.Count.ToString(CultureInfo.InvariantCulture);
        string defaultNumber = (_defaultButton + 1).ToString(CultureInfo.InvariantCulture);
        while (true)
        {
            writer.Write("Choice (" + range + ", Enter = " + defaultNumber + "): ");
            writer.Flush();
            string line = LineReader.ReadLine(writer, readKey, null).Trim();
            if (line.Length == 0)
            {
                line = defaultNumber;
            }

            string? error;
            if (int.TryParse(line, NumberStyles.None, CultureInfo.InvariantCulture, out int number)
                && number >= 1 && number <= _buttons.Count)
            {
                error = Check(number - 1);
                if (error == null)
                {
                    return number - 1;
                }
            }
            else
            {
                error = "Enter a number between 1 and " + _buttons.Count.ToString(CultureInfo.InvariantCulture) + ".";
            }

            writer.WriteLine("! " + PromptRow.Clean(error));
        }
    }

    private string ButtonLabel(int index) => PromptRow.Clean(_buttons[index]);

    private PromptRow ButtonRow()
    {
        var sb = new StringBuilder();
        var chips = new List<PromptChip>();
        _cellStarts.Clear();
        _cellLengths.Clear();
        int start = 0;
        int length = 0;
        for (int i = 0; i < _buttons.Count; i++)
        {
            if (i > 0)
            {
                sb.Append("  ");
            }

            string label = ButtonLabel(i);
            string cell = i == _cursor ? "[ " + label + " ]" : "  " + label + "  ";
            _cellStarts.Add(sb.Length);
            _cellLengths.Add(cell.Length);
            chips.Add(new PromptChip
            {
                Start = sb.Length,
                Length = cell.Length,
                LabelStart = sb.Length + 2,
                LabelLength = label.Length,
                Color = i == _cursor ? Style.ActiveColor : Style.ButtonColor,
                TextColor = Style.ButtonTextColor,
            });
            if (i == _cursor)
            {
                start = sb.Length;
                length = cell.Length;
            }

            sb.Append(cell);
        }

        var row = new PromptRow
        {
            AsciiPrefix = "  ",
            Text = sb.ToString(),
            Indent = true,
            HighlightStart = start,
            HighlightLength = length,
            HighlightColor = Style.ActiveColor,
        };
        row.Chips.AddRange(chips);
        return row;
    }
}
