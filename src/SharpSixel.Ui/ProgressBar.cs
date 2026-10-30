using System;
using System.Globalization;
using System.Text;

namespace SharpSixel.Ui;

/// <summary>Progress bar with optional caption and progress text. Drawn as sixel graphic or as ASCII.</summary>
public sealed class ProgressBar : Widget
{
    private const int TransparentAlphaLimit = 128;
    private double _maximum = 100;

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
        int inner = Style.Width - 2;
        int filled = (int)Math.Round(Fraction * inner, MidpointRounding.AwayFromZero);
        bool ansi = environment.UseAnsi;
        var sb = new StringBuilder();
        sb.Append(CaptionPrefix());
        sb.Append(AnsiCodes.Colorize(Style.AsciiBorderLeft.ToString(), Style.BorderColor, ansi));
        sb.Append(AnsiCodes.Colorize(new string(Style.AsciiFill, filled), Style.FillColor, ansi));
        sb.Append(AnsiCodes.Colorize(new string(Style.AsciiEmpty, inner - filled), Style.TrackColor, ansi));
        sb.Append(AnsiCodes.Colorize(Style.AsciiBorderRight.ToString(), Style.BorderColor, ansi));
        sb.Append(TrailingText());
        if (ansi)
        {
            sb.Append(AnsiCodes.EraseLineRest);
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    protected override string RenderSixel(WidgetEnvironment environment) =>
        SixelFrame.Build(
            CreateImage(environment),
            Style.HeightRows,
            Style.Width,
            CaptionPrefix(),
            TrailingText(),
            Style.BackgroundColor.A < TransparentAlphaLimit);

    internal SixelImage CreateImage(WidgetEnvironment environment)
    {
        int width = Style.Width * environment.CellWidth;
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
        int fillEnd = innerX + (int)Math.Round(innerWidth * Fraction, MidpointRounding.AwayFromZero);
        Raster.FillRoundedRect(image, innerX, innerY, innerWidth, innerHeight, innerRadius, Style.FillColor, fillEnd);
        return image;
    }

    private string CaptionPrefix() => string.IsNullOrEmpty(Caption) ? string.Empty : Caption + " ";

    private string TrailingText()
    {
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
