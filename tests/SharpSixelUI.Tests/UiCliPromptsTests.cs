using SharpSixel;
using SharpSixelUI;

namespace SharpSixelUI.Tests;

public class UiCliPromptsTests
{
    private sealed class Disposable : IDisposable
    {
        private readonly Action _onDispose;

        public Disposable(Action onDispose) => _onDispose = onDispose;

        public void Dispose() => _onDispose();
    }

    private static readonly SixelTerminalInfo NoSixelInfo = new SixelTerminalInfo(false, 0, 0, 10, 20);

    private static ConsoleKeyInfo Key(ConsoleKey key, char c = '\0') => new ConsoleKeyInfo(c, key, false, false, false);

    private static void Type(Queue<ConsoleKeyInfo> keys, string text)
    {
        foreach (char c in text)
        {
            keys.Enqueue(Key((ConsoleKey)0, c));
        }

        keys.Enqueue(Key(ConsoleKey.Enter, '\r'));
    }

    private static (int Code, string Output, string Error, bool OutputModeUsed) Execute(
        string[] args, bool terminal, Queue<ConsoleKeyInfo> keys)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        bool outputModeUsed = false;
        var env = new UiCliEnvironment(
            output,
            error,
            () => terminal,
            () => NoSixelInfo,
            () => throw new InvalidOperationException("Raw mode must not be used by prompts."),
            _ => { },
            () => false,
            () =>
            {
                if (keys.Count == 0)
                {
                    throw new OperationCanceledException();
                }

                return keys.Dequeue();
            },
            () =>
            {
                outputModeUsed = true;
                return new Disposable(() => { });
            });
        int code = UiCliApp.Run(args, env);
        return (code, output.ToString(), error.ToString(), outputModeUsed);
    }

    [Fact]
    public void Prompts_RunsAllPromptsAndPrintsAnswers()
    {
        var keys = new Queue<ConsoleKeyInfo>();
        keys.Enqueue(Key(ConsoleKey.Enter, '\r'));            // dialog: Start
        keys.Enqueue(Key(ConsoleKey.DownArrow));
        keys.Enqueue(Key(ConsoleKey.Enter, '\r'));            // Pears
        keys.Enqueue(Key(ConsoleKey.DownArrow));
        keys.Enqueue(Key(ConsoleKey.Spacebar, ' '));          // Cream + Sugar
        keys.Enqueue(Key(ConsoleKey.Enter, '\r'));
        Type(keys, "Ada");
        Type(keys, "secret");

        var run = Execute(new[] { "prompts", "--mode", "ascii" }, true, keys);

        Assert.Equal(0, run.Code);
        Assert.True(run.OutputModeUsed);
        Assert.Contains("Selected: Pears", run.Output);
        Assert.Contains("Selected: Cream, Sugar", run.Output);
        Assert.Contains("Hello, Ada!", run.Output);
        Assert.Contains("Password length: 6", run.Output);
        Assert.DoesNotContain("secret", run.Output);
        Assert.Empty(keys);
    }

    [Fact]
    public void Prompts_ValidationMessagesAreShown()
    {
        var keys = new Queue<ConsoleKeyInfo>();
        keys.Enqueue(Key(ConsoleKey.Enter, '\r'));            // dialog: Start
        keys.Enqueue(Key(ConsoleKey.Enter, '\r'));            // Apples
        keys.Enqueue(Key(ConsoleKey.Spacebar, ' '));          // deselects preselected Cream
        keys.Enqueue(Key(ConsoleKey.Enter, '\r'));            // below minimum
        keys.Enqueue(Key(ConsoleKey.Spacebar, ' '));
        keys.Enqueue(Key(ConsoleKey.Enter, '\r'));
        Type(keys, "A1");                                     // pattern error
        keys.Enqueue(Key(ConsoleKey.Backspace, '\b'));
        keys.Enqueue(Key(ConsoleKey.Backspace, '\b'));
        Type(keys, "Bo");
        Type(keys, "abc");                                    // too short
        keys.Enqueue(Key(ConsoleKey.Backspace, '\b'));
        keys.Enqueue(Key(ConsoleKey.Backspace, '\b'));
        keys.Enqueue(Key(ConsoleKey.Backspace, '\b'));
        Type(keys, "abcd");

        var run = Execute(new[] { "prompts", "--mode", "ascii" }, true, keys);

        Assert.Equal(0, run.Code);
        Assert.Contains("! Select at least 1 item.", run.Output);
        Assert.Contains("! Letters, spaces and . ' - only.", run.Output);
        Assert.Contains("! Enter at least 4 characters.", run.Output);
    }

    [Fact]
    public void Prompts_DialogCancel_EndsWithoutFurtherPrompts()
    {
        var keys = new Queue<ConsoleKeyInfo>();
        keys.Enqueue(Key(ConsoleKey.RightArrow));
        keys.Enqueue(Key(ConsoleKey.Enter, '\r'));            // Cancel

        var run = Execute(new[] { "prompts", "--mode", "ascii" }, true, keys);

        Assert.Equal(0, run.Code);
        Assert.Contains("Prompts demo", run.Output);
        Assert.DoesNotContain("Favorite fruit?", run.Output);
        Assert.Empty(keys);
    }

    [Fact]
    public void Prompts_CtrlC_Cancels()
    {
        var run = Execute(new[] { "prompts", "--mode", "ascii" }, true, new Queue<ConsoleKeyInfo>());
        Assert.Equal(1, run.Code);
        Assert.Contains("Cancelled.", run.Error);
        Assert.Contains("\u001b[?25h", run.Output);
    }

    [Fact]
    public void Prompts_RequireTerminal()
    {
        var run = Execute(new[] { "prompts" }, false, new Queue<ConsoleKeyInfo>());
        Assert.Equal(1, run.Code);
        Assert.Contains("needs a terminal", run.Error);
        Assert.Equal(string.Empty, run.Output);
    }

    [Fact]
    public void Help_MentionsPrompts()
    {
        var run = Execute(new[] { "--help" }, false, new Queue<ConsoleKeyInfo>());
        Assert.Contains("prompts", run.Output);
    }
}
