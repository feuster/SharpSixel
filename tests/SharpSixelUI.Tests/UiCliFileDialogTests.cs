using SharpSixel;
using SharpSixelUI;

namespace SharpSixelUI.Tests;

public sealed class UiCliFileDialogTests : IDisposable
{
    private sealed class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private static readonly SixelTerminalInfo NoSixelInfo = new SixelTerminalInfo(false, 0, 0, 10, 20);

    private readonly string _root;

    public UiCliFileDialogTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "SharpSixelUiCliFileDialog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "x.bmp"), "x");
        File.WriteAllText(Path.Combine(_root, "y.txt"), "y");
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private static ConsoleKeyInfo Key(ConsoleKey key, char c = '\0') => new ConsoleKeyInfo(c, key, false, false, false);

    private static void Type(Queue<ConsoleKeyInfo> keys, string text)
    {
        foreach (char c in text)
        {
            keys.Enqueue(Key((ConsoleKey)0, c));
        }
    }

    private static (int Code, string Output, string Error) Execute(string[] args, bool terminal, Queue<ConsoleKeyInfo> keys)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var env = new UiCliEnvironment(
            output,
            error,
            () => terminal,
            () => NoSixelInfo,
            () => throw new InvalidOperationException("Raw mode must not be used by dialogs."),
            _ => { },
            () => false,
            () => keys.Count == 0 ? throw new OperationCanceledException() : keys.Dequeue(),
            () => new Disposable());
        int code = UiCliApp.Run(args, env);
        return (code, output.ToString(), error.ToString());
    }

    [Fact]
    public void Open_ChoosesFileAndPrintsPath()
    {
        var keys = new Queue<ConsoleKeyInfo>();
        keys.Enqueue(Key(ConsoleKey.DownArrow));
        keys.Enqueue(Key(ConsoleKey.Enter, '\r'));

        var run = Execute(new[] { "open", "--mode", "ascii", "--dir", _root }, true, keys);

        Assert.Equal(0, run.Code);
        Assert.Contains("Open file", run.Output);
        Assert.Contains("Selected: " + Path.Combine(_root, "x.bmp"), run.Output);
        Assert.Empty(keys);
    }

    [Fact]
    public void Open_FilterAndCaptionAreApplied()
    {
        var keys = new Queue<ConsoleKeyInfo>();
        keys.Enqueue(Key(ConsoleKey.DownArrow));
        keys.Enqueue(Key(ConsoleKey.Enter, '\r'));

        var run = Execute(new[] { "open", "--mode", "ascii", "--dir", _root, "--filter", "*.txt", "--caption", "Pick one" }, true, keys);

        Assert.Equal(0, run.Code);
        Assert.Contains("Pick one", run.Output);
        Assert.DoesNotContain("x.bmp", run.Output);
        Assert.Contains("Selected: " + Path.Combine(_root, "y.txt"), run.Output);
    }

    [Fact]
    public void Save_TypedNamePrintsPath()
    {
        var keys = new Queue<ConsoleKeyInfo>();
        Type(keys, "out.sixel");
        keys.Enqueue(Key(ConsoleKey.Enter, '\r'));

        var run = Execute(new[] { "save", "--mode", "ascii", "--dir", _root }, true, keys);

        Assert.Equal(0, run.Code);
        Assert.Contains("Save file", run.Output);
        Assert.Contains("Save as: " + Path.Combine(_root, "out.sixel"), run.Output);
        Assert.False(File.Exists(Path.Combine(_root, "out.sixel")));
    }

    [Fact]
    public void Escape_Cancels()
    {
        var keys = new Queue<ConsoleKeyInfo>();
        keys.Enqueue(Key(ConsoleKey.Escape));

        var run = Execute(new[] { "open", "--mode", "ascii", "--dir", _root }, true, keys);

        Assert.Equal(1, run.Code);
        Assert.Contains("Cancelled.", run.Error);
        Assert.Contains("\u001b[?25h", run.Output);
    }

    [Fact]
    public void CtrlC_Cancels()
    {
        var run = Execute(new[] { "save", "--mode", "ascii", "--dir", _root }, true, new Queue<ConsoleKeyInfo>());
        Assert.Equal(1, run.Code);
        Assert.Contains("Cancelled.", run.Error);
    }

    [Fact]
    public void MissingDirectory_IsReported()
    {
        var run = Execute(new[] { "open", "--mode", "ascii", "--dir", Path.Combine(_root, "nope") }, true, new Queue<ConsoleKeyInfo>());
        Assert.Equal(1, run.Code);
        Assert.Contains("initial directory does not exist", run.Error);
    }

    [Theory]
    [InlineData("open")]
    [InlineData("save")]
    public void RequireTerminal(string command)
    {
        var run = Execute(new[] { command }, false, new Queue<ConsoleKeyInfo>());
        Assert.Equal(1, run.Code);
        Assert.Contains("need a terminal", run.Error);
        Assert.Equal(string.Empty, run.Output);
    }

    [Fact]
    public void Help_MentionsOpenSaveAndOptions()
    {
        var run = Execute(new[] { "--help" }, false, new Queue<ConsoleKeyInfo>());
        Assert.Contains("open|save", run.Output);
        Assert.Contains("--dir", run.Output);
        Assert.Contains("--filter", run.Output);
    }

    [Fact]
    public void Arguments_ParseDirAndFilter()
    {
        Assert.True(UiCliArguments.TryParse(new[] { "open", "--dir", "C:\\x", "--filter=*.bmp" }, out UiCliArguments result, out _));
        Assert.Equal("open", result.Command);
        Assert.Equal("C:\\x", result.StartDirectory);
        Assert.Equal("*.bmp", result.Filter);
        Assert.False(UiCliArguments.TryParse(new[] { "open", "--dir" }, out _, out string error));
        Assert.Contains("Missing value for --dir", error);
    }
}
