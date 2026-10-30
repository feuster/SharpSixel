using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class TableTests
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment AnsiAscii = new WidgetEnvironment(false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);
    private static readonly SixelColor Grid = new SixelColor(128, 128, 128);

    private static Table Sample(string? caption = null)
    {
        var table = new Table { Caption = caption };
        table.AddColumn("Name");
        table.AddColumn("Qty", TableAlignment.Right);
        table.AddRow("Apples", "42");
        table.AddRow("Pears", "7");
        return table;
    }

    [Fact]
    public void Ascii_DrawsFramedTableWithHeaderRule()
    {
        string expected = string.Join(
            "\n",
            "+--------+-----+",
            "| Name   | Qty |",
            "+--------+-----+",
            "| Apples |  42 |",
            "| Pears  |   7 |",
            "+--------+-----+");
        Assert.Equal(expected, Sample().Render(Plain));
    }

    [Fact]
    public void Ascii_Caption_IsFirstLine()
    {
        string[] lines = Sample("Title").Render(Plain).Split('\n');
        Assert.Equal(7, lines.Length);
        Assert.Equal("Title", lines[0]);
    }

    [Fact]
    public void Ascii_NoBorder_DrawsOnlyHeaderRuleAndSeparators()
    {
        Table table = Sample();
        table.Style.ShowBorder = false;
        string expected = string.Join("\n", " Name   | Qty ", "--------+-----", " Apples |  42 ", " Pears  |   7 ");
        Assert.Equal(expected, table.Render(Plain));
    }

    [Fact]
    public void Ascii_NoHeader_SkipsHeaderAndItsRule()
    {
        Table table = Sample();
        table.Style.ShowHeader = false;
        string[] lines = table.Render(Plain).Split('\n');
        Assert.Equal(4, lines.Length);
        Assert.DoesNotContain("Name", lines[1]);
        Assert.Equal("| Apples | 42 |", lines[1]);
    }

    [Fact]
    public void Ascii_RowSeparators_AddRulesBetweenRows()
    {
        Table table = Sample();
        table.Style.ShowRowSeparators = true;
        string[] lines = table.Render(Plain).Split('\n');
        Assert.Equal(7, lines.Length);
        Assert.Equal("+--------+-----+", lines[4]);
        Assert.Equal("| Pears  |   7 |", lines[5]);
    }

    [Fact]
    public void Ascii_NoColumnSeparators_RemovesInnerLines()
    {
        Table table = Sample();
        table.Style.ShowColumnSeparators = false;
        string[] lines = table.Render(Plain).Split('\n');
        Assert.Equal("+-------------+", lines[0]);
        Assert.Equal("| Name    Qty |", lines[1]);
        Assert.Equal("| Apples   42 |", lines[3]);
    }

    [Fact]
    public void Alignment_LeftRightCenter()
    {
        var table = new Table();
        table.Style.ShowBorder = false;
        table.Style.ShowHeader = false;
        table.Style.Padding = 0;
        table.AddColumn("c", TableAlignment.Center, 7);
        table.AddRow("ab");
        Assert.Equal("  ab   ", table.Render(Plain));
        table.Columns[0].Alignment = TableAlignment.Right;
        Assert.Equal("     ab", table.Render(Plain));
        table.Columns[0].Alignment = TableAlignment.Left;
        Assert.Equal("ab     ", table.Render(Plain));
    }

    [Fact]
    public void FixedWidth_CutsLongTextWithEllipsis()
    {
        var table = new Table();
        table.Style.ShowBorder = false;
        table.Style.ShowHeader = false;
        table.Style.Padding = 0;
        table.AddColumn("c", width: 4);
        table.AddRow("abcdef");
        Assert.Equal("abc\u2026", table.Render(Plain));
        table.Columns[0].Width = 1;
        Assert.Equal("a", table.Render(Plain));
    }

    [Fact]
    public void MissingAndExtraCells_AreHandled_ControlCharactersBecomeSpaces()
    {
        var table = new Table();
        table.AddColumn("A");
        table.AddColumn("B");
        table.AddRow("x");
        table.AddRow("y", "a\tb", "ignored");
        string[] lines = table.Render(Plain).Split('\n');
        Assert.Equal("| x |     |", lines[3]);
        Assert.Equal("| y | a b |", lines[4]);
    }

    [Fact]
    public void Ascii_WithAnsi_ColorsGridAndHeader()
    {
        string frame = Sample().Render(AnsiAscii);
        Assert.Contains("\u001b[38;2;128;128;128m+--------+-----+\u001b[0m", frame);
        Assert.Contains("\u001b[38;2;52;152;219m Name   \u001b[0m", frame);
        Assert.DoesNotContain('\u001b', Sample().Render(Plain));

        Table plainColors = Sample();
        plainColors.Style.BorderColor = SixelColor.Transparent;
        plainColors.Style.HeaderTextColor = SixelColor.Transparent;
        Assert.DoesNotContain('\u001b', plainColors.Render(AnsiAscii));
    }

    [Fact]
    public void Ascii_CustomCharacters_AreUsed()
    {
        Table table = Sample();
        table.Style.AsciiHorizontal = '=';
        table.Style.AsciiVertical = '!';
        table.Style.AsciiCorner = 'o';
        string[] lines = table.Render(Plain).Split('\n');
        Assert.Equal("o========o=====o", lines[0]);
        Assert.Equal("! Name   ! Qty !", lines[1]);
    }

    [Fact]
    public void Padding_WidensCells()
    {
        Table table = Sample();
        table.Style.Padding = 2;
        string[] lines = table.Render(Plain).Split('\n');
        Assert.Equal("+----------+-------+", lines[0]);
        Assert.Equal("|  Apples  |   42  |", lines[3]);
    }

    [Fact]
    public void GetRows_DependsOnCaptionHeaderBorderAndRows()
    {
        Table table = Sample();
        Assert.Equal(6, table.GetRows(Plain));
        Assert.Equal(6, table.GetRows(SixelEnv));
        table.Caption = "T";
        Assert.Equal(7, table.GetRows(SixelEnv));
        table.Style.ShowRowSeparators = true;
        Assert.Equal(8, table.GetRows(Plain));
        table.Style.ShowBorder = false;
        table.Style.ShowHeader = false;
        Assert.Equal(4, table.GetRows(Plain));
    }

    [Fact]
    public void Empty_HasOneRowAndNoContent()
    {
        var table = new Table();
        Assert.Equal(1, table.GetRows(Plain));
        Assert.Equal(1, table.GetRows(SixelEnv));
        Assert.Equal(string.Empty, table.Render(Plain));
        string frame = table.Render(SixelEnv);
        Assert.DoesNotContain("\u001bP", frame);
        Assert.EndsWith("\u001b8", frame);
    }

    [Fact]
    public void ColumnsWithoutRows_ShowOnlyHeader()
    {
        var table = new Table();
        table.AddColumn("A");
        string[] lines = table.Render(Plain).Split('\n');
        Assert.Equal(new[] { "+---+", "| A |", "+---+", "+---+" }, lines);
    }

    private static void Fails<T>(Action action)
        where T : Exception => Assert.Throws<T>(action);

    [Fact]
    public void InvalidValues_Throw()
    {
        Fails<ArgumentNullException>(() => new Table(null!));
        Fails<ArgumentNullException>(() => new Table().Render(null!));
        Fails<ArgumentNullException>(() => new Table().AddColumn(null!));
        Fails<ArgumentNullException>(() => new Table().Columns.Add(null!));
        Fails<ArgumentNullException>(() => new Table().AddRow(null!));
        Fails<ArgumentNullException>(() => new Table().Rows.Add(null!));
        Fails<ArgumentNullException>(() => new TableColumn("x").Header = null!);
        Fails<ArgumentOutOfRangeException>(() => new TableColumn("x", width: 0));
        Fails<ArgumentOutOfRangeException>(() => new TableStyle().Padding = -1);
        Fails<ArgumentOutOfRangeException>(() => new TableStyle().LineThickness = 0);
        Fails<ArgumentOutOfRangeException>(() => new TableStyle().LineThickness = 5);
    }

    [Fact]
    public void Image_HasCellBasedSize()
    {
        SixelImage image = Sample().CreateImage(SixelEnv);
        Assert.Equal(160, image.Width);
        Assert.Equal(120, image.Height);
    }

    [Fact]
    public void Image_DrawsFrameSeparatorAndRules()
    {
        SixelImage image = Sample().CreateImage(SixelEnv);
        Assert.Equal(Grid, image.GetPixel(5, 70));
        Assert.Equal(Grid, image.GetPixel(95, 70));
        Assert.Equal(Grid, image.GetPixel(155, 70));
        Assert.Equal(Grid, image.GetPixel(50, 10));
        Assert.Equal(Grid, image.GetPixel(50, 50));
        Assert.Equal(Grid, image.GetPixel(50, 110));
        Assert.Equal(0, image.GetPixel(50, 30).A);
        Assert.Equal(0, image.GetPixel(5, 5).A);
        Assert.Equal(0, image.GetPixel(30, 70).A);
    }

    [Fact]
    public void Image_LineThicknessAndTransparentColor()
    {
        Table table = Sample();
        table.Style.LineThickness = 3;
        Assert.Equal(Grid, table.CreateImage(SixelEnv).GetPixel(50, 9));
        Assert.Equal(Grid, table.CreateImage(SixelEnv).GetPixel(50, 11));

        table.Style.BorderColor = SixelColor.Transparent;
        Assert.Equal(0, table.CreateImage(SixelEnv).GetPixel(50, 10).A);
    }

    [Fact]
    public void SixelFrame_ErasesDrawsGraphicAndPlacesTexts()
    {
        string frame = Sample().Render(SixelEnv);
        Assert.StartsWith("\u001b7", frame);
        Assert.EndsWith("\u001b8", frame);
        Assert.Contains("\u001b[1B\u001b[16X\u001b[16C\u001b[K\u001b8", frame);
        Assert.Contains("\u001bP", frame);
        Assert.Contains("\u001b[1B\u001b[1C\u001b[38;2;52;152;219m Name   \u001b[0m\u001b8", frame);
        Assert.Contains("\u001b[1B\u001b[10C\u001b[38;2;52;152;219m Qty \u001b[0m\u001b8", frame);
        Assert.Contains("\u001b[3B\u001b[1C Apples \u001b8", frame);
        Assert.Contains("\u001b[4B\u001b[10C   7 \u001b8", frame);
    }

    [Fact]
    public void SixelFrame_HeaderBackgroundAndStripes()
    {
        Table table = Sample();
        table.Style.HeaderBackgroundColor = new SixelColor(10, 20, 30);
        table.Style.StripeColor = new SixelColor(1, 2, 3);
        string frame = table.Render(SixelEnv);
        Assert.Contains("\u001b[38;2;52;152;219m\u001b[48;2;10;20;30m Name   \u001b[0m", frame);
        Assert.Contains("\u001b[48;2;1;2;3m Pears  \u001b[0m", frame);
        Assert.DoesNotContain("\u001b[48;2;1;2;3m Apples ", frame);
    }

    [Fact]
    public void SixelFrame_CaptionShiftsRows()
    {
        string frame = Sample("Title").Render(SixelEnv);
        Assert.StartsWith("\u001b7Title\u001b[K\u001b8", frame);
        Assert.Contains("\u001b[2B\u001b[1C\u001b[38;2;52;152;219m Name   \u001b[0m\u001b8", frame);
        Assert.Contains("\u001b[4B\u001b[1C Apples \u001b8", frame);
    }

    [Fact]
    public void LiveRegion_PlacesEachAsciiLineOnItsRow()
    {
        var output = new StringWriter();
        var region = new LiveRegion(output, Plain);
        Table table = Sample();
        region.Begin(table.GetRows(Plain));
        region.Update(table);
        string[] lines = table.Render(Plain).Split('\n');
        string text = output.ToString();
        Assert.Contains("\u001b8" + lines[0] + "\u001b[K", text);
        Assert.Contains("\u001b8\u001b[1B" + lines[1] + "\u001b[K", text);
        Assert.Throws<ArgumentException>(() =>
        {
            var small = new LiveRegion(new StringWriter(), Plain);
            small.Begin(1);
            small.Update(table);
        });
    }
}
