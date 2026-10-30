using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class ProgressBarTests
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment AnsiAscii = new WidgetEnvironment(false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);

    private static ProgressBar Bar(double value, ProgressTextMode text = ProgressTextMode.None, string? caption = null)
    {
        var bar = new ProgressBar { Value = value, ProgressText = text, Caption = caption };
        bar.Style.Width = 12;
        return bar;
    }

    [Theory]
    [InlineData(0, "[----------]")]
    [InlineData(50, "[#####-----]")]
    [InlineData(100, "[##########]")]
    [InlineData(150, "[##########]")]
    [InlineData(-5, "[----------]")]
    public void Ascii_DrawsBarAndClamps(double value, string expected) =>
        Assert.Equal(expected, Bar(value).Render(Plain));

    [Fact]
    public void Ascii_NaNValue_IsEmpty() =>
        Assert.Equal("[----------]", Bar(double.NaN).Render(Plain));

    [Fact]
    public void Ascii_Percent_IsPaddedToThreeDigits()
    {
        Assert.Equal("[#####-----]  50%", Bar(50, ProgressTextMode.Percent).Render(Plain));
        Assert.Equal("[----------]   0%", Bar(0, ProgressTextMode.Percent).Render(Plain));
        Assert.Equal("[##########] 100%", Bar(100, ProgressTextMode.Percent).Render(Plain));
    }

    [Fact]
    public void Ascii_FractionText_UsesValueAndMaximum()
    {
        var bar = Bar(5, ProgressTextMode.Fraction);
        bar.Maximum = 10;
        Assert.Equal("[#####-----] 5/10", bar.Render(Plain));
    }

    [Fact]
    public void Ascii_Caption_IsPrefixed() =>
        Assert.Equal("Task [#####-----]  50%", Bar(50, ProgressTextMode.Percent, "Task").Render(Plain));

    [Fact]
    public void Ascii_CustomCharacters_AreUsed()
    {
        var bar = Bar(50);
        bar.Style.AsciiBorderLeft = '<';
        bar.Style.AsciiBorderRight = '>';
        bar.Style.AsciiFill = '=';
        bar.Style.AsciiEmpty = '.';
        Assert.Equal("<=====.....>", bar.Render(Plain));
    }

    [Fact]
    public void Ascii_WithAnsi_ColorsPartsAndErasesLine()
    {
        string frame = Bar(50).Render(AnsiAscii);
        Assert.Contains("\u001b[38;2;46;204;113m#####\u001b[0m", frame);
        Assert.Contains("\u001b[38;2;64;64;64m-----\u001b[0m", frame);
        Assert.EndsWith("\u001b[K", frame);
    }

    [Fact]
    public void Ascii_WithoutAnsi_ContainsNoEscapes() =>
        Assert.DoesNotContain('\u001b', Bar(50, ProgressTextMode.Percent, "x").Render(Plain));

    [Fact]
    public void GetRows_DependsOnMode()
    {
        var bar = Bar(0);
        bar.Style.HeightRows = 3;
        Assert.Equal(1, bar.GetRows(Plain));
        Assert.Equal(3, bar.GetRows(SixelEnv));
    }

    [Fact]
    public void InvalidValues_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProgressBar().Maximum = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProgressBar().Maximum = double.NaN);
        Assert.Throws<ArgumentNullException>(() => new ProgressBar(null!));
        Assert.Throws<ArgumentNullException>(() => new ProgressBar().Render(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProgressBarStyle().Width = 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProgressBarStyle().HeightRows = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProgressBarStyle().BarThickness = 0.05);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProgressBarStyle().CornerRadius = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProgressBarStyle().BorderThickness = -1);
    }

    private static ProgressBar SquareBar(double value)
    {
        var bar = new ProgressBar { Value = value };
        bar.Style.Width = 10;
        bar.Style.CornerRadius = 0;
        return bar;
    }

    [Fact]
    public void Image_HasCellBasedSize()
    {
        SixelImage image = SquareBar(0).CreateImage(SixelEnv);
        Assert.Equal(100, image.Width);
        Assert.Equal(18, image.Height);
    }

    [Fact]
    public void Image_DrawsBorderFillTrackAndTransparency()
    {
        var bar = SquareBar(50);
        SixelImage image = bar.CreateImage(SixelEnv);
        Assert.Equal(bar.Style.BorderColor, image.GetPixel(0, 10));
        Assert.Equal(bar.Style.BorderColor, image.GetPixel(99, 10));
        Assert.Equal(bar.Style.FillColor, image.GetPixel(20, 10));
        Assert.Equal(bar.Style.TrackColor, image.GetPixel(80, 10));
        Assert.Equal(0, image.GetPixel(50, 1).A);
        Assert.Equal(0, image.GetPixel(50, 16).A);
    }

    [Fact]
    public void Image_ZeroAndFullProgress_FillNothingOrEverything()
    {
        var empty = SquareBar(0);
        Assert.Equal(empty.Style.TrackColor, empty.CreateImage(SixelEnv).GetPixel(2, 10));
        var full = SquareBar(100);
        Assert.Equal(full.Style.FillColor, full.CreateImage(SixelEnv).GetPixel(97, 10));
    }

    [Fact]
    public void Image_RoundedCorner_IsTransparent()
    {
        var bar = SquareBar(0);
        bar.Style.CornerRadius = 6;
        SixelImage image = bar.CreateImage(SixelEnv);
        Assert.Equal(0, image.GetPixel(0, 6).A);
        Assert.NotEqual(0, image.GetPixel(50, 6).A);
    }

    [Fact]
    public void Image_BarIsCenteredInTheCellNotInTheShorterImage()
    {
        SixelImage image = SquareBar(0).CreateImage(new WidgetEnvironment(true, 10, 19));
        Assert.Equal(18, image.Height);
        Assert.Equal(0, image.GetPixel(50, 3).A);
        Assert.NotEqual(0, image.GetPixel(50, 4).A);
        Assert.NotEqual(0, image.GetPixel(50, 14).A);
        Assert.Equal(0, image.GetPixel(50, 15).A);
    }

    [Fact]
    public void Image_NoBorder_StartsWithTrack()
    {
        var bar = SquareBar(0);
        bar.Style.BorderThickness = 0;
        Assert.Equal(bar.Style.TrackColor, bar.CreateImage(SixelEnv).GetPixel(0, 10));
    }

    [Fact]
    public void Image_OpaqueBackground_FillsOutsideTheBar()
    {
        var bar = SquareBar(0);
        bar.Style.BackgroundColor = new SixelColor(1, 2, 3);
        Assert.Equal(new SixelColor(1, 2, 3), bar.CreateImage(SixelEnv).GetPixel(50, 1));
    }

    [Fact]
    public void Image_CustomColors_AreUsed()
    {
        var bar = SquareBar(100);
        bar.Style.FillColor = new SixelColor(255, 0, 0);
        Assert.Equal(new SixelColor(255, 0, 0), bar.CreateImage(SixelEnv).GetPixel(50, 10));
    }

    [Fact]
    public void SixelFrame_ContainsSixelDataAndCursorHandling()
    {
        string frame = SquareBar(50).Render(SixelEnv);
        Assert.StartsWith("\u001b7", frame);
        Assert.Contains("\u001bP", frame);
        Assert.Contains("\u001b\\", frame);
        Assert.EndsWith("\u001b8", frame);
    }

    [Fact]
    public void SixelFrame_ErasesCellsOnlyForTransparentBackground()
    {
        var transparent = SquareBar(50);
        Assert.Contains("\u001b[10X", transparent.Render(SixelEnv));

        var opaque = SquareBar(50);
        opaque.Style.BackgroundColor = new SixelColor(0, 0, 0);
        Assert.DoesNotContain("\u001b[10X", opaque.Render(SixelEnv));
    }

    [Fact]
    public void SixelFrame_PlacesCaptionAndProgressTextAroundImage()
    {
        var bar = SquareBar(50);
        bar.Caption = "Hi";
        bar.ProgressText = ProgressTextMode.Percent;
        string frame = bar.Render(SixelEnv);
        Assert.Contains("Hi \u001b8", frame);
        Assert.Contains("\u001b[3C\u001bP", frame);
        Assert.Contains("\u001b[13C  50%\u001b[K\u001b8", frame);
    }

    [Fact]
    public void SixelFrame_MultiRow_CentersTextRow()
    {
        var bar = SquareBar(50);
        bar.Style.HeightRows = 3;
        bar.ProgressText = ProgressTextMode.Percent;
        Assert.Contains("\u001b[1B\u001b[10C  50%", bar.Render(SixelEnv));
    }
}
