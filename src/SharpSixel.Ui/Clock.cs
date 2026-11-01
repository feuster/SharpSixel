using System;
using System.Globalization;
using System.Text;

namespace SharpSixel.Ui;

/// <summary>
/// Clock showing <see cref="Time"/>. Sixel: analog clock face with hour marks and hands, followed by the digital time as
/// terminal text. ASCII: the digital time only. The widget does not tick by itself: set <see cref="Time"/> (for example
/// to <see cref="DateTime.Now"/>) before each frame.
/// </summary>
public sealed class Clock : Widget
{
    private const int ColorAlphaLimit = 128;
    private const int TickCount = 12;

    /// <summary>Creates a clock with the default style.</summary>
    public Clock()
        : this(new ClockStyle())
    {
    }

    /// <summary>Creates a clock with the given style.</summary>
    public Clock(ClockStyle style)
    {
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    /// <summary>Look of the clock; may be changed between frames.</summary>
    public ClockStyle Style { get; }

    /// <summary>Time to show (only the time of day is used). Default 00:00:00.</summary>
    public DateTime Time { get; set; }

    /// <summary>Optional caption shown before the clock.</summary>
    public string? Caption { get; set; }

    /// <inheritdoc />
    protected override int SixelRows => Style.HeightRows;

    /// <inheritdoc />
    protected override string RenderAscii(WidgetEnvironment environment)
    {
        var sb = new StringBuilder();
        sb.Append(CaptionPrefix());
        sb.Append(TimeText(environment.UseAnsi));
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
            CaptionPrefix(),
            " " + TimeText(true),
            Style.BackgroundColor.A < ColorAlphaLimit);

    internal int ImageColumns(WidgetEnvironment environment) =>
        (int)Math.Ceiling((double)environment.GetImageHeight(Style.HeightRows) / environment.CellWidth);

    /// <inheritdoc />
    protected override int MeasureColumns(WidgetEnvironment environment) =>
        environment.UseSixel
            ? CaptionPrefix().Length + ImageColumns(environment) + 1 + TimeText(false).Length
            : base.MeasureColumns(environment);

    internal SixelImage CreateImage(WidgetEnvironment environment)
    {
        int size = environment.GetImageHeight(Style.HeightRows);
        int width = ImageColumns(environment) * environment.CellWidth;
        var image = new SixelImage(width, size);
        if (Style.BackgroundColor.A > 0)
        {
            Raster.Fill(image, Style.BackgroundColor);
        }

        int rim = Style.LineThickness;
        int left = (width - size) / 2;
        Raster.FillRoundedRect(image, left, 0, size, size, size / 2, Style.BorderColor);
        Raster.FillRoundedRect(image, left + rim, rim, size - (2 * rim), size - (2 * rim), (size / 2) - rim, Style.FaceColor);

        double centerX = width / 2.0;
        double centerY = size / 2.0;
        double reach = (size / 2.0) - rim;
        double tickThickness = Math.Max(1, size * 0.03);
        for (int i = 0; i < TickCount; i++)
        {
            double from = i % 3 == 0 ? 0.78 : 0.88;
            DrawRay(image, centerX, centerY, 2 * Math.PI * i / TickCount, reach * from, reach * 0.96, tickThickness, Style.TickColor);
        }

        double hourAngle = 2 * Math.PI * (((Time.Hour % 12) + (Time.Minute / 60.0)) / 12.0);
        double minuteAngle = 2 * Math.PI * ((Time.Minute + (Time.Second / 60.0)) / 60.0);
        DrawRay(image, centerX, centerY, hourAngle, 0, reach * 0.5, Math.Max(2, size * 0.07), Style.HandColor);
        DrawRay(image, centerX, centerY, minuteAngle, 0, reach * 0.75, Math.Max(2, size * 0.05), Style.HandColor);
        if (Style.ShowSecondHand)
        {
            DrawRay(image, centerX, centerY, 2 * Math.PI * (Time.Second / 60.0), 0, reach * 0.85, Math.Max(1, size * 0.02), Style.SecondHandColor);
        }

        return image;
    }

    /// <summary>Draws a line from <paramref name="from"/> to <paramref name="to"/> pixels away from the center; angle 0 = 12 o'clock, clockwise.</summary>
    private static void DrawRay(
        SixelImage image, double centerX, double centerY, double angle, double from, double to, double thickness, SixelColor color)
    {
        double sin = Math.Sin(angle);
        double cos = Math.Cos(angle);
        Raster.FillLine(
            image,
            centerX + (from * sin),
            centerY - (from * cos),
            centerX + (to * sin),
            centerY - (to * cos),
            thickness,
            color);
    }

    private string CaptionPrefix() => string.IsNullOrEmpty(Caption) ? string.Empty : Caption + " ";

    private string TimeText(bool ansi) =>
        AnsiCodes.Colorize(
            Time.ToString(Style.TimeFormat, CultureInfo.InvariantCulture),
            Style.TextColor,
            ansi && Style.TextColor.A >= ColorAlphaLimit);
}
