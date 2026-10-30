namespace SharpSixel.Ui;

/// <summary>
/// Open-file dialog: browse the file system (see <see cref="FileDialog"/>) and choose an existing file;
/// <see cref="Prompt{TResult}.Show(System.IO.TextWriter, WidgetEnvironment, System.Func{System.ConsoleKeyInfo})"/> returns its full path.
/// </summary>
public sealed class OpenFileDialog : FileDialog
{
    /// <summary>Creates an open-file dialog with the default style.</summary>
    public OpenFileDialog()
        : this(new PromptStyle())
    {
    }

    /// <summary>Creates an open-file dialog with the given style.</summary>
    public OpenFileDialog(PromptStyle style)
        : base(style, "(Up/Down to move, Enter to open, Esc to cancel)")
    {
    }
}
