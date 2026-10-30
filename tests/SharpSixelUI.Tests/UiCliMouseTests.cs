using SharpSixel;
using SharpSixel.Ui;
using SharpSixelUI;

namespace SharpSixelUI.Tests;

public class UiCliMouseTests
{
    private sealed class Disposable : IDisposable
    {
        private readonly Action _onDispose;

        public Disposable(Action onDispose) => _onDispose = onDispose;

        public void Dispose() => _onDispose();
    }

    private static readonly SixelTerminalInfo NoSixelInfo = new SixelTerminalInfo(false, 0, 0, 10, 20);

    private static InputEvent Origin(int row) => InputEvent.FromCursorPosition(row, 0);

    private static InputEvent Mouse(int column, int row, bool press) =>
        InputEvent.FromMouse(new MouseEvent(column, row, MouseButton.Left, press));

    private static InputEvent Key(ConsoleKey key, char c = '\0') =>
        InputEvent.FromKey(new ConsoleKeyInfo(c, key, false, false, false));

    private static (int Code, string Output, string Error, bool InputModeUsed, bool OutputModeUsed) Execute(
        string[] args, bool terminal, params InputEvent[] events)
    {
        var queue = new Queue<InputEvent>(events);
        var output = new StringWriter();
        var error = new StringWriter();
        bool inputModeUsed = false;
        bool outputModeUsed = false;
        var env = new UiCliEnvironment(
            output,
            error,
            () => terminal,
            () => NoSixelInfo,
            () => throw new InvalidOperationException("Terminal mode must not be used here."),
            _ => { },
            () => false,
            () => throw new OperationCanceledException(),
            () =>
            {
                outputModeUsed = true;
                return new Disposable(() => { });
            },
            () => queue.Count > 0 ? queue.Dequeue() : throw new OperationCanceledException(),
            () =>
            {
                inputModeUsed = true;
                return new Disposable(() => { });
            });
        int code = UiCliApp.Run(args, env);
        return (code, output.ToString(), error.ToString(), inputModeUsed, outputModeUsed);
    }

    [Fact]
    public void ButtonClick_PressAndReleaseOnButton_Clicks()
    {
        var run = Execute(new[] { "button", "--click", "--mode", "ascii" }, true, Origin(5), Mouse(3, 5, true), Mouse(3, 5, false));
        Assert.Equal(0, run.Code);
        Assert.True(run.InputModeUsed);
        Assert.Contains("Clicked.", run.Output);
        Assert.Contains("[#OK#]", run.Output);
        Assert.Contains("\u001b[?1000h", run.Output);
        Assert.Contains("\u001b[?1000l", run.Output);
    }

    [Fact]
    public void ButtonClick_ReleaseOutsideButton_DoesNotClick()
    {
        var run = Execute(
            new[] { "button", "--click", "--mode", "ascii" },
            true,
            Origin(5),
            Mouse(3, 5, true),
            Mouse(30, 5, false),
            Key(ConsoleKey.Enter, '\r'));
        Assert.Equal(0, run.Code);
        Assert.Contains("Clicked.", run.Output);
    }

    [Fact]
    public void ButtonClick_PressOutsideButton_DoesNotClick()
    {
        var run = Execute(
            new[] { "button", "--click", "--mode", "ascii" }, true, Origin(5), Mouse(30, 5, true), Mouse(30, 5, false));
        Assert.Equal(1, run.Code);
        Assert.Contains("Cancelled.", run.Output);
    }

    [Fact]
    public void ButtonClick_EscapeCancels()
    {
        var run = Execute(new[] { "button", "--click", "--mode", "ascii" }, true, Origin(5), Key(ConsoleKey.Escape, '\u001b'));
        Assert.Equal(1, run.Code);
        Assert.Contains("Cancelled.", run.Output);
    }

    [Fact]
    public void ButtonClick_NoMouse_ReadsKeysOnly()
    {
        var run = Execute(new[] { "button", "--click", "--no-mouse", "--mode", "ascii" }, true, Key(ConsoleKey.Spacebar, ' '));
        Assert.Equal(0, run.Code);
        Assert.Contains("Clicked.", run.Output);
        Assert.DoesNotContain("?1000h", run.Output);
    }

    [Fact]
    public void ButtonClick_RequiresTerminal()
    {
        var run = Execute(new[] { "button", "--click" }, false);
        Assert.Equal(1, run.Code);
        Assert.Contains("needs a terminal", run.Error);
    }

    [Fact]
    public void Prompts_WithMouse_ClickOnCancelButtonEndsDemo()
    {
        // The dialog has a title, two message rows and then the buttons; "Cancel" starts at text offset 11 (after "[ Start ]" and the gap).
        var run = Execute(new[] { "prompts", "--mode", "ascii" }, true, Origin(10), Mouse(15, 13, true));
        Assert.Equal(0, run.Code);
        Assert.True(run.InputModeUsed);
        Assert.False(run.OutputModeUsed);
        Assert.Contains("\u001b[?1000h", run.Output);
        Assert.DoesNotContain("Favorite fruit?", run.Output);
    }

    [Fact]
    public void Prompts_NoMouse_UsesKeyPathOnly()
    {
        var run = Execute(new[] { "prompts", "--mode", "ascii", "--no-mouse" }, true);
        Assert.Equal(1, run.Code);
        Assert.False(run.InputModeUsed);
        Assert.True(run.OutputModeUsed);
    }

    [Fact]
    public void Help_MentionsMouseOptions()
    {
        var run = Execute(new[] { "--help" }, false);
        Assert.Contains("--no-mouse", run.Output);
        Assert.Contains("--click", run.Output);
    }
}
