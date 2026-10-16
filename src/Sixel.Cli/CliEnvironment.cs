namespace Sixel.Cli;

/// <summary>I/O channels of the CLI; replaceable in tests.</summary>
internal sealed class CliEnvironment
{
    public CliEnvironment(Stream stdin, Stream stdout, TextWriter output, TextWriter error)
    {
        Stdin = stdin;
        Stdout = stdout;
        Output = output;
        Error = error;
    }

    /// <summary>Binary standard input (used for input "-").</summary>
    public Stream Stdin { get; }

    /// <summary>Binary standard output (sixel data when no output file is given).</summary>
    public Stream Stdout { get; }

    /// <summary>Text output for help and version.</summary>
    public TextWriter Output { get; }

    /// <summary>Text output for errors.</summary>
    public TextWriter Error { get; }
}
