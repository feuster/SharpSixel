namespace SharpSixel.Ui;

/// <summary>
/// Save-file dialog: browse the file system (see <see cref="FileDialog"/>) and choose the path of the file to write; nothing is
/// written. Below the list is a name field: typing (or Backspace) edits it, Enter confirms the name in the shown directory;
/// Up/Down (and the other list keys) move to the list, where Enter on a file copies its name and confirms, and on a directory
/// opens it. With <see cref="ConfirmOverwrite"/> an existing file must be confirmed by pressing Enter (or clicking) a second time.
/// <see cref="Prompt{TResult}.Show(System.IO.TextWriter, WidgetEnvironment, System.Func{System.ConsoleKeyInfo})"/> returns the full path.
/// </summary>
public sealed class SaveFileDialog : FileDialog
{
    /// <summary>Creates a save-file dialog with the default style.</summary>
    public SaveFileDialog()
        : this(new PromptStyle())
    {
    }

    /// <summary>Creates a save-file dialog with the given style.</summary>
    public SaveFileDialog(PromptStyle style)
        : base(style, "(Type a name, Up/Down to browse, Enter to save, Esc to cancel)")
    {
    }

    /// <summary>Initial content of the name field (also the answer of an empty input without ANSI/sixel support). Null = empty.</summary>
    public string? FileName { get; set; }

    /// <summary>True (default) to ask for confirmation before an existing file is chosen.</summary>
    public bool ConfirmOverwrite { get; set; } = true;

    internal override bool IsSave => true;

    internal override string InitialName => FileName ?? string.Empty;

    internal override bool ConfirmOverwriteEnabled => ConfirmOverwrite;
}
