using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class ClockTests
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment AnsiAscii = new WidgetEnvironment(false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);
    private static readonly DateTime Afternoon = new DateTime(2026, 10, 28, 14, 5, 9);
    private static readonly DateTime ThreeOClock = new DateTime(2026, 10, 28, 3, 0, 0);

    private static Clock At(DateTime time) => new Clock { Time = time };

    [Fact]
    public void Ascii_ShowsFormattedTimeWithCaption()
    {
        Assert.Equal("14:05:09", At(Afternoon).Render(Plain));
        Assert.Equal("Now 14:05:09", new Clock { Time = Afternoon, Caption = "Now" }.Render(Plain));
    }

    [Fact]
    public void Ascii_CustomFormatIsUsed()
    {
        var clock = At(Afternoon);
        clock.Style.TimeFormat = "hh:mm tt";
        Assert.Equal("02:05 PM", clock.Render(Plain));
    }

    [Fact]
    public void Ascii_WithAnsi_ErasesLineAndColorsTextOnlyIfSet()
    {
        var clock = At(Afternoon);
        Assert.Equal("14:05:09\u001b[K", clock.Render(AnsiAscii));

        clock.Style.TextColor = new SixelColor(255, 0, 0);
        Assert.Equal("\u001b[38;2;255;0;0m14:05:09\u001b[0m\u001b[K", clock.Render(AnsiAscii));
        Assert.DoesNotContain("\u001b", clock.Render(Plain), StringComparison.Ordinal);
    }

    [Fact]
    public void GetRows_DependsOnMode()
    {
        var clock = new Clock();
        clock.Style.HeightRows = 2;
        Assert.Equal(1, clock.GetRows(Plain));
        Assert.Equal(2, clock.GetRows(SixelEnv));
    }

    [Fact]
    public void InvalidValues_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new Clock(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ClockStyle().HeightRows = 21);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ClockStyle().LineThickness = 5);
        Assert.Throws<ArgumentNullException>(() => new ClockStyle().TimeFormat = null!);
        Assert.Throws<ArgumentException>(() => new ClockStyle().TimeFormat = string.Empty);
        Assert.Throws<FormatException>(() => new ClockStyle().TimeFormat = "%");
    }

    [Fact]
    public void Image_IsSquareAndCellAligned()
    {
        var clock = new Clock();
        SixelImage image = clock.CreateImage(SixelEnv);
        Assert.Equal(60, image.Width);
        Assert.Equal(60, image.Height);
        Assert.Equal(6, clock.ImageColumns(SixelEnv));

        clock.Style.HeightRows = 1;
        Assert.Equal(20, clock.CreateImage(SixelEnv).Width);
        Assert.Equal(18, clock.CreateImage(SixelEnv).Height);
    }

    [Fact]
    public void Image_DrawsRimTicksAndTransparentFace()
    {
        SixelImage image = At(ThreeOClock).CreateImage(SixelEnv);
        Assert.Equal(new SixelColor(128, 128, 128), image.GetPixel(30, 0));
        Assert.Equal(new SixelColor(128, 128, 128), image.GetPixel(6, 30));
        Assert.Equal(0, image.GetPixel(10, 30).A);
        Assert.Equal(0, image.GetPixel(0, 0).A);
    }

    [Fact]
    public void Image_HandsPointToTheTime()
    {
        var clock = At(ThreeOClock);
        clock.Style.ShowSecondHand = false;
        SixelImage image = clock.CreateImage(SixelEnv);
        Assert.Equal(clock.Style.HandColor, image.GetPixel(40, 30));
        Assert.Equal(clock.Style.HandColor, image.GetPixel(30, 20));
        Assert.Equal(0, image.GetPixel(30, 40).A);
    }

    [Fact]
    public void Image_SecondHandCanBeHidden()
    {
        var clock = At(new DateTime(2026, 10, 28, 3, 0, 30));
        Assert.Equal(clock.Style.SecondHandColor, clock.CreateImage(SixelEnv).GetPixel(30, 40));

        clock.Style.ShowSecondHand = false;
        Assert.Equal(0, clock.CreateImage(SixelEnv).GetPixel(30, 40).A);
    }

    [Fact]
    public void Image_HourHandUses12HourDial()
    {
        var clock = At(new DateTime(2026, 10, 28, 15, 0, 0));
        clock.Style.ShowSecondHand = false;
        Assert.Equal(clock.Style.HandColor, clock.CreateImage(SixelEnv).GetPixel(40, 30));
    }

    [Fact]
    public void Image_ColorsAreCustomizable()
    {
        var clock = At(ThreeOClock);
        clock.Style.BackgroundColor = new SixelColor(1, 2, 3);
        clock.Style.FaceColor = new SixelColor(4, 5, 6);
        clock.Style.BorderColor = new SixelColor(7, 8, 9);
        clock.Style.HandColor = new SixelColor(255, 0, 0);
        SixelImage image = clock.CreateImage(SixelEnv);
        Assert.Equal(new SixelColor(1, 2, 3), image.GetPixel(0, 0));
        Assert.Equal(new SixelColor(4, 5, 6), image.GetPixel(10, 30));
        Assert.Equal(new SixelColor(7, 8, 9), image.GetPixel(30, 0));
        Assert.Equal(new SixelColor(255, 0, 0), image.GetPixel(40, 30));
    }

    [Fact]
    public void SixelFrame_ContainsImageTimeAndErase()
    {
        string frame = new Clock { Time = Afternoon, Caption = "Now" }.Render(SixelEnv);
        Assert.StartsWith("\u001b7", frame, StringComparison.Ordinal);
        Assert.Contains("\u001bP", frame, StringComparison.Ordinal);
        Assert.Contains("\u001b[6X", frame, StringComparison.Ordinal);
        Assert.Contains("\u001b[1BNow ", frame, StringComparison.Ordinal);
        Assert.Contains(" 14:05:09\u001b[K", frame, StringComparison.Ordinal);
        Assert.EndsWith("\u001b8", frame, StringComparison.Ordinal);
    }

    [Fact]
    public void SixelFrame_OpaqueBackground_SkipsErase()
    {
        var clock = new Clock();
        clock.Style.BackgroundColor = new SixelColor(0, 0, 0);
        Assert.DoesNotContain("\u001b[6X", clock.Render(SixelEnv), StringComparison.Ordinal);
    }
}
