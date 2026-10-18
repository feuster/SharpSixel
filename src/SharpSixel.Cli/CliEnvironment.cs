namespace SharpSixel.Cli;

/// <summary>Drawable terminal area in pixels; <see cref="Estimated"/> is true if cell size had to be guessed.</summary>
internal sealed record TerminalSize(int Width, int Height, bool Estimated);

/// <summary>I/O channels of the CLI; replaceable in tests.</summary>
internal sealed class CliEnvironment
{
    public CliEnvironment(
        Stream stdin,
        Stream stdout,
        TextWriter output,
        TextWriter error,
        Func<bool> detectSixelTerminal,
        Func<TerminalSize?> queryTerminalSize,
        Func<bool> isOutputTerminal)
    {
        Stdin = stdin;
        Stdout = stdout;
        Output = output;
        Error = error;
        DetectSixelTerminal = detectSixelTerminal;
        QueryTerminalSize = queryTerminalSize;
        IsOutputTerminal = isOutputTerminal;
    }

    /// <summary>Binary standard input (used for input "-").</summary>
    public Stream Stdin { get; }

    /// <summary>Binary standard output (sixel data when no output file is given).</summary>
    public Stream Stdout { get; }

    /// <summary>Text output for help and version.</summary>
    public TextWriter Output { get; }

    /// <summary>Text output for errors.</summary>
    public TextWriter Error { get; }

    /// <summary>Asks the attached terminal whether it supports sixel graphics.</summary>
    public Func<bool> DetectSixelTerminal { get; }

    /// <summary>Determines the drawable terminal area in pixels; null if no terminal is attached.</summary>
    public Func<TerminalSize?> QueryTerminalSize { get; }

    /// <summary>True if standard output is a terminal (not redirected to a file or pipe).</summary>
    public Func<bool> IsOutputTerminal { get; }
}
