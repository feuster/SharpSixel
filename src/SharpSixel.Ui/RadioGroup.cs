using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SharpSixel.Ui;

/// <summary>
/// Group of radio options, one terminal row each, exactly one (or none) of them selected. Sixel: ring with a dot for the selected
/// option as graphic, labels as terminal text, a bar behind the focused option. ASCII: <c>(*) Label</c> / <c>( ) Label</c>, a
/// pointer in front of the focused one. The widget only draws; key handling and clicks belong to the caller, who changes
/// <see cref="SelectedIndex"/> and <see cref="FocusedIndex"/> between frames. The number of options must not grow beyond the
/// rows reserved in a <see cref="LiveRegion"/>.
/// </summary>
public sealed class RadioGroup : Widget
{
    private readonly Collection<string> _options = new OptionCollection();
    private int _selectedIndex = -1;
    private int _focusedIndex = -1;

    /// <summary>Creates a radio group with the default style.</summary>
    public RadioGroup()
        : this(new ToggleStyle())
    {
    }

    /// <summary>Creates a radio group with the given style.</summary>
    public RadioGroup(ToggleStyle style)
    {
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    /// <summary>Look of the group; may be changed between frames.</summary>
    public ToggleStyle Style { get; }

    /// <summary>The option labels (null entries are rejected; control characters are drawn as spaces).</summary>
    public IList<string> Options => _options;

    /// <summary>Adds an option.</summary>
    public void AddOption(string label) => _options.Add(label);

    /// <summary>Index of the selected option, -1 (default) for none. An index beyond the options selects nothing.</summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set => _selectedIndex = Guard.AtLeast(value, -1, nameof(value));
    }

    /// <summary>Index of the focused option, -1 (default) for none. An index beyond the options focuses nothing.</summary>
    public int FocusedIndex
    {
        get => _focusedIndex;
        set => _focusedIndex = Guard.AtLeast(value, -1, nameof(value));
    }

    /// <summary>False draws the group in the disabled color and without focus. Default true.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Index of the option at a position (column and row relative to the origin of the widget, for example from
    /// <see cref="LiveRegion.TryGetLocal"/>), or -1 if there is none. Use it to turn mouse events into clicks.
    /// </summary>
    public int IndexAt(WidgetEnvironment environment, int column, int row) =>
        row >= 0 && row < _options.Count && column >= 0 && column < GetColumns(environment) ? row : -1;

    private int Rows => Math.Max(1, _options.Count);

    /// <inheritdoc />
    protected override int SixelRows => Rows;

    /// <inheritdoc />
    protected override int AsciiRows => Rows;

    /// <inheritdoc />
    protected override int MeasureColumns(WidgetEnvironment environment) => CreateView().GetColumns(environment);

    /// <inheritdoc />
    protected override string RenderAscii(WidgetEnvironment environment) => CreateView().Render(environment);

    /// <inheritdoc />
    protected override string RenderSixel(WidgetEnvironment environment) => CreateView().Render(environment);

    internal PromptView CreateView()
    {
        var view = new PromptView(Style.ToPromptStyle());
        for (int i = 0; i < _options.Count; i++)
        {
            view.Rows.Add(Style.CreateRow(_options[i], MarkerShape.Radio, i == _selectedIndex, i == _focusedIndex, Enabled));
        }

        return view;
    }

    private sealed class OptionCollection : Collection<string>
    {
        protected override void InsertItem(int index, string item) => base.InsertItem(index, Check(item));

        protected override void SetItem(int index, string item) => base.SetItem(index, Check(item));

        private static string Check(string item) => item ?? throw new ArgumentNullException(nameof(item));
    }
}
