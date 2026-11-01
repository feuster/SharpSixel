using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class RuleTests
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);
    private static readonly SixelColor Line = new SixelColor(128, 128, 128);

    [Fact]
    public void Ascii_Horizontal_DrawsLineOfGivenWidth()
    {
        var rule = new Rule { Width = 10 };
        Assert.Equal(new string('-', 10), rule.Render(Plain));
        Assert.Equal(1, rule.GetRows(Plain));
        Assert.Equal(10, rule.GetColumns(Plain));
    }

    [Fact]
    public void Ascii_Horizontal_CentersTitle()
    {
        Assert.Equal("--- Hi ---", new Rule { Width = 10, Title = "Hi" }.Render(Plain));
        Assert.Equal("- Long \u2026 -", new Rule { Width = 10, Title = "Long title here" }.Render(Plain));
    }

    [Fact]
    public void Ascii_Horizontal_DropsTitleWithoutRoom() =>
        Assert.Equal("----", new Rule { Width = 4, Title = "Hi" }.Render(Plain));

    [Fact]
    public void Ascii_Horizontal_DefaultWidthIsTerminalWidthOr40()
    {
        Assert.Equal(40, new Rule().GetColumns(Plain));
        Assert.Equal(20, new Rule().GetColumns(new WidgetEnvironment(false, columns: 20)));
        Assert.Equal(20, new Rule { Width = 30 }.GetColumns(new WidgetEnvironment(false, columns: 20)));
    }

    [Fact]
    public void Ascii_Vertical_DrawsOneCharPerRow()
    {
        var rule = new Rule { Orientation = RuleOrientation.Vertical, Height = 3 };
        Assert.Equal("|\n|\n|", rule.Render(Plain));
        Assert.Equal(3, rule.GetRows(Plain));
        Assert.Equal(3, rule.GetRows(SixelEnv));
        Assert.Equal(1, rule.GetColumns(Plain));
        Assert.Equal(1, rule.GetColumns(SixelEnv));
    }

    [Fact]
    public void Ascii_CustomCharactersAndColor_AreUsed()
    {
        var rule = new Rule { Width = 3 };
        rule.Style.AsciiHorizontal = '=';
        rule.Style.Color = new SixelColor(255, 0, 0);
        Assert.Equal("\u001b[38;2;255;0;0m===\u001b[0m", rule.Render(new WidgetEnvironment(false)));
    }

    [Fact]
    public void Sixel_Horizontal_DrawsLineWithGapForTitle()
    {
        var rule = new Rule { Width = 10, Title = "Hi" };
        SixelImage image = rule.CreateImage(SixelEnv);
        Assert.Equal(100, image.Width);
        Assert.Equal(18, image.Height);
        Assert.Equal(Line, image.GetPixel(10, 8));
        Assert.Equal(Line, image.GetPixel(95, 8));
        Assert.Equal(SixelColor.Transparent, image.GetPixel(40, 8));
        Assert.Equal(SixelColor.Transparent, image.GetPixel(10, 0));
        string frame = rule.Render(SixelEnv);
        Assert.Contains("\u001bP", frame, StringComparison.Ordinal);
        Assert.Contains("\u001b[10X", frame, StringComparison.Ordinal);
        Assert.Contains("\u001b[3C\u001b[38;2;52;152;219m Hi \u001b[0m", frame, StringComparison.Ordinal);
    }

    [Fact]
    public void Sixel_Vertical_DrawsCenteredLine()
    {
        var rule = new Rule { Orientation = RuleOrientation.Vertical, Height = 2 };
        rule.Style.LineThickness = 2;
        SixelImage image = rule.CreateImage(SixelEnv);
        Assert.Equal(10, image.Width);
        Assert.Equal(36, image.Height);
        Assert.Equal(Line, image.GetPixel(4, 0));
        Assert.Equal(Line, image.GetPixel(5, 35));
        Assert.Equal(SixelColor.Transparent, image.GetPixel(2, 10));
        string frame = rule.Render(SixelEnv);
        Assert.Contains("\u001bP", frame, StringComparison.Ordinal);
        Assert.Contains("\u001b[1B\u001b[1X", frame, StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidValues_Throw()
    {
        var rule = new Rule();
        Assert.Throws<ArgumentOutOfRangeException>(() => rule.Width = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => rule.Height = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => rule.Style.LineThickness = 5);
        Assert.Throws<ArgumentNullException>(() => new Rule(null!));
    }
}
