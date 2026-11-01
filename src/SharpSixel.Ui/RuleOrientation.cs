namespace SharpSixel.Ui;

/// <summary>Direction of a <see cref="Rule"/>.</summary>
public enum RuleOrientation
{
    /// <summary>Line across one row, optionally with a centered title.</summary>
    Horizontal,

    /// <summary>Line down several rows (a separator between widgets in a <see cref="HorizontalLayout"/>).</summary>
    Vertical,
}
