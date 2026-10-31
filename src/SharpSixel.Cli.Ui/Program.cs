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

// Terminal width in cells for widgets with a flexible width; 0 if unknown or output is redirected.
int ConsoleColumns()
{
    try
    {
        return Console.IsOutputRedirected ? 0 : Console.WindowWidth;
    }
    catch (IOException)
    {
        return 0;
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
        ConsoleTerminal.EnterMouseInput,
        getColumns: ConsoleColumns));
