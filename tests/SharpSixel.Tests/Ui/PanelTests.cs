using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class PanelTests
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment AnsiAscii = new WidgetEnvironment(false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);
    private static readonly SixelColor Grid = new SixelColor(128, 128, 128);

    private static Panel Sample(string? title = "Info")
    {
        var panel = new Panel { Title = title };
        panel.AddLine("Hello");
        panel.AddLine("World!");
        return panel;
    }

    [Fact]
    public void Ascii_DrawsFrameWithTitleInTopEdge()
    {
        string expected = string.Join("\n", "+- Info -+", "| Hello  |", "| World! |", "+--------+");
        Assert.Equal(expected, Sample().Render(Plain));
    }

    [Fact]
    public void Ascii_NoTitle_DrawsPlainTopEdge()
    {
        string[] lines = Sample(null).Render(Plain).Split('\n');
        Assert.Equal("+--------+", lines[0]);
        Assert.Equal(4, lines.Length);
    }

    [Fact]
    public void Ascii_LongTitle_WidensPanel()
    {
        Panel panel = Sample("A long title");
        string[] lines = panel.Render(Plain).Split('\n');
        Assert.Equal("+- A long title -+", lines[0]);
        Assert.Equal("| Hello          |", lines[1]);
        Assert.Equal("+----------------+", lines[3]);
    }

    [Fact]
    public void Ascii_FixedWidth_CutsLinesAndTitleWithEllipsis()
    {
        var panel = new Panel { Title = "Info", Width = 5 };
        panel.AddLine("abcdefgh");
        panel.AddLine("ab");
        string expected = string.Join("\n", "+- In\u2026 -+", "| abcd\u2026 |", "| ab    |", "+-------+");
        Assert.Equal(expected, panel.Render(Plain));
    }

    [Fact]
    public void Ascii_TitleIsDropped_WhenNoSpaceForIt()
    {
        var panel = new Panel { Title = "Info", Width = 1 };
        panel.AddLine("x");
        Assert.Equal("+---+\n| x |\n+---+", panel.Render(Plain));
    }

    [Fact]
    public void Ascii_PaddingAndCustomCharacters_AreUsed()
    {
        Panel panel = Sample();
        panel.Style.Padding = 0;
        panel.Style.AsciiHorizontal = '=';
        panel.Style.AsciiVertical = '!';
        panel.Style.AsciiCorner = 'o';
        string expected = string.Join("\n", "o= Info =o", "!Hello   !", "!World!  !", "o========o");
        Assert.Equal(expected, panel.Render(Plain));
    }

    [Fact]
    public void ControlCharacters_BecomeSpaces()
    {
        var panel = new Panel();
        panel.AddLine("a\tb\nc");
        Assert.Equal("+-------+\n| a b c |\n+-------+", panel.Render(Plain));
    }

    [Fact]
    public void Ascii_WithAnsi_ColorsFrameAndTitle()
    {
        string frame = Sample().Render(AnsiAscii);
        Assert.Contains("\u001b[38;2;128;128;128m+-\u001b[0m\u001b[38;2;52;152;219m Info \u001b[0m", frame);
        Assert.Contains("\u001b[38;2;128;128;128m|\u001b[0m", frame);
        Assert.DoesNotContain('\u001b', Sample().Render(Plain));

        Panel plainColors = Sample();
        plainColors.Style.BorderColor = SixelColor.Transparent;
        plainColors.Style.TitleColor = SixelColor.Transparent;
        Assert.DoesNotContain('\u001b', plainColors.Render(AnsiAscii));
    }

    [Fact]
    public void GetRows_IsLinesPlusTwoEdges()
    {
        Panel panel = Sample();
        Assert.Equal(4, panel.GetRows(Plain));
        Assert.Equal(4, panel.GetRows(SixelEnv));
        panel.AddLine("more");
        Assert.Equal(5, panel.GetRows(Plain));
        Assert.Equal(2, new Panel().GetRows(SixelEnv));
    }

    [Fact]
    public void Empty_DrawsOnlyTheEdges()
    {
        Assert.Equal("+--+\n+--+", new Panel().Render(Plain));
    }

    [Fact]
    public void ChangedLines_AreDrawnWithNextFrame()
    {
        Panel panel = Sample();
        panel.Lines[0] = "Bye";
        Assert.Contains("| Bye    |", panel.Render(Plain));
    }

    private static void Fails<T>(Action action)
        where T : Exception => Assert.Throws<T>(action);

    [Fact]
    public void InvalidValues_Throw()
    {
        Fails<ArgumentNullException>(() => new Panel(null!));
        Fails<ArgumentNullException>(() => new Panel().Render(null!));
        Fails<ArgumentNullException>(() => new Panel().AddLine(null!));
        Fails<ArgumentNullException>(() => new Panel().Lines.Add(null!));
        Fails<ArgumentOutOfRangeException>(() => new Panel().Width = 0);
        Fails<ArgumentOutOfRangeException>(() => new PanelStyle().Padding = -1);
        Fails<ArgumentOutOfRangeException>(() => new PanelStyle().LineThickness = 0);
        Fails<ArgumentOutOfRangeException>(() => new PanelStyle().LineThickness = 5);
        Fails<ArgumentOutOfRangeException>(() => new PanelStyle().CornerRadius = -1);
    }

    [Fact]
    public void Image_HasCellBasedSize()
    {
        SixelImage image = Sample().CreateImage(SixelEnv);
        Assert.Equal(100, image.Width);
        Assert.Equal(78, image.Height);
    }

    [Fact]
    public void Image_DrawsRoundedOutline()
    {
        SixelImage image = Sample(null).CreateImage(SixelEnv);
        Assert.Equal(Grid, image.GetPixel(5, 40));
        Assert.Equal(Grid, image.GetPixel(95, 40));
        Assert.Equal(Grid, image.GetPixel(50, 10));
        Assert.Equal(Grid, image.GetPixel(50, 68));
        Assert.Equal(0, image.GetPixel(50, 40).A);
        Assert.Equal(0, image.GetPixel(2, 40).A);
        Assert.Equal(0, image.GetPixel(5, 10).A);
    }

    [Fact]
    public void Image_Title_LeavesGapInTopEdge()
    {
        SixelImage image = Sample().CreateImage(SixelEnv);
        Assert.Equal(0, image.GetPixel(50, 10).A);
        Assert.Equal(Grid, image.GetPixel(10, 10));
        Assert.Equal(Grid, image.GetPixel(85, 10));
        Assert.Equal(Grid, image.GetPixel(50, 68));
    }

    [Fact]
    public void Image_BackgroundFillsInsideOnly()
    {
        Panel panel = Sample();
        var fill = new SixelColor(1, 2, 3);
        panel.Style.BackgroundColor = fill;
        SixelImage image = panel.CreateImage(SixelEnv);
        Assert.Equal(fill, image.GetPixel(50, 40));
        Assert.Equal(Grid, image.GetPixel(5, 40));
        Assert.Equal(0, image.GetPixel(2, 40).A);
    }

    [Fact]
    public void Image_LineThicknessAndTransparentColor()
    {
        Panel panel = Sample(null);
        panel.Style.LineThickness = 3;
        SixelImage thick = panel.CreateImage(SixelEnv);
        Assert.Equal(Grid, thick.GetPixel(50, 9));
        Assert.Equal(Grid, thick.GetPixel(50, 11));

        panel.Style.BorderColor = SixelColor.Transparent;
        Assert.Equal(0, panel.CreateImage(SixelEnv).GetPixel(50, 10).A);
    }

    [Fact]
    public void SixelFrame_ErasesDrawsGraphicAndPlacesTexts()
    {
        string frame = Sample().Render(SixelEnv);
        Assert.StartsWith("\u001b7\u001b[10X\u001b[10C\u001b[K\u001b8", frame);
        Assert.EndsWith("\u001b8", frame);
        Assert.Contains("\u001b[3B\u001b[10X\u001b[10C\u001b[K\u001b8", frame);
        Assert.Contains("\u001bP", frame);
        Assert.Contains("\u001b[2C\u001b[38;2;52;152;219m Info \u001b[0m\u001b8", frame);
        Assert.Contains("\u001b[1B\u001b[2CHello \u001b8", frame);
        Assert.Contains("\u001b[2B\u001b[2CWorld!\u001b8", frame);
    }

    [Fact]
    public void SixelFrame_BackgroundPaintsTextCells()
    {
        Panel panel = Sample();
        panel.Style.BackgroundColor = new SixelColor(1, 2, 3);
        string frame = panel.Render(SixelEnv);
        Assert.Contains("\u001b[1B\u001b[2C\u001b[48;2;1;2;3mHello \u001b[0m\u001b8", frame);
    }

    [Fact]
    public void SixelFrame_EmptyPanel_HasNoTexts()
    {
        string frame = new Panel().Render(SixelEnv);
        Assert.Contains("\u001bP", frame);
        Assert.EndsWith("\u001b8", frame);
        Assert.DoesNotContain("\u001b[1B\u001b[2C", frame, StringComparison.Ordinal);
    }

    [Fact]
    public void LiveRegion_PlacesEachAsciiLineOnItsRow()
    {
        var output = new StringWriter();
        var region = new LiveRegion(output, Plain);
        Panel panel = Sample();
        region.Begin(panel.GetRows(Plain));
        region.Update(panel);
        string[] lines = panel.Render(Plain).Split('\n');
        string text = output.ToString();
        Assert.Contains("\u001b8" + lines[0] + "\u001b[K", text);
        Assert.Contains("\u001b8\u001b[1B" + lines[1] + "\u001b[K", text);
        Assert.Throws<ArgumentException>(() =>
        {
            var small = new LiveRegion(new StringWriter(), Plain);
            small.Begin(1);
            small.Update(panel);
        });
    }
}
