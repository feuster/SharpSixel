using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace SharpSixel.Ui;

/// <summary>
/// Choose one item from a list: Up/Down (wrapping), Home/End and PageUp/PageDown move, Enter confirms. A list longer than
/// <see cref="PageSize"/> scrolls. The current item has a radio marker (sixel graphic or ASCII).
/// The optional <see cref="Prompt{TResult}.Validator"/> can reject a choice. Without ANSI/sixel support the choices are
/// numbered and the user types the number.
/// </summary>
/// <typeparam name="T">Type of the choices.</typeparam>
public sealed class SelectionPrompt<T> : Prompt<T>
{
    private readonly List<T> _choices = new List<T>();
    private readonly ReadOnlyCollection<T> _choicesView;
    private readonly ChoiceNavigator _navigator = new ChoiceNavigator();
    private Func<T, string> _converter = DefaultConverter;
    private int _pageSize = 10;

    /// <summary>Creates a selection prompt with the default style.</summary>
    public SelectionPrompt()
        : this(new PromptStyle())
    {
    }

    /// <summary>Creates a selection prompt with the given style.</summary>
    public SelectionPrompt(PromptStyle style)
        : base(style)
    {
        _choicesView = new ReadOnlyCollection<T>(_choices);
    }

    /// <summary>The choices in display order.</summary>
    public IReadOnlyList<T> Choices => _choicesView;

    /// <summary>Converts a choice to its display text. Default <c>ToString()</c>.</summary>
    public Func<T, string> Converter
    {
        get => _converter;
        set => _converter = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Maximum number of choices shown at once (at least 1). Default 10.</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Guard.AtLeast(value, 1, nameof(value));
    }

    /// <summary>Hint line below the choices; null or empty hides it.</summary>
    public string? Instructions { get; set; } = "(Up/Down to move, Enter to select)";

    /// <summary>Adds a choice.</summary>
    public void AddChoice(T choice) => _choices.Add(choice);

    /// <summary>Adds several choices.</summary>
    public void AddChoices(IEnumerable<T> choices)
    {
        if (choices == null)
        {
            throw new ArgumentNullException(nameof(choices));
        }

        _choices.AddRange(choices);
    }

    internal override int RowCount =>
        (HasTitle ? 1 : 0) + _navigator.Visible + (string.IsNullOrEmpty(Instructions) ? 0 : 1) + 1;

    internal override T Result => _choices[_navigator.Cursor];

    internal override string Summary => Label(_navigator.Cursor);

    internal override void Begin()
    {
        if (_choices.Count == 0)
        {
            throw new InvalidOperationException("The prompt has no choices.");
        }

        _navigator.Reset(_choices.Count, _pageSize);
    }

    internal override void BuildRows(List<PromptRow> rows)
    {
        AddTitleRow(rows);
        for (int i = _navigator.Top; i < _navigator.Top + _navigator.Visible; i++)
        {
            bool current = i == _navigator.Cursor;
            rows.Add(new PromptRow
            {
                AsciiPrefix = Style.AsciiPrefix(current, current ? Style.AsciiRadioOn : Style.AsciiRadioOff),
                Text = Label(i),
                Shape = MarkerShape.Radio,
                MarkerColor = current ? Style.ActiveColor : Style.InactiveColor,
                Filled = current,
                TextColor = current ? (SixelColor?)Style.ActiveColor : null,
                Bar = current,
            });
        }

        AddHintRow(rows, Instructions);
        AddErrorRow(rows);
    }

    internal override bool OnKey(ConsoleKeyInfo key)
    {
        Error = null;
        if (_navigator.Handle(key) || key.Key != ConsoleKey.Enter)
        {
            return false;
        }

        Error = Check(Result);
        return Error == null;
    }

    internal override bool OnMouse(int column, int row, WidgetEnvironment environment)
    {
        Error = null;
        int item = row - (HasTitle ? 1 : 0);
        if (item < 0 || item >= _navigator.Visible)
        {
            return false;
        }

        _navigator.MoveTo(_navigator.Top + item);
        Error = Check(Result);
        return Error == null;
    }

    internal override T RunPlain(TextWriter writer, Func<ConsoleKeyInfo> readKey)
    {
        if (HasTitle)
        {
            writer.WriteLine(PromptRow.Clean(Title));
        }

        for (int i = 0; i < _choices.Count; i++)
        {
            writer.WriteLine("  " + (i + 1).ToString(CultureInfo.InvariantCulture) + ") " + Label(i));
        }

        while (true)
        {
            writer.Write("Choice (1-" + _choices.Count.ToString(CultureInfo.InvariantCulture) + "): ");
            writer.Flush();
            string line = LineReader.ReadLine(writer, readKey, null).Trim();
            string? error;
            if (int.TryParse(line, NumberStyles.None, CultureInfo.InvariantCulture, out int number)
                && number >= 1 && number <= _choices.Count)
            {
                T choice = _choices[number - 1];
                error = Check(choice);
                if (error == null)
                {
                    return choice;
                }
            }
            else
            {
                error = "Enter a number between 1 and " + _choices.Count.ToString(CultureInfo.InvariantCulture) + ".";
            }

            writer.WriteLine("! " + PromptRow.Clean(error));
        }
    }

    private static string DefaultConverter(T value) => value?.ToString() ?? string.Empty;

    private string Label(int index) => PromptRow.Clean(_converter(_choices[index]));
}
