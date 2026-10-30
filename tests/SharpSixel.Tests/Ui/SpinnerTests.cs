using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class SpinnerTests
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment AnsiAscii = new WidgetEnvironment(false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);

    [Fact]
    public void Ascii_CyclesFramesAndWraps()
    {
        var spinner = new Spinner();
        var frames = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            frames.Add(spinner.Render(Plain));
            spinner.Advance();
        }

        Assert.Equal(new[] { "|", "/", "-", "\\", "|" }, frames);
        Assert.Equal(5, spinner.Frame);
    }

    [Fact]
    public void Ascii_CaptionFollowsFrame() =>
        Assert.Equal("| Work", new Spinner { Caption = "Work" }.Render(Plain));

    [Fact]
    public void Ascii_WithAnsi_UsesActiveColorAndErasesLine() =>
        Assert.Equal("\u001b[38;2;52;152;219m|\u001b[0m\u001b[K", new Spinner().Render(AnsiAscii));

    [Fact]
    public void Ascii_WithoutAnsi_ContainsNoEscapes() =>
        Assert.DoesNotContain('\u001b', new Spinner { Caption = "x" }.Render(Plain));

    [Fact]
    public void Ascii_CustomFrames_AreUsedAndCopied()
    {
        var source = new[] { "a", "bb" };
        var spinner = new Spinner();
        spinner.Style.AsciiFrames = source;
        source[0] = "changed";
        spinner.Advance();
        Assert.Equal("bb", spinner.Render(Plain));
        spinner.Advance();
        Assert.Equal("a", spinner.Render(Plain));

        spinner.Style.AsciiFrames[0] = "mutated";
        Assert.Equal("a", spinner.Style.AsciiFrames[0]);
    }

    [Fact]
    public void GetRows_DependsOnMode()
    {
        var spinner = new Spinner();
        spinner.Style.HeightRows = 2;
        Assert.Equal(1, spinner.GetRows(Plain));
        Assert.Equal(2, spinner.GetRows(SixelEnv));
    }

    [Fact]
    public void InvalidValues_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new Spinner(null!));
        Assert.Throws<ArgumentNullException>(() => new SpinnerStyle().AsciiFrames = null!);
        Assert.Throws<ArgumentException>(() => new SpinnerStyle().AsciiFrames = new string[0]);
        Assert.Throws<ArgumentException>(() => new SpinnerStyle().AsciiFrames = new[] { "a", string.Empty });
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpinnerStyle().HeightRows = 21);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpinnerStyle().SegmentCount = 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpinnerStyle().TrailLength = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpinnerStyle().DotScale = 0.6);
    }

    [Fact]
    public void Image_IsSquareAndCellAligned()
    {
        var spinner = new Spinner();
        SixelImage one = spinner.CreateImage(SixelEnv);
        Assert.Equal(20, one.Width);
        Assert.Equal(18, one.Height);

        spinner.Style.HeightRows = 2;
        SixelImage two = spinner.CreateImage(SixelEnv);
        Assert.Equal(40, two.Width);
        Assert.Equal(36, two.Height);
        Assert.Equal(4, spinner.ImageColumns(SixelEnv));
    }

    [Fact]
    public void Image_HeadIsActiveAndTrailEndIsInactive()
    {
        var spinner = new Spinner();
        Assert.Equal(spinner.Style.ActiveColor, spinner.CreateImage(SixelEnv).GetPixel(10, 3));

        for (int i = 0; i < 4; i++)
        {
            spinner.Advance();
        }

        SixelImage image = spinner.CreateImage(SixelEnv);
        Assert.Equal(spinner.Style.InactiveColor, image.GetPixel(10, 3));
        Assert.Equal(spinner.Style.ActiveColor, image.GetPixel(10, 15));
    }

    [Fact]
    public void Image_TrailFadesBetweenColors()
    {
        var spinner = new Spinner();
        spinner.Advance();
        SixelColor behindHead = spinner.CreateImage(SixelEnv).GetPixel(10, 3);
        Assert.NotEqual(spinner.Style.ActiveColor, behindHead);
        Assert.NotEqual(spinner.Style.InactiveColor, behindHead);
    }

    [Fact]
    public void Image_BackgroundTransparentByDefaultAndFillableAndColorsCustomizable()
    {
        var spinner = new Spinner();
        Assert.Equal(0, spinner.CreateImage(SixelEnv).GetPixel(0, 0).A);

        spinner.Style.BackgroundColor = new SixelColor(1, 2, 3);
        spinner.Style.ActiveColor = new SixelColor(255, 0, 0);
        SixelImage image = spinner.CreateImage(SixelEnv);
        Assert.Equal(new SixelColor(1, 2, 3), image.GetPixel(0, 0));
        Assert.Equal(new SixelColor(255, 0, 0), image.GetPixel(10, 3));
    }

    [Fact]
    public void Image_TrailLongerThanSegmentCount_DoesNotThrow()
    {
        var spinner = new Spinner();
        spinner.Style.SegmentCount = 3;
        spinner.Style.TrailLength = 24;
        spinner.Advance();
        Assert.Equal(20, spinner.CreateImage(SixelEnv).Width);
    }

    [Fact]
    public void SixelFrame_ContainsImageCaptionAndErase()
    {
        string frame = new Spinner { Caption = "Work" }.Render(SixelEnv);
        Assert.StartsWith("\u001b7", frame);
        Assert.Contains("\u001bP", frame);
        Assert.Contains("\u001b[2X", frame);
        Assert.Contains("\u001b[2C Work\u001b[K", frame);
        Assert.EndsWith("\u001b8", frame);
    }

    [Fact]
    public void SixelFrame_OpaqueBackground_SkipsErase()
    {
        var spinner = new Spinner();
        spinner.Style.BackgroundColor = new SixelColor(0, 0, 0);
        Assert.DoesNotContain("\u001b[2X", spinner.Render(SixelEnv));
    }
}
