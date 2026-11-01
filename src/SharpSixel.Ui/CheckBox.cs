using System;

namespace SharpSixel.Ui;

/// <summary>
/// Check box with a label. Sixel: rounded box (with a check mark if <see cref="Checked"/>) as graphic, label as terminal text,
/// a bar behind the focused box. ASCII: <c>[x] Label</c> / <c>[ ] Label</c>, a pointer in front of the focused one.
/// The widget only draws; key handling and clicks belong to the caller, who changes <see cref="Checked"/> and
/// <see cref="Focused"/> between frames. The label should fit on one terminal row.
/// </summary>
public sealed class CheckBox : Widget
{
    private string _label;

    /// <summary>Creates a check box with the default style.</summary>
    /// <param name="label">Text next to the box.</param>
    public CheckBox(string label)
        : this(label, new ToggleStyle())
    {
    }

    /// <summary>Creates a check box with the given style.</summary>
    /// <param name="label">Text next to the box.</param>
    /// <param name="style">Look of the check box.</param>
    public CheckBox(string label, ToggleStyle style)
    {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    /// <summary>Look of the check box; may be changed between frames.</summary>
    public ToggleStyle Style { get; }

    /// <summary>Text next to the box; control characters are drawn as spaces.</summary>
    public string Label
    {
        get => _label;
        set => _label = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>True if the box is checked.</summary>
    public bool Checked { get; set; }

    /// <summary>True if the check box has the focus (highlighted).</summary>
    public bool Focused { get; set; }

    /// <summary>False draws the check box in the disabled color and without focus. Default true.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// True if a position (column and row relative to the origin of the widget, for example from
    /// <see cref="LiveRegion.TryGetLocal"/>) lies on the check box. Use it to turn mouse events into clicks.
    /// </summary>
    public bool Contains(WidgetEnvironment environment, int column, int row) =>
        row == 0 && column >= 0 && column < GetColumns(environment);

    /// <inheritdoc />
    protected override int SixelRows => 1;

    /// <inheritdoc />
    protected override int MeasureColumns(WidgetEnvironment environment) => CreateView().GetColumns(environment);

    /// <inheritdoc />
    protected override string RenderAscii(WidgetEnvironment environment) => CreateView().Render(environment);

    /// <inheritdoc />
    protected override string RenderSixel(WidgetEnvironment environment) => CreateView().Render(environment);

    internal PromptView CreateView()
    {
        var view = new PromptView(Style.ToPromptStyle());
        view.Rows.Add(Style.CreateRow(_label, MarkerShape.Check, Checked, Focused, Enabled));
        return view;
    }
}
