namespace SharpSixel.Tests.Ui;

/// <summary>Scripted key source for prompt tests; throws when the script is exhausted (a prompt waiting for more keys would hang).</summary>
internal sealed class KeyScript
{
    private readonly Queue<ConsoleKeyInfo> _keys = new Queue<ConsoleKeyInfo>();

    public int Remaining => _keys.Count;

    public KeyScript Press(ConsoleKey key, char keyChar = '\0')
    {
        _keys.Enqueue(new ConsoleKeyInfo(keyChar, key, false, false, false));
        return this;
    }

    public KeyScript Up() => Press(ConsoleKey.UpArrow);

    public KeyScript Down(int times = 1)
    {
        for (int i = 0; i < times; i++)
        {
            Press(ConsoleKey.DownArrow);
        }

        return this;
    }

    public KeyScript Home() => Press(ConsoleKey.Home);

    public KeyScript End() => Press(ConsoleKey.End);

    public KeyScript PageUp() => Press(ConsoleKey.PageUp);

    public KeyScript PageDown() => Press(ConsoleKey.PageDown);

    public KeyScript Space() => Press(ConsoleKey.Spacebar, ' ');

    public KeyScript Enter() => Press(ConsoleKey.Enter, '\r');

    public KeyScript Backspace() => Press(ConsoleKey.Backspace, '\b');

    public KeyScript Type(string text)
    {
        foreach (char c in text)
        {
            Press(c == ' ' ? ConsoleKey.Spacebar : (ConsoleKey)0, c);
        }

        return this;
    }

    public ConsoleKeyInfo Read()
    {
        if (_keys.Count == 0)
        {
            throw new InvalidOperationException("The key script is exhausted.");
        }

        return _keys.Dequeue();
    }
}
