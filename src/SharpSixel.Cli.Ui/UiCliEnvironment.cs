using SharpSixel;
using SharpSixel.Ui;

namespace SharpSixelUI;

/// <summary>Terminal and timing channels of the demo CLI; replaceable in tests.</summary>
internal sealed class UiCliEnvironment
{
    public UiCliEnvironment(
        TextWriter output,
        TextWriter error,
        Func<bool> isOutputTerminal,
        Func<SixelTerminalInfo?> queryTerminal,
        Func<IDisposable> enterTerminalMode,
        Action<int> delay,
        Func<bool> isCancelled,
        Func<ConsoleKeyInfo>? readKey = null,
        Func<IDisposable>? enterOutputMode = null,
        Func<InputEvent>? readInput = null,
        Func<IDisposable>? enterInputMode = null,
        Func<DateTime>? now = null)
    {
        Output = output;
        Error = error;
        IsOutputTerminal = isOutputTerminal;
        QueryTerminal = queryTerminal;
        EnterTerminalMode = enterTerminalMode;
        Delay = delay;
        IsCancelled = isCancelled;
        ReadKey = readKey ?? (() => Console.ReadKey(true));
        EnterOutputMode = enterOutputMode ?? enterTerminalMode;
        ReadInput = readInput;
        EnterInputMode = enterInputMode;
        Now = now ?? (() => DateTime.Now);
    }

    /// <summary>Output for widgets and help.</summary>
    public TextWriter Output { get; }

    /// <summary>Output for errors.</summary>
    public TextWriter Error { get; }

    /// <summary>True if output goes to a terminal (not redirected).</summary>
    public Func<bool> IsOutputTerminal { get; }

    /// <summary>Queries sixel support and cell size; null if unavailable.</summary>
    public Func<SixelTerminalInfo?> QueryTerminal { get; }

    /// <summary>Enables VT sequences / raw mode until disposed.</summary>
    public Func<IDisposable> EnterTerminalMode { get; }

    /// <summary>Waits the given number of milliseconds.</summary>
    public Action<int> Delay { get; }

    /// <summary>True if the user requested cancellation (Ctrl+C).</summary>
    public Func<bool> IsCancelled { get; }

    /// <summary>Reads one key without echo (prompts); throws <see cref="OperationCanceledException"/> on Ctrl+C.</summary>
    public Func<ConsoleKeyInfo> ReadKey { get; }

    /// <summary>Enables VT sequences for output only, leaving keyboard input untouched (prompts read keys via <see cref="ReadKey"/>).</summary>
    public Func<IDisposable> EnterOutputMode { get; }

    /// <summary>Reads keys and mouse reports as VT input; null if mouse input is not available (prompts then read keys only).</summary>
    public Func<InputEvent>? ReadInput { get; }

    /// <summary>Puts the terminal in the raw VT input mode that <see cref="ReadInput"/> needs until disposed.</summary>
    public Func<IDisposable>? EnterInputMode { get; }

    /// <summary>Current local date and time (clock and date commands); the system time by default.</summary>
    public Func<DateTime> Now { get; }
}
