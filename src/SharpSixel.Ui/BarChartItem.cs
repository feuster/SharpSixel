using System;

namespace SharpSixel.Ui;

/// <summary>One bar of a <see cref="BarChart"/>: label, value and optional color.</summary>
public sealed class BarChartItem
{
    private string _label;

    /// <summary>Creates an item.</summary>
    /// <param name="label">Text shown before the bar.</param>
    /// <param name="value">Bar value (negative and NaN values are drawn as empty bars).</param>
    /// <param name="color">Bar color; null picks a color from <see cref="BarChartStyle.Palette"/> by position.</param>
    public BarChartItem(string label, double value, SixelColor? color = null)
    {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        Value = value;
        Color = color;
    }

    /// <summary>Text shown before the bar.</summary>
    public string Label
    {
        get => _label;
        set => _label = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Bar value; may be changed between frames.</summary>
    public double Value { get; set; }

    /// <summary>Bar color; null picks a color from <see cref="BarChartStyle.Palette"/> by position.</summary>
    public SixelColor? Color { get; set; }
}
