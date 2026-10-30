using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class BarChartTests
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment AnsiAscii = new WidgetEnvironment(false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);
    private static readonly SixelColor First = new SixelColor(46, 204, 113);
    private static readonly SixelColor Second = new SixelColor(52, 152, 219);

    private static BarChart Chart(string? caption = null)
    {
        var chart = new BarChart { Caption = caption };
        chart.Style.Width = 10;
        chart.Style.CornerRadius = 0;
        chart.Add("A", 5);
        chart.Add("BB", 10);
        return chart;
    }

    [Fact]
    public void Ascii_DrawsLabelsBarsAndAlignedValues()
    {
        string expected = "A  " + "#####" + "     " + " " + " 5" + "\n" + "BB " + "##########" + " " + "10";
        Assert.Equal(expected, Chart().Render(Plain));
    }

    [Fact]
    public void Ascii_Caption_IsFirstLine()
    {
        string[] lines = Chart("Title").Render(Plain).Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.Equal("Title", lines[0]);
    }

    [Fact]
    public void Ascii_ExplicitMaximum_ScalesAndClamps()
    {
        BarChart chart = Chart();
        chart.Maximum = 20;
        chart.Add("C", 40);
        string[] lines = chart.Render(Plain).Split('\n');
        Assert.StartsWith("A  ###" + "       ", lines[0]);
        Assert.StartsWith("BB #####" + "     ", lines[1]);
        Assert.StartsWith("C  ##########", lines[2]);
    }

    [Fact]
    public void Ascii_NegativeNaNAndAllZero_DrawEmptyBars()
    {
        var chart = new BarChart();
        chart.Style.Width = 4;
        chart.Add("n", -3);
        chart.Add("x", double.NaN);
        chart.Add("z", 0);
        foreach (string line in chart.Render(Plain).Split('\n'))
        {
            Assert.DoesNotContain('#', line);
        }
    }

    [Fact]
    public void Ascii_HideValuesAndFormat()
    {
        BarChart chart = Chart();
        chart.ShowValues = false;
        Assert.DoesNotContain("10", chart.Render(Plain));

        chart.ShowValues = true;
        chart.ValueFormat = "0.0";
        Assert.EndsWith("10.0", chart.Render(Plain));
        Assert.Contains(" 5.0\n", chart.Render(Plain));
    }

    [Fact]
    public void Ascii_CustomCharacters_AreUsed()
    {
        BarChart chart = Chart();
        chart.Style.AsciiFill = '=';
        chart.Style.AsciiEmpty = '.';
        Assert.StartsWith("A  =====.....", chart.Render(Plain));
    }

    [Fact]
    public void Ascii_WithAnsi_UsesPaletteAndItemColors()
    {
        BarChart chart = Chart();
        chart.Items[1].Color = new SixelColor(255, 0, 0);
        string frame = chart.Render(AnsiAscii);
        Assert.Contains("\u001b[38;2;46;204;113m#####\u001b[0m", frame);
        Assert.Contains("\u001b[38;2;255;0;0m##########\u001b[0m", frame);
        Assert.DoesNotContain('\u001b', chart.Render(Plain));
    }

    [Fact]
    public void Palette_IsRepeated()
    {
        BarChart chart = Chart();
        chart.Style.Palette = new[] { First };
        Assert.Equal(2, chart.Render(AnsiAscii).Split(new[] { "\u001b[38;2;46;204;113m" }, StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void GetRows_DependsOnModeCaptionAndItems()
    {
        BarChart chart = Chart();
        chart.Style.BarHeightRows = 3;
        Assert.Equal(2, chart.GetRows(Plain));
        Assert.Equal(6, chart.GetRows(SixelEnv));
        chart.Caption = "T";
        Assert.Equal(3, chart.GetRows(Plain));
        Assert.Equal(7, chart.GetRows(SixelEnv));
    }

    [Fact]
    public void Empty_HasOneRowAndNoContent()
    {
        var chart = new BarChart();
        Assert.Equal(1, chart.GetRows(Plain));
        Assert.Equal(1, chart.GetRows(SixelEnv));
        Assert.Equal(string.Empty, chart.Render(Plain));
        string frame = chart.Render(SixelEnv);
        Assert.DoesNotContain("\u001bP", frame);
        Assert.EndsWith("\u001b8", frame);
    }

    private static void Fails<T>(Action action)
        where T : Exception => Assert.Throws<T>(action);

    [Fact]
    public void InvalidValues_Throw()
    {
        Fails<ArgumentNullException>(() => new BarChart(null!));
        Fails<ArgumentNullException>(() => new BarChart().Render(null!));
        Fails<ArgumentNullException>(() => new BarChart().Add(null!, 1));
        Fails<ArgumentNullException>(() => new BarChart().Items.Add(null!));
        Fails<ArgumentNullException>(() => new BarChartItem("x", 1).Label = null!);
        Fails<ArgumentOutOfRangeException>(() => new BarChart().Maximum = 0);
        Fails<ArgumentOutOfRangeException>(() => new BarChart().Maximum = double.NaN);
        Fails<ArgumentNullException>(() => new BarChart().ValueFormat = null!);
        Fails<ArgumentException>(() => new BarChart().ValueFormat = "x");
        Fails<ArgumentOutOfRangeException>(() => new BarChartStyle().Width = 0);
        Fails<ArgumentOutOfRangeException>(() => new BarChartStyle().BarHeightRows = 0);
        Fails<ArgumentOutOfRangeException>(() => new BarChartStyle().BarHeightRows = 11);
        Fails<ArgumentOutOfRangeException>(() => new BarChartStyle().BarThickness = 0.05);
        Fails<ArgumentOutOfRangeException>(() => new BarChartStyle().CornerRadius = -1);
        Fails<ArgumentNullException>(() => new BarChartStyle().Palette = null!);
        Fails<ArgumentException>(() => new BarChartStyle().Palette = new SixelColor[0]);
    }

    [Fact]
    public void Image_HasCellBasedSize()
    {
        SixelImage image = Chart().CreateImage(SixelEnv);
        Assert.Equal(100, image.Width);
        Assert.Equal(36, image.Height);
    }

    [Fact]
    public void Image_DrawsProportionalBarsInOwnColors()
    {
        SixelImage image = Chart().CreateImage(SixelEnv);
        Assert.Equal(First, image.GetPixel(10, 8));
        Assert.Equal(0, image.GetPixel(60, 8).A);
        Assert.Equal(0, image.GetPixel(10, 1).A);
        Assert.Equal(Second, image.GetPixel(95, 26));
    }

    [Fact]
    public void Image_BarsFollowTheCellRowsInsteadOfTheShorterImage()
    {
        var chart = new BarChart();
        for (int i = 0; i < 4; i++)
        {
            chart.Add("x", 10);
        }

        SixelImage image = chart.CreateImage(new WidgetEnvironment(true, 10, 19));
        Assert.Equal(72, image.Height);
        foreach (int top in new[] { 4, 23, 42, 61 })
        {
            Assert.Equal(0, image.GetPixel(50, top - 1).A);
            Assert.NotEqual(0, image.GetPixel(50, top).A);
            Assert.NotEqual(0, image.GetPixel(50, top + 10).A);
        }
    }

    [Fact]
    public void Image_TrackAndBackground_AreDrawnWhenSet()
    {
        BarChart chart = Chart();
        chart.Style.TrackColor = new SixelColor(1, 2, 3);
        chart.Style.BackgroundColor = new SixelColor(4, 5, 6);
        SixelImage image = chart.CreateImage(SixelEnv);
        Assert.Equal(new SixelColor(1, 2, 3), image.GetPixel(60, 8));
        Assert.Equal(new SixelColor(4, 5, 6), image.GetPixel(60, 1));
    }

    [Fact]
    public void SixelFrame_PlacesLabelsGraphicAndValues()
    {
        string frame = Chart().Render(SixelEnv);
        Assert.StartsWith("\u001b7A\u001b8", frame);
        Assert.EndsWith("\u001b8", frame);
        Assert.Contains("\u001b[1BBB\u001b8", frame);
        Assert.Contains("\u001b[3C\u001b[10X", frame);
        Assert.Contains("\u001b[3C\u001bP", frame);
        Assert.Contains("\u001b[13C  5\u001b[K\u001b8", frame);
        Assert.Contains("\u001b[1B\u001b[13C 10\u001b[K\u001b8", frame);
    }

    [Fact]
    public void SixelFrame_OpaqueBackground_SkipsErase()
    {
        BarChart chart = Chart();
        chart.Style.BackgroundColor = new SixelColor(0, 0, 0);
        Assert.DoesNotContain("[10X", chart.Render(SixelEnv));
    }

    [Fact]
    public void SixelFrame_CaptionAndTallBars_ShiftRows()
    {
        BarChart chart = Chart("Title");
        Assert.Contains("Title\u001b[K\u001b8", chart.Render(SixelEnv));
        Assert.Contains("\u001b[1BA\u001b8", chart.Render(SixelEnv));

        BarChart tall = Chart();
        tall.Style.BarHeightRows = 3;
        string frame = tall.Render(SixelEnv);
        Assert.Contains("\u001b[1BA\u001b8", frame);
        Assert.Contains("\u001b[4BBB\u001b8", frame);
    }

    [Fact]
    public void LiveRegion_PlacesEachAsciiLineOnItsRow()
    {
        var output = new StringWriter();
        var region = new LiveRegion(output, Plain);
        BarChart chart = Chart();
        region.Begin(chart.GetRows(Plain));
        region.Update(chart);
        string[] lines = chart.Render(Plain).Split('\n');
        string text = output.ToString();
        Assert.Contains("\u001b8" + lines[0] + "\u001b[K", text);
        Assert.Contains("\u001b8\u001b[1B" + lines[1] + "\u001b[K", text);
        Assert.Throws<ArgumentException>(() =>
        {
            var small = new LiveRegion(new StringWriter(), Plain);
            small.Begin(1);
            small.Update(chart);
        });
    }
}
