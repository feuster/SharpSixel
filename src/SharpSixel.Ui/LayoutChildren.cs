using System;
using System.Collections.ObjectModel;

namespace SharpSixel.Ui;

/// <summary>Children of a layout: no null entries, not the layout itself; reports removed children.</summary>
internal sealed class LayoutChildren : Collection<Widget>
{
    private readonly Widget _owner;
    private readonly Action<Widget>? _removed;

    public LayoutChildren(Widget owner, Action<Widget>? removed = null)
    {
        _owner = owner;
        _removed = removed;
    }

    protected override void InsertItem(int index, Widget item) => base.InsertItem(index, Check(item));

    protected override void SetItem(int index, Widget item)
    {
        Widget old = this[index];
        base.SetItem(index, Check(item));
        _removed?.Invoke(old);
    }

    protected override void RemoveItem(int index)
    {
        Widget old = this[index];
        base.RemoveItem(index);
        _removed?.Invoke(old);
    }

    protected override void ClearItems()
    {
        Widget[] old = new Widget[Count];
        CopyTo(old, 0);
        base.ClearItems();
        foreach (Widget widget in old)
        {
            _removed?.Invoke(widget);
        }
    }

    private Widget Check(Widget item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (ReferenceEquals(item, _owner))
        {
            throw new ArgumentException("A layout cannot contain itself.", nameof(item));
        }

        return item;
    }
}