using SharpSixel.Cli;

return CliApp.Run(
    args,
    new CliEnvironment(
        Console.OpenStandardInput(),
        Console.OpenStandardOutput(),
        Console.Out,
        Console.Error,
        ConsoleTerminal.DetectSixel,
        ConsoleTerminal.QuerySize,
        () => !Console.IsOutputRedirected));
