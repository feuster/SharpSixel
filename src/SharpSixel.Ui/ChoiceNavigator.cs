using System;

namespace SharpSixel.Ui;

/// <summary>Cursor and scroll window of a choice list (selection and multi-selection prompts).</summary>
internal sealed class ChoiceNavigator
{
    public int Count { get; private set; }

    public int PageSize { get; private set; }

    public int Cursor { get; private set; }

    /// <summary>Index of the first visible item.</summary>
    public int Top { get; private set; }

    /// <summary>Number of visible items.</summary>
    public int Visible => Math.Min(PageSize, Count);

    public void Reset(int count, int pageSize)
    {
        Count = count;
        PageSize = pageSize;
        Cursor = 0;
        Top = 0;
    }

    /// <summary>Moves the cursor to a visible item (mouse click).</summary>
    public void MoveTo(int index) => Cursor = Math.Max(0, Math.Min(Count - 1, index));

    /// <summary>Handles a navigation key (Up/Down wrap around, Home, End, PageUp, PageDown); false if the key is not one.</summary>
    public bool Handle(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                Cursor = (Cursor + Count - 1) % Count;
                break;
            case ConsoleKey.DownArrow:
                Cursor = (Cursor + 1) % Count;
                break;
            case ConsoleKey.Home:
                Cursor = 0;
                break;
            case ConsoleKey.End:
                Cursor = Count - 1;
                break;
            case ConsoleKey.PageUp:
                Cursor = Math.Max(0, Cursor - Visible);
                break;
            case ConsoleKey.PageDown:
                Cursor = Math.Min(Count - 1, Cursor + Visible);
                break;
            default:
                return false;
        }

        if (Cursor < Top)
        {
            Top = Cursor;
        }
        else if (Cursor >= Top + Visible)
        {
            Top = Cursor - Visible + 1;
        }

        return true;
    }
}
