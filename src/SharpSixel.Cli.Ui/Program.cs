using SharpSixel.Cli;
using SharpSixel.Ui;
using SharpSixelUI;

bool cancelled = false;
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancelled = true;
};

// Prompts read single keys; Ctrl+C is delivered as a key then (instead of the cancel event) and ends the prompt.
ConsoleKeyInfo ReadKey()
{
    if (Console.IsInputRedirected)
    {
        throw new InvalidOperationException("Keyboard input is not available (input is redirected).");
    }

    Console.TreatControlCAsInput = true;
    try
    {
        ConsoleKeyInfo key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.C && (key.Modifiers & ConsoleModifiers.Control) != 0)
        {
            throw new OperationCanceledException();
        }

        return key;
    }
    finally
    {
        Console.TreatControlCAsInput = false;
    }
}

ConsoleInput? consoleInput = null;
InputEvent ReadInput() => (consoleInput ??= new ConsoleInput()).Read();

return UiCliApp.Run(
    args,
    new UiCliEnvironment(
        Console.Out,
        Console.Error,
        () => !Console.IsOutputRedirected,
        ConsoleTerminal.QueryInfo,
        ConsoleTerminal.EnterTerminalMode,
        Thread.Sleep,
        () => cancelled,
        ReadKey,
        ConsoleTerminal.EnterVtOutput,
        ReadInput,
        ConsoleTerminal.EnterMouseInput));
