using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace SharpSixel.Ui;

/// <summary>
/// Choose any number of items from a list: Up/Down (wrapping), Home/End and PageUp/PageDown move, Space toggles the current
/// item, Enter confirms. Built-in validation: <see cref="MinSelected"/> and <see cref="MaxSelected"/>; the optional
/// <see cref="Prompt{TResult}.Validator"/> runs afterwards. The answer keeps the order of the choices. Without ANSI/sixel
/// support the choices are numbered and the user types the numbers separated by commas (empty input keeps the preselection).
/// </summary>
/// <typeparam name="T">Type of the choices.</typeparam>
public sealed class MultiSelectionPrompt<T> : Prompt<IReadOnlyList<T>>
{
    private readonly List<T> _choices = new List<T>();
    private readonly List<bool> _preselected = new List<bool>();
    private readonly ReadOnlyCollection<T> _choicesView;
    private readonly ChoiceNavigator _navigator = new ChoiceNavigator();
    private Func<T, string> _converter = DefaultConverter;
    private int _pageSize = 10;
    private int _minSelected;
    private int? _maxSelected;
    private bool[] _state = new bool[0];

    /// <summary>Creates a multi-selection prompt with the default style.</summary>
    public MultiSelectionPrompt()
        : this(new PromptStyle())
    {
    }

    /// <summary>Creates a multi-selection prompt with the given style.</summary>
    public MultiSelectionPrompt(PromptStyle style)
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

    /// <summary>Minimum number of selected items (0 = none required, default). Must not exceed the number of choices.</summary>
    public int MinSelected
    {
        get => _minSelected;
        set => _minSelected = Guard.AtLeast(value, 0, nameof(value));
    }

    /// <summary>Maximum number of selected items; null (default) = unlimited. Must not be below <see cref="MinSelected"/>.</summary>
    public int? MaxSelected
    {
        get => _maxSelected;
        set => _maxSelected = value.HasValue ? Guard.AtLeast(value.Value, 1, nameof(value)) : (int?)null;
    }

    /// <summary>Hint line below the choices; null or empty hides it.</summary>
    public string? Instructions { get; set; } = "(Up/Down to move, Space to toggle, Enter to confirm)";

    /// <summary>Adds a choice, optionally preselected.</summary>
    public void AddChoice(T choice, bool selected = false)
    {
        _choices.Add(choice);
        _preselected.Add(selected);
    }

    internal override int RowCount =>
        (HasTitle ? 1 : 0) + _navigator.Visible + (string.IsNullOrEmpty(Instructions) ? 0 : 1) + 1;

    internal override IReadOnlyList<T> Result => SelectedItems(_state);

    internal override string Summary => string.Join(", ", SelectedLabels(_state));

    internal override void Begin()
    {
        if (_choices.Count == 0)
        {
            throw new InvalidOperationException("The prompt has no choices.");
        }

        if (_minSelected > _choices.Count)
        {
            throw new InvalidOperationException("MinSelected exceeds the number of choices.");
        }

        if (_maxSelected.HasValue && _maxSelected.Value < _minSelected)
        {
            throw new InvalidOperationException("MaxSelected is below MinSelected.");
        }

        _navigator.Reset(_choices.Count, _pageSize);
        _state = _preselected.ToArray();
    }

    internal override void BuildRows(List<PromptRow> rows)
    {
        AddTitleRow(rows);
        for (int i = _navigator.Top; i < _navigator.Top + _navigator.Visible; i++)
        {
            bool current = i == _navigator.Cursor;
            bool selected = _state[i];
            rows.Add(new PromptRow
            {
                AsciiPrefix = Style.AsciiPrefix(current, selected ? Style.AsciiCheckOn : Style.AsciiCheckOff),
                Text = Label(i),
                Shape = MarkerShape.Check,
                MarkerColor = current || selected ? Style.ActiveColor : Style.InactiveColor,
                Filled = selected,
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
        if (_navigator.Handle(key))
        {
            return false;
        }

        if (key.Key == ConsoleKey.Spacebar)
        {
            _state[_navigator.Cursor] = !_state[_navigator.Cursor];
            return false;
        }

        if (key.Key != ConsoleKey.Enter)
        {
            return false;
        }

        Error = Validate(_state);
        return Error == null;
    }

    internal override bool OnMouse(int column, int row, WidgetEnvironment environment)
    {
        Error = null;
        int item = row - (HasTitle ? 1 : 0);
        if (item >= 0 && item < _navigator.Visible)
        {
            int index = _navigator.Top + item;
            _navigator.MoveTo(index);
            _state[index] = !_state[index];
        }

        return false;
    }

    internal override IReadOnlyList<T> RunPlain(TextWriter writer, Func<ConsoleKeyInfo> readKey)
    {
        if (HasTitle)
        {
            writer.WriteLine(PromptRow.Clean(Title));
        }

        for (int i = 0; i < _choices.Count; i++)
        {
            writer.WriteLine("  " + (i + 1).ToString(CultureInfo.InvariantCulture) + ") " + Label(i));
        }

        string range = "1-" + _choices.Count.ToString(CultureInfo.InvariantCulture);
        while (true)
        {
            writer.Write("Choices (" + range + ", separated by commas): ");
            writer.Flush();
            string line = LineReader.ReadLine(writer, readKey, null);
            bool[]? state = line.Trim().Length == 0 ? (bool[])_state.Clone() : ParseNumbers(line);
            string? error = state == null
                ? "Enter numbers between " + range.Replace("-", " and ") + ", separated by commas."
                : Validate(state);
            if (error == null)
            {
                return SelectedItems(state!);
            }

            writer.WriteLine("! " + PromptRow.Clean(error));
        }
    }

    private static string DefaultConverter(T value) => value?.ToString() ?? string.Empty;

    private string Label(int index) => PromptRow.Clean(_converter(_choices[index]));

    private bool[]? ParseNumbers(string line)
    {
        var state = new bool[_choices.Count];
        foreach (string part in line.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out int number)
                || number < 1 || number > _choices.Count)
            {
                return null;
            }

            state[number - 1] = true;
        }

        return state;
    }

    /// <summary>Built-in count rules first, then the custom validator.</summary>
    private string? Validate(bool[] state)
    {
        int count = 0;
        foreach (bool selected in state)
        {
            if (selected)
            {
                count++;
            }
        }

        if (count < _minSelected)
        {
            return "Select at least " + Count(_minSelected, "item") + ".";
        }

        if (_maxSelected.HasValue && count > _maxSelected.Value)
        {
            return "Select at most " + Count(_maxSelected.Value, "item") + ".";
        }

        return Check(SelectedItems(state));
    }

    private IReadOnlyList<T> SelectedItems(bool[] state)
    {
        var items = new List<T>();
        for (int i = 0; i < _choices.Count; i++)
        {
            if (state[i])
            {
                items.Add(_choices[i]);
            }
        }

        return items;
    }

    private IEnumerable<string> SelectedLabels(bool[] state)
    {
        for (int i = 0; i < _choices.Count; i++)
        {
            if (state[i])
            {
                yield return Label(i);
            }
        }
    }
}
