using System;
using System.Globalization;
using System.Text;

namespace SharpSixel.Ui;

/// <summary>Progress bar with optional caption and progress text. Drawn as sixel graphic or as ASCII.</summary>
public sealed class ProgressBar : Widget
{
    private const int TransparentAlphaLimit = 128;
    private const int MinBarWidth = 3;
    private double _maximum = 100;
    private int _frame;

    /// <summary>Creates a progress bar with the default style.</summary>
    public ProgressBar()
        : this(new ProgressBarStyle())
    {
    }

    /// <summary>Creates a progress bar with the given style.</summary>
    public ProgressBar(ProgressBarStyle style)
    {
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    /// <summary>Look of the bar; may be changed between frames.</summary>
    public ProgressBarStyle Style { get; }

    /// <summary>Current value (values outside 0..<see cref="Maximum"/> are clamped when drawing).</summary>
    public double Value { get; set; }

    /// <summary>Value that represents 100 %. Must be greater than 0. Default 100.</summary>
    public double Maximum
    {
        get => _maximum;
        set
        {
            if (!(value > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Maximum must be greater than 0.");
            }

            _maximum = value;
        }
    }

    /// <summary>Optional caption shown before the bar.</summary>
    public string? Caption { get; set; }

    /// <summary>Text shown after the bar. Default percent.</summary>
    public ProgressTextMode ProgressText { get; set; } = ProgressTextMode.Percent;

    /// <summary>
    /// True if the progress is not known: the bar shows a segment that moves back and forth (call <see cref="Advance"/> per
    /// animation step) and no progress text; <see cref="Value"/> is ignored.
    /// </summary>
    public bool IsIndeterminate { get; set; }

    /// <summary>Current animation step of an indeterminate bar (starts at 0).</summary>
    public int Frame => _frame;

    /// <summary>Moves an indeterminate bar to the next animation step.</summary>
    public void Advance() => _frame = _frame == int.MaxValue ? 0 : _frame + 1;

    /// <summary>Cells the caption is padded to (set by a <see cref="ProgressGroup"/> to align its bars), 0 for the natural width.</summary>
    internal int CaptionPad { get; set; }

    /// <summary>Progress as fraction between 0 and 1.</summary>
    public double Fraction
    {
        get
        {
            double fraction = Value / Maximum;
            if (double.IsNaN(fraction) || fraction < 0)
            {
                return 0;
            }

            return fraction > 1 ? 1 : fraction;
        }
    }

    /// <inheritdoc />
    protected override int SixelRows => Style.HeightRows;

    /// <inheritdoc />
    protected override string RenderAscii(WidgetEnvironment environment)
    {
        var layout = Layout(environment);
        int inner = layout.Bar - 2;
        int skipped = 0;
        int filled = (int)Math.Round(Fraction * inner, MidpointRounding.AwayFromZero);
        if (IsIndeterminate)
        {
            filled = Math.Min(inner, Math.Max(1, (int)Math.Round(inner * Style.IndeterminateSegment, MidpointRounding.AwayFromZero)));
            skipped = (int)Math.Round(MarqueePhase() * (inner - filled), MidpointRounding.AwayFromZero);
        }

        bool ansi = environment.UseAnsi;
        var sb = new StringBuilder();
        sb.Append(layout.Prefix);
        sb.Append(AnsiCodes.Colorize(Style.AsciiBorderLeft.ToString(), Style.BorderColor, ansi));
        sb.Append(AnsiCodes.Colorize(new string(Style.AsciiEmpty, skipped), Style.TrackColor, ansi));
        sb.Append(AnsiCodes.Colorize(new string(Style.AsciiFill, filled), Style.FillColor, ansi));
        sb.Append(AnsiCodes.Colorize(new string(Style.AsciiEmpty, inner - filled - skipped), Style.TrackColor, ansi));
        sb.Append(AnsiCodes.Colorize(Style.AsciiBorderRight.ToString(), Style.BorderColor, ansi));
        sb.Append(layout.Trailing);
        if (ansi)
        {
            sb.Append(AnsiCodes.EraseLineRest);
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    protected override string RenderSixel(WidgetEnvironment environment)
    {
        var layout = Layout(environment);
        return SixelFrame.Build(
            CreateImage(environment),
            Style.HeightRows,
            layout.Bar,
            layout.Prefix,
            layout.Trailing,
            Style.BackgroundColor.A < TransparentAlphaLimit);
    }

    internal SixelImage CreateImage(WidgetEnvironment environment)
    {
        int width = BarWidth(environment) * environment.CellWidth;
        int height = environment.GetImageHeight(Style.HeightRows);
        var image = new SixelImage(width, height);
        if (Style.BackgroundColor.A > 0)
        {
            Raster.Fill(image, Style.BackgroundColor);
        }

        int border = Style.BorderThickness;
        int radius = Style.CornerRadius;
        int thickness = Math.Min(height, Math.Max((2 * border) + 1, (int)Math.Round(height * Style.BarThickness)));
        int top = Math.Min((Style.HeightRows * environment.CellHeight - thickness) / 2, height - thickness);
        if (border > 0)
        {
            Raster.FillRoundedRect(image, 0, top, width, thickness, radius, Style.BorderColor);
        }

        int innerX = border;
        int innerY = top + border;
        int innerWidth = width - (2 * border);
        int innerHeight = thickness - (2 * border);
        int innerRadius = Math.Max(0, radius - border);
        Raster.FillRoundedRect(image, innerX, innerY, innerWidth, innerHeight, innerRadius, Style.TrackColor);
        if (IsIndeterminate)
        {
            int segment = Math.Min(innerWidth, Math.Max(1, (int)Math.Round(innerWidth * Style.IndeterminateSegment, MidpointRounding.AwayFromZero)));
            int start = (int)Math.Round(MarqueePhase() * (innerWidth - segment), MidpointRounding.AwayFromZero);
            Raster.FillRoundedRect(image, innerX + start, innerY, segment, innerHeight, innerRadius, Style.FillColor);
            return image;
        }

        int fillEnd = innerX + (int)Math.Round(innerWidth * Fraction, MidpointRounding.AwayFromZero);
        Raster.FillRoundedRect(image, innerX, innerY, innerWidth, innerHeight, innerRadius, Style.FillColor, fillEnd);
        return image;
    }

    /// <summary>Bar width in cells (see <see cref="Layout"/>).</summary>
    private int BarWidth(WidgetEnvironment environment) => Layout(environment).Bar;

    /// <summary>
    /// Splits the terminal width into caption, bar and text. The bar is reduced to <see cref="ProgressBarStyle.Width"/> at most and
    /// to <see cref="MinBarWidth"/> at least; if caption and text still do not fit, the text and then the caption are cut
    /// (with an ellipsis) so the line never exceeds the terminal width and cannot wrap into the following lines.
    /// </summary>
    private (string Prefix, string Trailing, int Bar) Layout(WidgetEnvironment environment)
    {
        string prefix = CaptionPrefix();
        string trailing = TrailingText();
        if (environment.Columns <= 0)
        {
            return (prefix, trailing, Style.Width);
        }

        int available = Math.Max(0, environment.Columns - MinBarWidth);
        if (TextFit.Width(prefix) + TextFit.Width(trailing) > available)
        {
            trailing = TextFit.Clip(trailing, available);
            int room = available - TextFit.Width(trailing);
            prefix = room >= 2 && Caption is not null ? TextFit.Clip(Caption, room - 1) + " " : string.Empty;
        }

        int bar = Math.Min(Style.Width, Math.Max(MinBarWidth, environment.Columns - TextFit.Width(prefix) - TextFit.Width(trailing)));
        return (prefix, trailing, bar);
    }

    private string CaptionPrefix() =>
        string.IsNullOrEmpty(Caption) && CaptionPad == 0 ? string.Empty : TextFit.PadRight(Caption ?? string.Empty, CaptionPad) + " ";

    /// <summary>Position of the moving segment of an indeterminate bar: 0 (left end) to 1 (right end), back and forth.</summary>
    private double MarqueePhase()
    {
        int steps = Style.IndeterminateSteps;
        int position = _frame % (2 * steps);
        return position <= steps ? (double)position / steps : (double)((2 * steps) - position) / steps;
    }

    private string TrailingText()
    {
        if (IsIndeterminate)
        {
            return string.Empty;
        }

        switch (ProgressText)
        {
            case ProgressTextMode.Percent:
                int percent = (int)Math.Round(Fraction * 100, MidpointRounding.AwayFromZero);
                return " " + percent.ToString(CultureInfo.InvariantCulture).PadLeft(3) + "%";
            case ProgressTextMode.Fraction:
                return " " + string.Format(CultureInfo.InvariantCulture, "{0:0.##}/{1:0.##}", Value, Maximum);
            default:
                return string.Empty;
        }
    }
}
