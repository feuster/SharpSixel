using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Sixel.Cli;

/// <summary>Probes the attached console for sixel support (raw mode, DA1 query, timeout).</summary>
internal static class ConsoleTerminal
{
    private static readonly TimeSpan AnswerTimeout = TimeSpan.FromMilliseconds(1000);

    /// <summary>Returns false if input or output is redirected, the terminal does not answer, or lacks sixel support.</summary>
    public static bool DetectSixel()
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            return false;
        }

        using (RawMode.Enter())
        using (Stream stdin = Console.OpenStandardInput())
        {
            var reader = new TimeoutReader(stdin, AnswerTimeout);
            return SixelTerminal.Detect(Console.Out, reader.ReadChar);
        }
    }

    /// <summary>Reads bytes from a blocking stream with an overall deadline.</summary>
    private sealed class TimeoutReader
    {
        private readonly Stream _stream;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly TimeSpan _timeout;
        private Task<int>? _pending;

        public TimeoutReader(Stream stream, TimeSpan timeout)
        {
            _stream = stream;
            _timeout = timeout;
        }

        public int ReadChar()
        {
            _pending ??= Task.Run(_stream.ReadByte);
            TimeSpan remaining = _timeout - _clock.Elapsed;
            if (remaining <= TimeSpan.Zero || !_pending.Wait(remaining))
            {
                return -1;
            }

            int value = _pending.Result;
            _pending = null;
            return value;
        }
    }

    /// <summary>Switches the console to unbuffered, non-echoing input with VT sequences and restores it on dispose.</summary>
    private sealed class RawMode : IDisposable
    {
        private const int StdInputHandle = -10;
        private const int StdOutputHandle = -11;
        private const uint EnableLineInput = 0x0002;
        private const uint EnableEchoInput = 0x0004;
        private const uint EnableVirtualTerminalProcessing = 0x0004;
        private const uint EnableVirtualTerminalInput = 0x0200;

        private readonly Action? _restore;

        private RawMode(Action? restore)
        {
            _restore = restore;
        }

        public static RawMode Enter() =>
            new RawMode(OperatingSystem.IsWindows() ? EnterWindows() : EnterUnix());

        public void Dispose() => _restore?.Invoke();

        private static Action? EnterWindows()
        {
            nint input = GetStdHandle(StdInputHandle);
            nint output = GetStdHandle(StdOutputHandle);
            if (!GetConsoleMode(input, out uint inputMode) || !GetConsoleMode(output, out uint outputMode))
            {
                return null;
            }

            SetConsoleMode(input, (inputMode & ~(EnableLineInput | EnableEchoInput)) | EnableVirtualTerminalInput);
            SetConsoleMode(output, outputMode | EnableVirtualTerminalProcessing);
            return () =>
            {
                SetConsoleMode(input, inputMode);
                SetConsoleMode(output, outputMode);
            };
        }

        private static Action? EnterUnix()
        {
            string? saved = RunStty("-g", captureOutput: true)?.Trim();
            if (string.IsNullOrEmpty(saved) || RunStty("-icanon -echo", captureOutput: false) == null)
            {
                return null;
            }

            return () => RunStty(saved, captureOutput: false);
        }

        /// <summary>Runs stty on the inherited terminal; returns captured output, or null if it failed.</summary>
        private static string? RunStty(string arguments, bool captureOutput)
        {
            try
            {
                var info = new ProcessStartInfo("stty", arguments)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = captureOutput,
                };
                using Process? process = Process.Start(info);
                if (process == null)
                {
                    return null;
                }

                string output = captureOutput ? process.StandardOutput.ReadToEnd() : string.Empty;
                process.WaitForExit();
                return process.ExitCode == 0 ? output : null;
            }
            catch (Win32Exception)
            {
                return null;
            }
        }

        [DllImport("kernel32.dll")]
        private static extern nint GetStdHandle(int handle);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetConsoleMode(nint handle, out uint mode);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetConsoleMode(nint handle, uint mode);
    }
}
