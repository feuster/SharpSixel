using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SharpSixel.Ui;

/// <summary>
/// Base of the interactive prompts. <see cref="Show(TextWriter, WidgetEnvironment, Func{ConsoleKeyInfo})"/> draws the prompt in place (sixel markers or ASCII, see
/// <see cref="WidgetEnvironment"/>), reads keys until the input is valid, removes the prompt and prints one summary line
/// (title and answer). Without ANSI and sixel support a plain line-based dialog is used instead. Labels and texts should fit
/// on one terminal row, otherwise the in-place redraw breaks.
/// </summary>
/// <typeparam name="TResult">Type of the answer.</typeparam>
public abstract class Prompt<TResult>
{
    internal Prompt(PromptStyle style)
    {
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    /// <summary>Look of the prompt; may be changed between calls of <see cref="Show(TextWriter, WidgetEnvironment, Func{ConsoleKeyInfo})"/>.</summary>
    public PromptStyle Style { get; }

    /// <summary>Optional title (question) shown above the choices or in front of the text input.</summary>
    public string? Title { get; set; }

    /// <summary>
    /// Optional custom validation of the answer, called after the built-in rules when the user confirms with Enter.
    /// An invalid result shows its message below the prompt and the user can correct the input.
    /// </summary>
    public Func<TResult, ValidationResult>? Validator { get; set; }

    internal string? Error { get; set; }

    internal abstract int RowCount { get; }

    internal abstract TResult Result { get; }

    internal abstract string Summary { get; }

    /// <summary>
    /// Shows the prompt and returns the validated answer.
    /// </summary>
    /// <param name="writer">Output (the terminal).</param>
    /// <param name="environment">Sixel or ASCII rendering.</param>
    /// <param name="readKey">Key source; null reads from the console without echo (<c>Console.ReadKey(true)</c>).</param>
    /// <exception cref="InvalidOperationException">The prompt is not configured consistently (for example no choices).</exception>
    public TResult Show(TextWriter writer, WidgetEnvironment environment, Func<ConsoleKeyInfo>? readKey = null)
    {
        Func<ConsoleKeyInfo> read = readKey ?? (() => Console.ReadKey(true));
        return Run(writer, environment, () => InputEvent.FromKey(read()), mouse: false, read);
    }

    /// <summary>
    /// Shows the prompt like <see cref="Show(TextWriter, WidgetEnvironment, Func{ConsoleKeyInfo})"/>, additionally with mouse
    /// support: a left click chooses (dialog button, selection item) or toggles (multi-selection item) the clicked row, the wheel
    /// moves the cursor. Mouse reporting is turned on while the prompt is shown (see <see cref="LiveRegion.EnableMouse"/>), so
    /// <paramref name="readInput"/> must deliver terminal input in VT mode, for example through a <see cref="VtInputParser"/>.
    /// Without ANSI/sixel support the plain dialog is used and only keys are read.
    /// </summary>
    /// <param name="writer">Output (the terminal).</param>
    /// <param name="environment">Sixel or ASCII rendering.</param>
    /// <param name="readInput">Source of keys and mouse reports.</param>
    /// <exception cref="InvalidOperationException">The prompt is not configured consistently (for example no choices).</exception>
    public TResult ShowWithMouse(TextWriter writer, WidgetEnvironment environment, Func<InputEvent> readInput)
    {
        if (readInput == null)
        {
            throw new ArgumentNullException(nameof(readInput));
        }

        return Run(writer, environment, readInput, mouse: true, NextKey(readInput));
    }

    private static Func<ConsoleKeyInfo> NextKey(Func<InputEvent> readInput) =>
        () =>
        {
            while (true)
            {
                InputEvent input = readInput();
                if (input.Kind == InputKind.Key)
                {
                    return input.Key;
                }
            }
        };

    private TResult Run(
        TextWriter writer, WidgetEnvironment environment, Func<InputEvent> readInput, bool mouse, Func<ConsoleKeyInfo> readKey)
    {
        if (writer == null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (environment == null)
        {
            throw new ArgumentNullException(nameof(environment));
        }

        Begin();
        Error = null;
        if (!environment.UseSixel && !environment.UseAnsi)
        {
            return RunPlain(writer, readKey);
        }

        return RunLive(writer, environment, readInput, mouse);
    }

    /// <summary>Checks the configuration and resets the input state.</summary>
    internal abstract void Begin();

    /// <summary>Builds exactly <see cref="RowCount"/> rows of the current frame.</summary>
    internal abstract void BuildRows(List<PromptRow> rows);

    /// <summary>Handles a key; true once a valid answer is confirmed.</summary>
    internal abstract bool OnKey(ConsoleKeyInfo key);

    /// <summary>Handles a left click at a position inside the prompt (column and row relative to its origin); true once a valid answer is confirmed.</summary>
    internal virtual bool OnMouse(int column, int row, WidgetEnvironment environment) => false;

    internal abstract TResult RunPlain(TextWriter writer, Func<ConsoleKeyInfo> readKey);

    /// <summary>Runs the custom validator; returns its error message or null.</summary>
    internal string? Check(TResult value)
    {
        ValidationResult? result = Validator?.Invoke(value);
        return result != null && !result.IsValid ? result.Message : null;
    }

    internal static string Count(int number, string noun) =>
        number.ToString(CultureInfo.InvariantCulture) + " " + noun + (number == 1 ? string.Empty : "s");

    internal bool HasTitle => !string.IsNullOrEmpty(Title);

    internal void AddTitleRow(List<PromptRow> rows)
    {
        if (HasTitle)
        {
            rows.Add(new PromptRow { Text = Title! });
        }
    }

    internal void AddHintRow(List<PromptRow> rows, string? hint)
    {
        if (!string.IsNullOrEmpty(hint))
        {
            rows.Add(new PromptRow { Text = hint!, TextColor = Style.InactiveColor });
        }
    }

    /// <summary>Adds the validation row: always present (empty without error) so the row count stays constant.</summary>
    internal void AddErrorRow(List<PromptRow> rows)
    {
        rows.Add(Error == null
            ? new PromptRow()
            : new PromptRow
            {
                AsciiPrefix = "! ",
                Text = Error,
                Shape = MarkerShape.Dot,
                MarkerColor = Style.ErrorColor,
                TextColor = Style.ErrorColor,
            });
    }

    private TResult RunLive(TextWriter writer, WidgetEnvironment environment, Func<InputEvent> readInput, bool mouse)
    {
        var view = new PromptView(Style);
        var region = new LiveRegion(writer, environment);
        try
        {
            writer.Write(AnsiCodes.HideCursor);
            region.Begin(RowCount + PromptView.ExtraRows(Style, environment));
            if (mouse)
            {
                region.EnableMouse(readInput);
            }

            bool done = false;
            while (!done)
            {
                view.Rows.Clear();
                BuildRows(view.Rows);
                region.Update(view);
                done = Handle(readInput(), region, environment);
            }
        }
        finally
        {
            region.End(clear: true);
            writer.Write(AnsiCodes.ShowCursor);
            writer.Flush();
        }

        string answer = AnsiCodes.Colorize(PromptRow.Clean(Summary), Style.ActiveColor, environment.UseAnsi);
        writer.WriteLine(HasTitle ? PromptRow.Clean(Title) + " " + answer : answer);
        writer.Flush();
        return Result;
    }

    private bool Handle(InputEvent input, LiveRegion region, WidgetEnvironment environment)
    {
        if (input.Kind == InputKind.Key)
        {
            return OnKey(input.Key);
        }

        MouseEvent? mouse = input.Mouse;
        if (mouse == null || !region.TryGetLocal(mouse, out int column, out int row))
        {
            return false;
        }

        if (PromptView.ExtraRows(Style, environment) > 0)
        {
            // Positions are relative to the frame: its border row and column are not part of the content.
            column--;
            row--;
        }

        if (mouse.Button == MouseButton.WheelUp || mouse.Button == MouseButton.WheelDown)
        {
            return OnKey(new ConsoleKeyInfo(
                '\0', mouse.Button == MouseButton.WheelUp ? ConsoleKey.UpArrow : ConsoleKey.DownArrow, false, false, false));
        }

        return mouse.Button == MouseButton.Left && mouse.IsPress && OnMouse(column, row, environment);
    }
}
