using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace SharpSixel.Ui;

/// <summary>
/// Horizontal bar chart: one bar per item with label before and value after the bar, optional caption on top.
/// Drawn as sixel graphic or as ASCII. The number of items must not grow beyond the rows reserved in a
/// <see cref="LiveRegion"/> (<see cref="Widget.GetRows"/> at <see cref="LiveRegion.Begin"/>) and should stay constant while shown.
/// </summary>
public sealed class BarChart : Widget
{
    private const int TransparentAlphaLimit = 128;
    private const string DefaultValueFormat = "0.##";
    private readonly ItemCollection _items = new ItemCollection();
    private double? _maximum;
    private string _valueFormat = DefaultValueFormat;

    /// <summary>Creates a bar chart with the default style.</summary>
    public BarChart()
        : this(new BarChartStyle())
    {
    }

    /// <summary>Creates a bar chart with the given style.</summary>
    public BarChart(BarChartStyle style)
    {
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    /// <summary>Look of the chart; may be changed between frames.</summary>
    public BarChartStyle Style { get; }

    /// <summary>Optional caption shown on a row above the bars.</summary>
    public string? Caption { get; set; }

    /// <summary>The bars (null items are rejected).</summary>
    public IList<BarChartItem> Items => _items;

    /// <summary>Value that fills the whole bar area. Null (default) uses the largest item value. Must be greater than 0.</summary>
    public double? Maximum
    {
        get => _maximum;
        set
        {
            if (value.HasValue && !(value.Value > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Maximum must be greater than 0.");
            }

            _maximum = value;
        }
    }

    /// <summary>True (default) to show the value after each bar.</summary>
    public bool ShowValues { get; set; } = true;

    /// <summary>Numeric format string of the shown values (invariant culture). Default <c>0.##</c>.</summary>
    public string ValueFormat
    {
        get => _valueFormat;
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            try
            {
                0d.ToString(value, CultureInfo.InvariantCulture);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid number format.", nameof(value), ex);
            }

            _valueFormat = value;
        }
    }

    /// <summary>Adds a bar and returns it (change its <see cref="BarChartItem.Value"/> later to update the chart).</summary>
    public BarChartItem Add(string label, double value, SixelColor? color = null)
    {
        var item = new BarChartItem(label, value, color);
        _items.Add(item);
        return item;
    }

    private int CaptionRows => string.IsNullOrEmpty(Caption) ? 0 : 1;

    /// <inheritdoc />
    protected override int SixelRows => Math.Max(1, CaptionRows + (_items.Count * Style.BarHeightRows));

    /// <inheritdoc />
    protected override int AsciiRows => Math.Max(1, CaptionRows + _items.Count);

    /// <inheritdoc />
    protected override string RenderAscii(WidgetEnvironment environment)
    {
        int barWidth = BarWidth(environment);
        int labelWidth = LabelWidth();
        string[] values = ValueTexts();
        int valueWidth = MaxLength(values);
        double maximum = EffectiveMaximum();
        var lines = new List<string>();
        if (CaptionRows > 0)
        {
            lines.Add(Caption!);
        }

        for (int i = 0; i < _items.Count; i++)
        {
            int filled = (int)Math.Round(Fraction(_items[i], maximum) * barWidth, MidpointRounding.AwayFromZero);
            var sb = new StringBuilder(_items[i].Label.PadRight(labelWidth));
            if (labelWidth > 0)
            {
                sb.Append(' ');
            }

            sb.Append(AnsiCodes.Colorize(new string(Style.AsciiFill, filled), ColorOf(i), environment.UseAnsi));
            sb.Append(new string(Style.AsciiEmpty, barWidth - filled));
            if (ShowValues)
            {
                sb.Append(' ').Append(values[i].PadLeft(valueWidth));
            }

            lines.Add(sb.ToString());
        }

        return string.Join("\n", lines);
    }

    /// <inheritdoc />
    protected override string RenderSixel(WidgetEnvironment environment)
    {
        int count = _items.Count;
        int captionRows = CaptionRows;
        int barRows = Style.BarHeightRows;
        int barWidth = BarWidth(environment);
        int labelWidth = LabelWidth();
        int imageColumn = labelWidth > 0 ? labelWidth + 1 : 0;
        var sb = new StringBuilder(AnsiCodes.SaveCursor);
        if (captionRows > 0)
        {
            sb.Append(Caption).Append(AnsiCodes.EraseLineRest).Append(AnsiCodes.RestoreCursor);
        }

        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                if (_items[i].Label.Length > 0)
                {
                    sb.Append(AnsiCodes.Down(TextRow(i))).Append(_items[i].Label).Append(AnsiCodes.RestoreCursor);
                }
            }

            if (Style.BackgroundColor.A < TransparentAlphaLimit)
            {
                for (int row = 0; row < count * barRows; row++)
                {
                    sb.Append(AnsiCodes.Down(captionRows + row)).Append(AnsiCodes.Forward(imageColumn))
                        .Append(AnsiCodes.EraseChars(barWidth)).Append(AnsiCodes.RestoreCursor);
                }
            }

            sb.Append(AnsiCodes.Down(captionRows)).Append(AnsiCodes.Forward(imageColumn))
                .Append(new SixelEncoder().Encode(CreateImage(environment))).Append(AnsiCodes.RestoreCursor);

            if (ShowValues)
            {
                string[] values = ValueTexts();
                int valueWidth = MaxLength(values);
                for (int i = 0; i < count; i++)
                {
                    sb.Append(AnsiCodes.Down(TextRow(i))).Append(AnsiCodes.Forward(imageColumn + barWidth))
                        .Append(' ').Append(values[i].PadLeft(valueWidth)).Append(AnsiCodes.EraseLineRest)
                        .Append(AnsiCodes.RestoreCursor);
                }
            }
        }

        return sb.Append(AnsiCodes.RestoreCursor).ToString();
    }

    internal SixelImage CreateImage(WidgetEnvironment environment)
    {
        int count = _items.Count;
        int barWidth = BarWidth(environment);
        int width = barWidth * environment.CellWidth;
        int height = environment.GetImageHeight(count * Style.BarHeightRows);
        var image = new SixelImage(width, height);
        if (Style.BackgroundColor.A > 0)
        {
            Raster.Fill(image, Style.BackgroundColor);
        }

        double maximum = EffectiveMaximum();
        double band = (double)height / Math.Max(1, count);
        int cellBand = Style.BarHeightRows * environment.CellHeight;   // bars follow the text rows, not the shorter image
        int thickness = Math.Min(
            Math.Max(1, (int)Math.Floor(band)),
            Math.Max(1, (int)Math.Round(band * Style.BarThickness, MidpointRounding.AwayFromZero)));
        for (int i = 0; i < count; i++)
        {
            int top = (i * cellBand) + ((cellBand - thickness) / 2);
            top = Math.Max(0, Math.Min(top, height - thickness));
            if (Style.TrackColor.A > 0)
            {
                Raster.FillRoundedRect(image, 0, top, width, thickness, Style.CornerRadius, Style.TrackColor);
            }

            int fillWidth = (int)Math.Round(Fraction(_items[i], maximum) * width, MidpointRounding.AwayFromZero);
            Raster.FillRoundedRect(image, 0, top, fillWidth, thickness, Style.CornerRadius, ColorOf(i));
        }

        return image;
    }

    private int TextRow(int index) => CaptionRows + (index * Style.BarHeightRows) + ((Style.BarHeightRows - 1) / 2);

    private SixelColor ColorOf(int index) => _items[index].Color ?? Style.PaletteColorAt(index);

    /// <summary>Bar width in cells: <see cref="BarChartStyle.Width"/>, reduced so label, bar and value fit the terminal width.</summary>
    private int BarWidth(WidgetEnvironment environment)
    {
        if (environment.Columns <= 0)
        {
            return Style.Width;
        }

        int labelWidth = LabelWidth();
        int used = labelWidth > 0 ? labelWidth + 1 : 0;
        if (ShowValues)
        {
            used += 1 + MaxLength(ValueTexts());
        }

        return Math.Max(1, Math.Min(Style.Width, environment.Columns - used));
    }

    private int LabelWidth()
    {
        int width = 0;
        foreach (BarChartItem item in _items)
        {
            width = Math.Max(width, item.Label.Length);
        }

        return width;
    }

    private string[] ValueTexts()
    {
        var texts = new string[_items.Count];
        for (int i = 0; i < texts.Length; i++)
        {
            texts[i] = _items[i].Value.ToString(_valueFormat, CultureInfo.InvariantCulture);
        }

        return texts;
    }

    private static int MaxLength(string[] texts)
    {
        int length = 0;
        foreach (string text in texts)
        {
            length = Math.Max(length, text.Length);
        }

        return length;
    }

    private double EffectiveMaximum()
    {
        if (_maximum.HasValue)
        {
            return _maximum.Value;
        }

        double max = 0;
        foreach (BarChartItem item in _items)
        {
            if (item.Value > max && !double.IsInfinity(item.Value))
            {
                max = item.Value;
            }
        }

        return max > 0 ? max : 1;
    }

    private static double Fraction(BarChartItem item, double maximum)
    {
        double fraction = item.Value / maximum;
        if (double.IsNaN(fraction) || fraction < 0)
        {
            return 0;
        }

        return fraction > 1 ? 1 : fraction;
    }

    private sealed class ItemCollection : Collection<BarChartItem>
    {
        protected override void InsertItem(int index, BarChartItem item) => base.InsertItem(index, Check(item));

        protected override void SetItem(int index, BarChartItem item) => base.SetItem(index, Check(item));

        private static BarChartItem Check(BarChartItem item) => item ?? throw new ArgumentNullException(nameof(item));
    }
}
