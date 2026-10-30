using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpSixel.Ui;

/// <summary>
/// Free text input on one line: typed characters are appended, Backspace removes the last one, Enter confirms (there is no
/// cursor movement inside the text). Built-in validation: <see cref="AllowEmpty"/>, <see cref="MinLength"/>,
/// <see cref="MaxLength"/> (also stops typing) and <see cref="Pattern"/>; the optional <see cref="Prompt{TResult}.Validator"/>
/// runs afterwards. The sixel marker in front of the title is a dot; the caret is drawn as reverse-video cell
/// (underscore without ANSI). Without ANSI/sixel support the dialog reads a plain line.
/// </summary>
public sealed class TextPrompt : Prompt<string>
{
    private readonly StringBuilder _input = new StringBuilder();
    private int _minLength;
    private int? _maxLength;
    private string _result = string.Empty;

    /// <summary>Creates a text prompt with the default style.</summary>
    public TextPrompt()
        : this(new PromptStyle())
    {
    }

    /// <summary>Creates a text prompt with the given style.</summary>
    public TextPrompt(PromptStyle style)
        : base(style)
    {
    }

    /// <summary>Answer used (and shown in parentheses) if the user confirms an empty input; not shown for secret input. Null = none.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>True to accept an empty answer. Default false (a value is required unless <see cref="DefaultValue"/> is set).</summary>
    public bool AllowEmpty { get; set; }

    /// <summary>Minimum length of a non-empty answer (0 = no limit, default).</summary>
    public int MinLength
    {
        get => _minLength;
        set => _minLength = Guard.AtLeast(value, 0, nameof(value));
    }

    /// <summary>Maximum length of the answer; null (default) = unlimited. Must not be below <see cref="MinLength"/>.</summary>
    public int? MaxLength
    {
        get => _maxLength;
        set => _maxLength = value.HasValue ? Guard.AtLeast(value.Value, 1, nameof(value)) : (int?)null;
    }

    /// <summary>Format a non-empty answer must match; null = any.</summary>
    public Regex? Pattern { get; set; }

    /// <summary>Message if <see cref="Pattern"/> does not match; null uses a default text.</summary>
    public string? PatternMessage { get; set; }

    /// <summary>True to show <see cref="Mask"/> instead of the typed characters (passwords).</summary>
    public bool IsSecret { get; set; }

    /// <summary>Character shown for each typed character of a secret input. Default '*'.</summary>
    public char Mask { get; set; } = '*';

    internal override int RowCount => 2;

    internal override string Result => _result;

    internal override string Summary => IsSecret ? new string(Mask, _result.Length) : _result;

    internal override void Begin()
    {
        if (_maxLength.HasValue && _maxLength.Value < _minLength)
        {
            throw new InvalidOperationException("MaxLength is below MinLength.");
        }

        _input.Clear();
        _result = string.Empty;
    }

    internal override void BuildRows(List<PromptRow> rows)
    {
        string shown = IsSecret ? new string(Mask, _input.Length) : _input.ToString();
        rows.Add(new PromptRow
        {
            Text = FieldPrefix() + shown,
            Shape = MarkerShape.Dot,
            MarkerColor = Style.ActiveColor,
            Caret = true,
        });
        AddErrorRow(rows);
    }

    internal override bool OnKey(ConsoleKeyInfo key)
    {
        Error = null;
        if (key.Key == ConsoleKey.Enter)
        {
            string value = CurrentValue();
            Error = Validate(value);
            if (Error != null)
            {
                return false;
            }

            _result = value;
            return true;
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (_input.Length > 0)
            {
                _input.Length--;
            }
        }
        else if (!char.IsControl(key.KeyChar) && (!_maxLength.HasValue || _input.Length < _maxLength.Value))
        {
            _input.Append(key.KeyChar);
        }

        return false;
    }

    internal override string RunPlain(TextWriter writer, Func<ConsoleKeyInfo> readKey)
    {
        while (true)
        {
            writer.Write(FieldPrefix());
            writer.Flush();
            string line = LineReader.ReadLine(writer, readKey, IsSecret ? Mask : (char?)null);
            string value = line.Length == 0 && DefaultValue != null ? DefaultValue : line;
            string? error = Validate(value);
            if (error == null)
            {
                return value;
            }

            writer.WriteLine("! " + PromptRow.Clean(error));
        }
    }

    private string CurrentValue() => _input.Length == 0 && DefaultValue != null ? DefaultValue : _input.ToString();

    /// <summary>Title and default hint followed by a space, e.g. <c>Name (Bob) </c>.</summary>
    private string FieldPrefix()
    {
        var sb = new StringBuilder(PromptRow.Clean(Title));
        if (!IsSecret && !string.IsNullOrEmpty(DefaultValue))
        {
            sb.Append(" (").Append(PromptRow.Clean(DefaultValue)).Append(')');
        }

        if (sb.Length > 0)
        {
            sb.Append(' ');
        }

        return sb.ToString();
    }

    /// <summary>Built-in rules first, then the custom validator.</summary>
    private string? Validate(string value)
    {
        if (value.Length == 0)
        {
            if (!AllowEmpty)
            {
                return "A value is required.";
            }
        }
        else
        {
            if (value.Length < _minLength)
            {
                return "Enter at least " + Count(_minLength, "character") + ".";
            }

            if (_maxLength.HasValue && value.Length > _maxLength.Value)
            {
                return "Enter at most " + Count(_maxLength.Value, "character") + ".";
            }

            if (Pattern != null && !Pattern.IsMatch(value))
            {
                return PatternMessage ?? "The input has an invalid format.";
            }
        }

        return Check(value);
    }
}
