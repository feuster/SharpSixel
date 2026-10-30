using System.Text;
using SharpSixel.Ui;

namespace SharpSixelUI;

/// <summary>
/// Reads keys and mouse reports from the standard input stream as VT sequences and turns them into <see cref="InputEvent"/>s.
/// The console must be in the mode of <c>ConsoleTerminal.EnterMouseInput</c> while reading. Ctrl+C (character 0x03) and the
/// end of the input end the reading with an <see cref="OperationCanceledException"/>.
/// </summary>
internal sealed class ConsoleInput
{
    private static readonly TimeSpan SequenceTimeout = TimeSpan.FromMilliseconds(50);

    private readonly Stream _stream = Console.OpenStandardInput();
    private readonly Decoder _decoder = Encoding.UTF8.GetDecoder();
    private readonly Queue<char> _chars = new Queue<char>();
    private readonly VtInputParser _parser;
    private Task<int>? _pending;

    public ConsoleInput()
    {
        _parser = new VtInputParser(() => ReadChar(null), () => ReadChar(SequenceTimeout));
    }

    public InputEvent Read()
    {
        if (Console.IsInputRedirected)
        {
            throw new InvalidOperationException("Keyboard input is not available (input is redirected).");
        }

        InputEvent input;
        try
        {
            input = _parser.Read();
        }
        catch (EndOfStreamException)
        {
            throw new OperationCanceledException();
        }

        if (input.Kind == InputKind.Key && input.Key.Key == ConsoleKey.C && (input.Key.Modifiers & ConsoleModifiers.Control) != 0)
        {
            throw new OperationCanceledException();
        }

        return input;
    }

    /// <summary>Next character; with a timeout -1 if none arrives in time (the pending read is kept for the next call), -1 also at the end of the input.</summary>
    private int ReadChar(TimeSpan? timeout)
    {
        var buffer = new char[2];
        while (_chars.Count == 0)
        {
            _pending ??= Task.Run(_stream.ReadByte);
            if (timeout is TimeSpan limit)
            {
                if (!_pending.Wait(limit))
                {
                    return -1;
                }
            }
            else
            {
                _pending.Wait();
            }

            int value = _pending.Result;
            _pending = null;
            if (value < 0)
            {
                return -1;
            }

            int count = _decoder.GetChars(new[] { (byte)value }, 0, 1, buffer, 0);
            for (int i = 0; i < count; i++)
            {
                _chars.Enqueue(buffer[i]);
            }
        }

        return _chars.Dequeue();
    }
}
