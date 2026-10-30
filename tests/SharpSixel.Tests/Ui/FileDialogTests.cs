using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public sealed class FileDialogTests : IDisposable
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment AnsiAscii = new WidgetEnvironment(false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);

    private readonly string _root;

    public FileDialogTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "SharpSixelFileDialog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "sub"));
        File.WriteAllText(Path.Combine(_root, "a.bmp"), "a");
        File.WriteAllText(Path.Combine(_root, "b.sixel"), "b");
        File.WriteAllText(Path.Combine(_root, "c.txt"), "c");
        File.WriteAllText(Path.Combine(_root, "sub", "d.bmp"), "d");
        string hidden = Path.Combine(_root, ".hidden.bmp");
        File.WriteAllText(hidden, "h");
        File.SetAttributes(hidden, FileAttributes.Hidden);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string P(params string[] parts) => Path.Combine(_root, Path.Combine(parts));

    private OpenFileDialog Open() => new OpenFileDialog { InitialDirectory = _root };

    private SaveFileDialog Save() => new SaveFileDialog { InitialDirectory = _root };

    private static string Show(FileDialog dialog, KeyScript keys, WidgetEnvironment env, out string result)
    {
        var writer = new StringWriter();
        result = dialog.Show(writer, env, keys.Read);
        return writer.ToString();
    }

    private static Func<InputEvent> Script(params InputEvent[] events)
    {
        var queue = new Queue<InputEvent>(events);
        return () => queue.Count > 0 ? queue.Dequeue() : throw new InvalidOperationException("The input script is exhausted.");
    }

    private static InputEvent Origin() => InputEvent.FromCursorPosition(5, 0);

    private static InputEvent Click(int column, int row) => InputEvent.FromMouse(new MouseEvent(column, row, MouseButton.Left, true));

    // ---- Open: listing and navigation ----

    [Fact]
    public void Open_ListsParentDirectoriesThenFilesSorted_HidesHidden()
    {
        string output = Show(Open(), new KeyScript().Down(2).Enter(), AnsiAscii, out _);
        int parent = output.IndexOf(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        int sub = output.IndexOf("sub" + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        int a = output.IndexOf("a.bmp", StringComparison.Ordinal);
        int b = output.IndexOf("b.sixel", StringComparison.Ordinal);
        int c = output.IndexOf("c.txt", StringComparison.Ordinal);
        Assert.True(parent >= 0 && parent < sub && sub < a && a < b && b < c);
        Assert.DoesNotContain(".hidden.bmp", output, StringComparison.Ordinal);
        Assert.Contains(_root, output, StringComparison.Ordinal);
    }

    [Fact]
    public void Open_ShowHidden_ListsHiddenEntries()
    {
        FileDialog dialog = Open();
        dialog.ShowHidden = true;
        Assert.Contains(".hidden.bmp", Show(dialog, new KeyScript().Down(2).Enter(), AnsiAscii, out _), StringComparison.Ordinal);
    }

    [Fact]
    public void Open_EnterOnFile_ReturnsFullPath()
    {
        Show(Open(), new KeyScript().Down(2).Enter(), AnsiAscii, out string result);
        Assert.Equal(P("a.bmp"), result);
    }

    [Fact]
    public void Open_EnterOnDirectory_NavigatesIntoIt()
    {
        Show(Open(), new KeyScript().Down().Enter().Down().Enter(), AnsiAscii, out string result);
        Assert.Equal(P("sub", "d.bmp"), result);
    }

    [Fact]
    public void Open_EnterOnParent_NavigatesUp()
    {
        FileDialog dialog = Open();
        dialog.InitialDirectory = P("sub");
        Show(dialog, new KeyScript().Enter().Down(2).Enter(), AnsiAscii, out string result);
        Assert.Equal(P("a.bmp"), result);
    }

    [Fact]
    public void Open_InitialDirectoryWithTrailingSeparator_IsNormalized()
    {
        FileDialog dialog = Open();
        dialog.InitialDirectory = P("sub") + Path.DirectorySeparatorChar;
        Show(dialog, new KeyScript().Enter().Down(2).Enter(), AnsiAscii, out string result);
        Assert.Equal(P("a.bmp"), result);
    }

    [Fact]
    public void Open_Filter_ListsMatchingFilesOnly()
    {
        FileDialog dialog = Open();
        dialog.Filter = "*.BMP; *.sixe?";
        string output = Show(dialog, new KeyScript().Down(2).Enter(), AnsiAscii, out string result);
        Assert.Equal(P("a.bmp"), result);
        Assert.Contains("b.sixel", output, StringComparison.Ordinal);
        Assert.DoesNotContain("c.txt", output, StringComparison.Ordinal);
        Assert.Contains("sub" + Path.DirectorySeparatorChar, output, StringComparison.Ordinal);
    }

    [Fact]
    public void Open_Navigation_WrapsAndPages()
    {
        FileDialog dialog = Open();
        dialog.PageSize = 2;
        Show(dialog, new KeyScript().End().Enter(), AnsiAscii, out string end);
        Assert.Equal(P("c.txt"), end);
        Show(dialog, new KeyScript().Up().Enter(), AnsiAscii, out string up);
        Assert.Equal(P("c.txt"), up);
        Show(dialog, new KeyScript().PageDown().Enter(), AnsiAscii, out string paged);
        Assert.Equal(P("a.bmp"), paged);
    }

    [Fact]
    public void Open_TypingIsIgnored()
    {
        Show(Open(), new KeyScript().Type("xyz").Press(ConsoleKey.Backspace, '\b').Down(2).Enter(), AnsiAscii, out string result);
        Assert.Equal(P("a.bmp"), result);
    }

    [Fact]
    public void Open_Validator_RejectsFile()
    {
        FileDialog dialog = Open();
        dialog.Validator = p => p.EndsWith("a.bmp", StringComparison.Ordinal) ? ValidationResult.Error("Not a.") : ValidationResult.Success();
        string output = Show(dialog, new KeyScript().Down(2).Enter().Down().Enter(), AnsiAscii, out string result);
        Assert.Equal(P("b.sixel"), result);
        Assert.Contains("! Not a.", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Escape_ThrowsOperationCancelledAndRestoresCursor()
    {
        var writer = new StringWriter();
        Assert.Throws<OperationCanceledException>(() => Open().Show(writer, AnsiAscii, new KeyScript().Press(ConsoleKey.Escape).Read));
        Assert.EndsWith("\u001b[?25h", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Ascii_ShowsTitleDirectoryMarkersAndInstructions()
    {
        FileDialog dialog = Open();
        dialog.Title = "Open file";
        string output = Show(dialog, new KeyScript().Down(2).Enter(), AnsiAscii, out _);
        Assert.Contains("Open file", output, StringComparison.Ordinal);
        Assert.Contains("> (*) ..", output, StringComparison.Ordinal);
        Assert.Contains("  ( ) a.bmp", output, StringComparison.Ordinal);
        Assert.Contains("Enter to open", output, StringComparison.Ordinal);
        Assert.EndsWith(P("a.bmp") + "\u001b[0m" + Environment.NewLine, output, StringComparison.Ordinal);
    }

    [Fact]
    public void LongNames_AreShortenedWithEllipsis()
    {
        File.WriteAllText(P(new string('x', 40) + ".bmp"), "x");
        FileDialog dialog = Open();
        dialog.Width = 20;
        var writer = new StringWriter();
        Assert.Throws<OperationCanceledException>(
            () => dialog.Show(writer, AnsiAscii, new KeyScript().End().Press(ConsoleKey.Escape).Read));
        string output = writer.ToString();
        Assert.Contains(new string('x', 19) + "\u2026", output, StringComparison.Ordinal);
        Assert.DoesNotContain(new string('x', 20), output, StringComparison.Ordinal);
    }

    [Fact]
    public void RowCount_IsConstantAndReservesPageSizeRows()
    {
        FileDialog dialog = Open();
        dialog.PageSize = 3;
        var writer = new StringWriter();
        dialog.Show(writer, AnsiAscii, new KeyScript().Down(2).Enter().Read);
        // 1 directory + 3 list + 1 instructions + 1 error row = 6 rows (5 newlines).
        Assert.Contains("\r\n\n\n\n\n\u001b[5A", writer.ToString(), StringComparison.Ordinal);

        FileDialog padded = Open();
        padded.InitialDirectory = P("sub");
        var second = new StringWriter();
        padded.Show(second, AnsiAscii, new KeyScript().Down().Enter().Read);
        // default PageSize 10: 1 + 10 + 1 + 1 = 13 rows although the directory has two entries.
        Assert.Contains("\u001b[12A", second.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Sixel_DrawsMarkerGraphic()
    {
        string output = Show(Open(), new KeyScript().Down(2).Enter(), SixelEnv, out string result);
        Assert.Equal(P("a.bmp"), result);
        Assert.Contains("\u001bP", output, StringComparison.Ordinal);
        Assert.Contains("\u001b[4C", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Configuration_IsValidated()
    {
        FileDialog missing = Open();
        missing.InitialDirectory = P("nope");
        Assert.Throws<InvalidOperationException>(() => Show(missing, new KeyScript(), AnsiAscii, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OpenFileDialog().PageSize = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => new OpenFileDialog().Width = 19);
        Assert.Throws<ArgumentNullException>(() => new SaveFileDialog(null!));
    }

    [Fact]
    public void WindowsDriveRoot_ParentListsDrives()
    {
        if (Path.DirectorySeparatorChar != '\\')
        {
            return;
        }

        FileDialog dialog = Open();
        dialog.InitialDirectory = Path.GetPathRoot(_root);
        var writer = new StringWriter();
        Assert.Throws<OperationCanceledException>(
            () => dialog.Show(writer, AnsiAscii, new KeyScript().Enter().Press(ConsoleKey.Escape).Read));
        Assert.Contains("Drives", writer.ToString(), StringComparison.Ordinal);
        Assert.Contains(Directory.GetLogicalDrives()[0], writer.ToString(), StringComparison.Ordinal);
    }

    // ---- Save ----

    [Fact]
    public void Save_TypedName_ReturnsPathInCurrentDirectory()
    {
        Show(Save(), new KeyScript().Type("new.sixel").Enter(), AnsiAscii, out string result);
        Assert.Equal(P("new.sixel"), result);
    }

    [Fact]
    public void Save_FileNameIsPrefilledAndBackspaceEdits()
    {
        SaveFileDialog dialog = Save();
        dialog.FileName = "out.bmpx";
        string output = Show(dialog, new KeyScript().Backspace().Enter(), AnsiAscii, out string result);
        Assert.Equal(P("out.bmp"), result);
        Assert.Contains("File name: out.bmpx", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Save_ExistingFile_NeedsSecondEnter()
    {
        string output = Show(Save(), new KeyScript().Type("a.bmp").Enter().Enter(), AnsiAscii, out string result);
        Assert.Equal(P("a.bmp"), result);
        Assert.Contains("! File exists. Press Enter again to overwrite.", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Save_OverwriteConfirmationIsResetByOtherKeys()
    {
        string output = Show(Save(), new KeyScript().Type("a.bmp").Enter().Backspace().Type("p").Enter().Enter(), AnsiAscii, out string result);
        Assert.Equal(P("a.bmp"), result);
        Assert.Equal(2, output.Split(new[] { "File exists." }, StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void Save_ConfirmOverwriteOff_AcceptsExistingFile()
    {
        SaveFileDialog dialog = Save();
        dialog.ConfirmOverwrite = false;
        Show(dialog, new KeyScript().Type("a.bmp").Enter(), AnsiAscii, out string result);
        Assert.Equal(P("a.bmp"), result);
    }

    [Fact]
    public void Save_EnterOnListedFile_CopiesNameAndAsksToOverwrite()
    {
        string output = Show(Save(), new KeyScript().Down(2).Enter().Enter(), AnsiAscii, out string result);
        Assert.Equal(P("a.bmp"), result);
        Assert.Contains("File name: a.bmp", output, StringComparison.Ordinal);
        Assert.Contains("File exists.", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Save_EnterOnDirectory_NavigatesAndSavesThere()
    {
        Show(Save(), new KeyScript().Down().Enter().Type("n.bmp").Enter(), AnsiAscii, out string result);
        Assert.Equal(P("sub", "n.bmp"), result);
    }

    [Fact]
    public void Save_RejectsEmptyInvalidAndDirectoryNames()
    {
        string output = Show(
            Save(),
            new KeyScript().Enter().Type("a/b").Enter().Backspace().Backspace().Backspace().Type("sub").Enter().Backspace().Backspace().Backspace().Type("ok").Enter(),
            AnsiAscii,
            out string result);
        Assert.Equal(P("ok"), result);
        Assert.Contains("! Enter a file name.", output, StringComparison.Ordinal);
        Assert.Contains("! Enter a valid file name.", output, StringComparison.Ordinal);
        Assert.Contains("! A directory with this name exists.", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Save_Validator_RejectsPath()
    {
        SaveFileDialog dialog = Save();
        dialog.Validator = p => p.EndsWith(".exe", StringComparison.Ordinal) ? ValidationResult.Error("No exe.") : ValidationResult.Success();
        string output = Show(dialog, new KeyScript().Type("x.exe").Enter().Backspace().Backspace().Backspace().Type("bmp").Enter(), AnsiAscii, out string result);
        Assert.Equal(P("x.bmp"), result);
        Assert.Contains("! No exe.", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Save_ReservesNameRow()
    {
        var writer = new StringWriter();
        SaveFileDialog dialog = Save();
        dialog.PageSize = 3;
        dialog.Show(writer, AnsiAscii, new KeyScript().Type("n").Enter().Read);
        // 1 directory + 3 list + 1 name + 1 instructions + 1 error row = 7 rows.
        Assert.Contains("\r\n\n\n\n\n\n\u001b[6A", writer.ToString(), StringComparison.Ordinal);
    }

    // ---- Mouse ----

    [Fact]
    public void Mouse_ClickOpensDirectoryAndChoosesFile()
    {
        var writer = new StringWriter();
        string result = Open().ShowWithMouse(writer, AnsiAscii, Script(Origin(), Click(3, 7), Click(3, 7)));
        Assert.Equal(P("sub", "d.bmp"), result);
    }

    [Fact]
    public void Mouse_ClickOnFile_Chooses()
    {
        Assert.Equal(P("a.bmp"), Open().ShowWithMouse(new StringWriter(), AnsiAscii, Script(Origin(), Click(3, 8))));
    }

    [Fact]
    public void Mouse_ClickOutsideEntries_IsIgnored()
    {
        InputEvent Key(ConsoleKey key, char c = '\0') => InputEvent.FromKey(new ConsoleKeyInfo(c, key, false, false, false));
        string result = Open().ShowWithMouse(
            new StringWriter(),
            AnsiAscii,
            Script(Origin(), Click(3, 5), Click(3, 14), Click(3, 3), Key(ConsoleKey.DownArrow), Key(ConsoleKey.DownArrow), Key(ConsoleKey.Enter, '\r')));
        Assert.Equal(P("a.bmp"), result);
    }

    // ---- Plain ----

    [Fact]
    public void Plain_Open_ResolvesRelativeNameAndReportsErrors()
    {
        string output = Show(Open(), new KeyScript().Enter().Type("missing.bmp").Enter().Type("a.bmp").Enter(), Plain, out string result);
        Assert.Equal(P("a.bmp"), result);
        Assert.Contains("! Enter a file name or path.", output, StringComparison.Ordinal);
        Assert.Contains("! The file does not exist.", output, StringComparison.Ordinal);
        Assert.DoesNotContain("\u001b", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Plain_Open_AcceptsFullPath()
    {
        Show(Open(), new KeyScript().Type(P("sub", "d.bmp")).Enter(), Plain, out string result);
        Assert.Equal(P("sub", "d.bmp"), result);
    }

    [Fact]
    public void Plain_Save_OverwriteQuestionAndDefaultName()
    {
        SaveFileDialog dialog = Save();
        dialog.FileName = "a.bmp";
        string output = Show(dialog, new KeyScript().Enter().Type("n").Enter().Type("fresh.bmp").Enter(), Plain, out string result);
        Assert.Equal(P("fresh.bmp"), result);
        Assert.Contains("File name or path (a.bmp): ", output, StringComparison.Ordinal);
        Assert.Contains("File exists. Overwrite? (y/n): ", output, StringComparison.Ordinal);

        Show(dialog, new KeyScript().Enter().Type("y").Enter(), Plain, out string overwritten);
        Assert.Equal(P("a.bmp"), overwritten);
    }

    [Fact]
    public void Plain_Save_RejectsDirectoryAndMissingParent()
    {
        string output = Show(
            Save(), new KeyScript().Type("sub").Enter().Type(P("nope", "x.bmp")).Enter().Type("x.bmp").Enter(), Plain, out string result);
        Assert.Equal(P("x.bmp"), result);
        Assert.Contains("! A directory with this name exists.", output, StringComparison.Ordinal);
        Assert.Contains("! The directory does not exist.", output, StringComparison.Ordinal);
    }
}
