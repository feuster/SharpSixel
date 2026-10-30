namespace SharpSixel.Ui;

/// <summary>Text shown next to a progress bar.</summary>
public enum ProgressTextMode
{
    /// <summary>No progress text.</summary>
    None,

    /// <summary>Percentage, e.g. <c>50%</c>.</summary>
    Percent,

    /// <summary>Value and maximum, e.g. <c>5/10</c>.</summary>
    Fraction,
}
