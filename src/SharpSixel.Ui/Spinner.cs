using System;
using System.Text;

namespace SharpSixel.Ui;

/// <summary>Spinner with optional caption. Call <see cref="Advance"/> per animation step. Drawn as sixel ring or ASCII frames.</summary>
public sealed class Spinner : Widget
{
    private const int TransparentAlphaLimit = 128;
    private int _frame;

    /// <summary>Creates a spinner with the default style.</summary>
    public Spinner()
        : this(new SpinnerStyle())
    {
    }

    /// <summary>Creates a spinner with the given style.</summary>
    public Spinner(SpinnerStyle style)
    {
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    /// <summary>Look of the spinner; may be changed between frames.</summary>
    public SpinnerStyle Style { get; }

    /// <summary>Optional caption shown after the spinner.</summary>
    public string? Caption { get; set; }

    /// <summary>Current animation step (starts at 0).</summary>
    public int Frame => _frame;

    /// <summary>Moves to the next animation step.</summary>
    public void Advance() => _frame = _frame == int.MaxValue ? 0 : _frame + 1;

    /// <inheritdoc />
    protected override int SixelRows => Style.HeightRows;

    /// <inheritdoc />
    protected override string RenderAscii(WidgetEnvironment environment)
    {
        var sb = new StringBuilder();
        sb.Append(AnsiCodes.Colorize(Style.AsciiFrameAt(_frame), Style.ActiveColor, environment.UseAnsi));
        sb.Append(TrailingText());
        if (environment.UseAnsi)
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
            ImageColumns(environment),
            string.Empty,
            TrailingText(),
            Style.BackgroundColor.A < TransparentAlphaLimit);

    internal int ImageColumns(WidgetEnvironment environment) =>
        (int)Math.Ceiling((double)environment.GetImageHeight(Style.HeightRows) / environment.CellWidth);

    /// <inheritdoc />
    protected override int MeasureColumns(WidgetEnvironment environment) =>
        environment.UseSixel ? ImageColumns(environment) + TextFit.Width(TrailingText()) : base.MeasureColumns(environment);

    internal SixelImage CreateImage(WidgetEnvironment environment)
    {
        int size = environment.GetImageHeight(Style.HeightRows);
        int width = ImageColumns(environment) * environment.CellWidth;
        var image = new SixelImage(width, size);
        if (Style.BackgroundColor.A > 0)
        {
            Raster.Fill(image, Style.BackgroundColor);
        }

        int count = Style.SegmentCount;
        int trail = Math.Min(Style.TrailLength, count);
        int dotRadius = Math.Max(1, (int)Math.Round(size / 2.0 * Style.DotScale, MidpointRounding.AwayFromZero));
        double ringRadius = Math.Max(0, (size / 2.0) - dotRadius - 1);
        double centerX = width / 2.0;
        double centerY = size / 2.0;
        int head = _frame % count;
        for (int i = 0; i < count; i++)
        {
            int age = ((head - i) % count + count) % count;
            double weight = age < trail ? 1.0 - ((double)age / trail) : 0.0;
            double angle = (-Math.PI / 2) + (2 * Math.PI * i / count);
            int dotX = (int)Math.Round(centerX + (ringRadius * Math.Cos(angle)), MidpointRounding.AwayFromZero);
            int dotY = (int)Math.Round(centerY + (ringRadius * Math.Sin(angle)), MidpointRounding.AwayFromZero);
            Raster.FillRoundedRect(
                image,
                dotX - dotRadius,
                dotY - dotRadius,
                2 * dotRadius,
                2 * dotRadius,
                dotRadius,
                Blend(Style.InactiveColor, Style.ActiveColor, weight));
        }

        return image;
    }

    private static SixelColor Blend(SixelColor from, SixelColor to, double weight) =>
        new SixelColor(
            Mix(from.R, to.R, weight),
            Mix(from.G, to.G, weight),
            Mix(from.B, to.B, weight),
            Mix(from.A, to.A, weight));

    private static byte Mix(byte from, byte to, double weight) =>
        (byte)Math.Round(from + ((to - from) * weight), MidpointRounding.AwayFromZero);

    private string TrailingText() => string.IsNullOrEmpty(Caption) ? string.Empty : " " + Caption;
}
