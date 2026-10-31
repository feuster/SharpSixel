using System.Globalization;
using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class DateDisplayTests
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment AnsiAscii = new WidgetEnvironment(false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);
    private static readonly SixelColor Gray = new SixelColor(128, 128, 128);

    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    private static DateDisplay On(int day) => WithEnUs(new DateDisplay { Date = new DateTime(2026, 10, day) });

    private static DateDisplay WithEnUs(DateDisplay display)
    {
        display.Style.Culture = EnUs;
        return display;
    }

    [Fact]
    public void Ascii_ShowsFormattedDateWithCaption()
    {
        Assert.Equal("Wed, 10/28/2026", On(28).Render(Plain));
        Assert.Equal(
            "Today Wed, 10/28/2026",
            WithEnUs(new DateDisplay { Date = new DateTime(2026, 10, 28), Caption = "Today" }).Render(Plain));
    }

    [Fact]
    public void Ascii_UsesDateOrderAndNamesOfTheCulture()
    {
        DateDisplay display = On(28);
        display.Style.Culture = CultureInfo.GetCultureInfo("de-DE");
        string text = display.Render(Plain);
        Assert.StartsWith("Mi", text, StringComparison.Ordinal);
        Assert.EndsWith(", 28.10.2026", text, StringComparison.Ordinal);

        display.Style.Culture = CultureInfo.GetCultureInfo("en-GB");
        Assert.Equal("Wed, 28/10/2026", display.Render(Plain));
    }

    [Fact]
    public void Ascii_WithoutCulture_UsesCurrentCulture()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var display = new DateDisplay { Date = new DateTime(2026, 10, 28) };
            Assert.EndsWith(", 28.10.2026", display.Render(Plain), StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Ascii_CustomFormatUsesTheCulture()
    {
        DateDisplay display = On(28);
        display.Style.DateFormat = "dddd, d MMMM yyyy";
        Assert.Equal("Wednesday, 28 October 2026", display.Render(Plain));
        display.Style.Culture = CultureInfo.GetCultureInfo("de-DE");
        Assert.Equal("Mittwoch, 28 Oktober 2026", display.Render(Plain));
    }

    [Fact]
    public void DateFormat_NullRestoresLocalizedDefault()
    {
        DateDisplay display = On(28);
        display.Style.DateFormat = "yyyy-MM-dd";
        display.Style.DateFormat = null;
        Assert.Null(display.Style.DateFormat);
        Assert.Equal("Wed, 10/28/2026", display.Render(Plain));
    }

    [Fact]
    public void Ascii_CustomFormatIsUsed()
    {
        DateDisplay display = On(28);
        display.Style.DateFormat = "yyyy-MM-dd";
        Assert.Equal("2026-10-28", display.Render(Plain));
    }

    [Fact]
    public void Ascii_WithAnsi_ErasesLineAndColorsTextOnlyIfSet()
    {
        DateDisplay display = On(28);
        Assert.Equal("Wed, 10/28/2026\u001b[K", display.Render(AnsiAscii));

        display.Style.TextColor = new SixelColor(255, 0, 0);
        Assert.Equal("\u001b[38;2;255;0;0mWed, 10/28/2026\u001b[0m\u001b[K", display.Render(AnsiAscii));
        Assert.DoesNotContain("\u001b", display.Render(Plain), StringComparison.Ordinal);
    }

    [Fact]
    public void GetRows_DependsOnMode()
    {
        var display = new DateDisplay();
        display.Style.HeightRows = 2;
        Assert.Equal(1, display.GetRows(Plain));
        Assert.Equal(2, display.GetRows(SixelEnv));
    }

    [Fact]
    public void InvalidValues_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new DateDisplay(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DateDisplayStyle().HeightRows = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => new DateDisplayStyle().LineThickness = 5);
        Assert.Throws<ArgumentOutOfRangeException>(() => new DateDisplayStyle().CornerRadius = -1);
        Assert.Throws<ArgumentException>(() => new DateDisplayStyle().DateFormat = string.Empty);
        Assert.Throws<FormatException>(() => new DateDisplayStyle().DateFormat = "%");
    }

    [Fact]
    public void Image_IsSquareAndCellAligned()
    {
        DateDisplay display = On(5);
        SixelImage image = display.CreateImage(SixelEnv);
        Assert.Equal(60, image.Width);
        Assert.Equal(60, image.Height);
        Assert.Equal(6, display.ImageColumns(SixelEnv));
    }

    [Fact]
    public void Image_DrawsOutlineSeparatorRingsAndTransparentPage()
    {
        SixelImage image = On(5).CreateImage(SixelEnv);
        Assert.Equal(Gray, image.GetPixel(30, 5));
        Assert.Equal(Gray, image.GetPixel(30, 21));
        Assert.Equal(new DateDisplayStyle().AccentColor, image.GetPixel(15, 2));
        Assert.Equal(0, image.GetPixel(30, 12).A);
        Assert.Equal(0, image.GetPixel(0, 0).A);
    }

    [Fact]
    public void Image_DrawsTheDayAsSevenSegmentDigits()
    {
        SixelColor digit = new DateDisplayStyle().DigitColor;
        Assert.Equal(digit, On(8).CreateImage(SixelEnv).GetPixel(24, 33));
        Assert.Equal(0, On(1).CreateImage(SixelEnv).GetPixel(24, 33).A);
        Assert.Equal(digit, On(1).CreateImage(SixelEnv).GetPixel(34, 33));
    }

    [Fact]
    public void Image_TwoDigitDayDiffersFromSingleDigit()
    {
        SixelImage single = On(8).CreateImage(SixelEnv);
        SixelImage twice = On(28).CreateImage(SixelEnv);
        Assert.NotEqual(single.GetPixel(24, 33), twice.GetPixel(24, 33));
    }

    [Fact]
    public void Image_ColorsAreCustomizable()
    {
        DateDisplay display = On(8);
        display.Style.BackgroundColor = new SixelColor(1, 2, 3);
        display.Style.PaperColor = new SixelColor(4, 5, 6);
        display.Style.BorderColor = new SixelColor(7, 8, 9);
        display.Style.DigitColor = new SixelColor(255, 0, 0);
        SixelImage image = display.CreateImage(SixelEnv);
        Assert.Equal(new SixelColor(1, 2, 3), image.GetPixel(0, 0));
        Assert.Equal(new SixelColor(4, 5, 6), image.GetPixel(30, 12));
        Assert.Equal(new SixelColor(7, 8, 9), image.GetPixel(30, 5));
        Assert.Equal(new SixelColor(255, 0, 0), image.GetPixel(24, 33));
    }

    [Fact]
    public void Image_SmallestSize_DoesNotThrow()
    {
        foreach (int rows in new[] { 1, 2, 20 })
        {
            DateDisplay display = On(28);
            display.Style.HeightRows = rows;
            display.Style.LineThickness = 4;
            Assert.True(display.CreateImage(SixelEnv).Width > 0);
        }
    }

    [Fact]
    public void SixelFrame_ContainsImageDateAndErase()
    {
        string frame = WithEnUs(new DateDisplay { Date = new DateTime(2026, 10, 28), Caption = "Today" }).Render(SixelEnv);
        Assert.StartsWith("\u001b7", frame, StringComparison.Ordinal);
        Assert.Contains("\u001bP", frame, StringComparison.Ordinal);
        Assert.Contains("\u001b[6X", frame, StringComparison.Ordinal);
        Assert.Contains("\u001b[1BToday ", frame, StringComparison.Ordinal);
        Assert.Contains(" Wed, 10/28/2026\u001b[K", frame, StringComparison.Ordinal);
        Assert.EndsWith("\u001b8", frame, StringComparison.Ordinal);
    }

    [Fact]
    public void SixelFrame_OpaqueBackground_SkipsErase()
    {
        var display = new DateDisplay();
        display.Style.BackgroundColor = new SixelColor(0, 0, 0);
        Assert.DoesNotContain("\u001b[6X", display.Render(SixelEnv), StringComparison.Ordinal);
    }
}
