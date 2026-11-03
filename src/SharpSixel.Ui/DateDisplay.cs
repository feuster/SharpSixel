using System;
using System.Globalization;
using System.Text;

namespace SharpSixel.Ui;

/// <summary>
/// Date display showing <see cref="Date"/>. Sixel: calendar page graphic with the day number in seven-segment digits,
/// followed by the formatted date as terminal text. ASCII: the formatted date only. The widget does not follow the clock
/// by itself: set <see cref="Date"/> (for example to <see cref="DateTime.Today"/>) before each frame.
/// </summary>
public sealed class DateDisplay : Widget
{
    private const int ColorAlphaLimit = 128;

    // Seven-segment masks of the digits 0-9: bit 0x40 = a (top), 0x20 = b, 0x10 = c, 0x08 = d (bottom), 0x04 = e, 0x02 = f, 0x01 = g (middle).
    private static readonly int[] Segments = { 0x7E, 0x30, 0x6D, 0x79, 0x33, 0x5B, 0x5F, 0x70, 0x7F, 0x7B };

    /// <summary>Creates a date display with the default style.</summary>
    public DateDisplay()
        : this(new DateDisplayStyle())
    {
    }

    /// <summary>Creates a date display with the given style.</summary>
    public DateDisplay(DateDisplayStyle style)
    {
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    /// <summary>Look of the date display; may be changed between frames.</summary>
    public DateDisplayStyle Style { get; }

    /// <summary>Date to show (only the date part is used). Default 0001-01-01.</summary>
    public DateTime Date { get; set; } = DateTime.MinValue;

    /// <summary>Optional caption shown before the date display.</summary>
    public string? Caption { get; set; }

    /// <inheritdoc />
    protected override int SixelRows => Style.HeightRows;

    /// <inheritdoc />
    protected override string RenderAscii(WidgetEnvironment environment)
    {
        var sb = new StringBuilder();
        sb.Append(CaptionPrefix());
        sb.Append(DateText(environment.UseAnsi));
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
            " " + DateText(true),
            Style.BackgroundColor.A < ColorAlphaLimit);

    internal int ImageColumns(WidgetEnvironment environment) =>
        (int)Math.Ceiling((double)environment.GetImageHeight(Style.HeightRows) / environment.CellWidth);

    /// <inheritdoc />
    protected override int MeasureColumns(WidgetEnvironment environment) =>
        environment.UseSixel
            ? TextFit.Width(CaptionPrefix()) + ImageColumns(environment) + 1 + TextFit.Width(DateText(false))
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

        int line = Style.LineThickness;
        int radius = Style.CornerRadius;
        int ringHeight = Math.Max(3, size / 6);
        int ringWidth = Math.Max(2, size / 12);
        int left = (width - size) / 2;
        int top = ringHeight / 2;
        int pageHeight = size - top;
        Raster.FillRoundedRect(image, left, top, size, pageHeight, radius, Style.BorderColor);
        Raster.FillRoundedRect(
            image, left + line, top + line, size - (2 * line), pageHeight - (2 * line), Math.Max(0, radius - line), Style.PaperColor);

        int separatorTop = top + (pageHeight * 3 / 10);
        Raster.FillRoundedRect(image, left + line, separatorTop, size - (2 * line), line, 0, Style.BorderColor);
        foreach (int center in new[] { left + (size / 4), left + (size * 3 / 4) })
        {
            Raster.FillRoundedRect(image, center - (ringWidth / 2), 0, ringWidth, ringHeight, ringWidth / 2, Style.AccentColor);
        }

        DrawDay(image, left, size, separatorTop + line, top + pageHeight - line);
        return image;
    }

    private void DrawDay(SixelImage image, int left, int size, int bodyTop, int bodyBottom)
    {
        int bodyHeight = bodyBottom - bodyTop;
        if (bodyHeight < 3)
        {
            return;
        }

        string day = Date.Day.ToString(CultureInfo.InvariantCulture);
        int height = Math.Max(3, bodyHeight * 7 / 10);
        int width = Math.Max(2, height * 5 / 9);
        int stroke = Math.Max(1, height / 7);
        int gap = stroke;
        int x = left + ((size - ((day.Length * width) + ((day.Length - 1) * gap))) / 2);
        int y = bodyTop + ((bodyHeight - height) / 2);
        foreach (char digit in day)
        {
            DrawDigit(image, x, y, width, height, stroke, digit - '0', Style.DigitColor);
            x += width + gap;
        }
    }

    private static void DrawDigit(SixelImage image, int x, int y, int width, int height, int stroke, int digit, SixelColor color)
    {
        int mask = Segments[digit];
        int middle = y + ((height - stroke) / 2);
        int upper = middle + stroke - y;
        int lower = y + height - middle;
        int right = x + width - stroke;
        Segment(image, mask, 0x40, x, y, width, stroke, color);
        Segment(image, mask, 0x20, right, y, stroke, upper, color);
        Segment(image, mask, 0x10, right, middle, stroke, lower, color);
        Segment(image, mask, 0x08, x, y + height - stroke, width, stroke, color);
        Segment(image, mask, 0x04, x, middle, stroke, lower, color);
        Segment(image, mask, 0x02, x, y, stroke, upper, color);
        Segment(image, mask, 0x01, x, middle, width, stroke, color);
    }

    private static void Segment(SixelImage image, int mask, int bit, int x, int y, int width, int height, SixelColor color)
    {
        if ((mask & bit) != 0)
        {
            Raster.FillRoundedRect(image, x, y, width, height, 0, color);
        }
    }

    private string CaptionPrefix() => string.IsNullOrEmpty(Caption) ? string.Empty : Caption + " ";

    private string DateText(bool ansi) =>
        AnsiCodes.Colorize(
            Style.Format(Date),
            Style.TextColor,
            ansi && Style.TextColor.A >= ColorAlphaLimit);
}
