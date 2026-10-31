using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class ColumnLimitTests
{
    private static WidgetEnvironment Ascii(int columns) => new WidgetEnvironment(false, useAnsi: false, columns: columns);

    private static int LongestLine(string frame) => frame.Split('\n').Max(line => line.Length);

    [Fact]
    public void Columns_DefaultIsUnknown()
    {
        Assert.Equal(0, new WidgetEnvironment(false).Columns);
        Assert.Equal(0, WidgetEnvironment.Create(RenderMode.Ascii, null).Columns);
    }

    [Fact]
    public void Columns_NegativeIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WidgetEnvironment(false, columns: -1));
    }

    [Fact]
    public void Create_ExplicitColumnsWin()
    {
        var info = new SixelTerminalInfo(true, 800, 400, 10, 20);

        Assert.Equal(100, WidgetEnvironment.Create(RenderMode.Auto, info, columns: 100).Columns);
    }

    [Fact]
    public void Create_DerivesColumnsFromTextAreaAndCellSize()
    {
        var info = new SixelTerminalInfo(true, 800, 400, 10, 20);

        Assert.Equal(80, WidgetEnvironment.Create(RenderMode.Auto, info).Columns);
    }

    [Fact]
    public void Create_WithoutTextAreaSize_LeavesColumnsUnknown()
    {
        var info = new SixelTerminalInfo(true, 0, 0, 10, 20);

        Assert.Equal(0, WidgetEnvironment.Create(RenderMode.Auto, info).Columns);
    }

    [Fact]
    public void Table_ShrinksWidestColumnToFit()
    {
        var table = new Table();
        table.AddColumn("Name");
        table.AddColumn("Description");
        table.AddRow("a", new string('x', 60));

        string unlimited = table.Render(Ascii(0));
        string limited = table.Render(Ascii(30));

        Assert.True(LongestLine(unlimited) > 30);
        Assert.True(LongestLine(limited) <= 30);
        Assert.Contains("\u2026", limited, StringComparison.Ordinal);
    }

    [Fact]
    public void Table_RowCountDoesNotDependOnColumns()
    {
        var table = new Table();
        table.AddColumn("A");
        table.AddRow(new string('x', 60));

        Assert.Equal(table.GetRows(Ascii(0)), table.GetRows(Ascii(20)));
    }

    [Fact]
    public void Panel_CutsLongLines()
    {
        var panel = new Panel { Title = "Title" };
        panel.AddLine(new string('x', 60));

        string limited = panel.Render(Ascii(25));

        Assert.True(LongestLine(limited) <= 25);
        Assert.True(LongestLine(panel.Render(Ascii(0))) > 25);
    }

    [Fact]
    public void Panel_FixedWidthIsLimitedToo()
    {
        var panel = new Panel { Width = 50 };
        panel.AddLine("text");

        Assert.True(LongestLine(panel.Render(Ascii(20))) <= 20);
    }

    [Fact]
    public void Tree_CutsLongNodeText()
    {
        var tree = new Tree { Title = "Root" };
        TreeNode node = tree.AddNode("parent");
        node.AddNode(new string('y', 60));

        string limited = tree.Render(Ascii(30));

        Assert.True(LongestLine(limited) <= 30);
        Assert.True(LongestLine(tree.Render(Ascii(0))) > 30);
    }

    [Fact]
    public void BarChart_ShrinksBarsSoLinesFit()
    {
        var chart = new BarChart();
        chart.Add("alpha", 5);
        chart.Add("beta", 10);

        string limited = chart.Render(Ascii(25));

        Assert.True(LongestLine(limited) <= 25);
        Assert.True(LongestLine(chart.Render(Ascii(0))) > 25);
    }

    [Fact]
    public void ProgressBar_ShrinksBarSoLineFits()
    {
        var bar = new ProgressBar { Caption = "Download", Value = 50 };

        string limited = bar.Render(Ascii(30));

        Assert.True(LongestLine(limited) <= 30);
        Assert.True(LongestLine(bar.Render(Ascii(0))) > 30);
    }

    [Fact]
    public void ProgressBar_NeverShrinksBelowMinimum()
    {
        var bar = new ProgressBar { Value = 50 };

        string limited = bar.Render(Ascii(5));

        Assert.StartsWith("[", limited, StringComparison.Ordinal);
        Assert.Contains("]", limited, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ProgressTextMode.Percent)]
    [InlineData(ProgressTextMode.Fraction)]
    public void ProgressBar_VeryNarrowTerminal_CutsCaptionAndTextSoLineNeverWraps(ProgressTextMode mode)
    {
        var bar = new ProgressBar { Caption = "Upload of a long caption", Value = 50, ProgressText = mode };

        for (int columns = 4; columns <= 30; columns++)
        {
            string frame = bar.Render(Ascii(columns));

            Assert.True(LongestLine(frame) <= columns, $"columns={columns}: '{frame}'");
        }
    }

    [Fact]
    public void BarChart_VeryNarrowTerminal_CutsCaptionLabelsAndValuesSoLineNeverWraps()
    {
        var chart = new BarChart { Caption = "Umsatz pro Quartal im Jahr" };
        chart.Add("Apples and oranges", 4);
        chart.Add("Cherries", 123456);

        for (int columns = 3; columns <= 40; columns++)
        {
            string frame = chart.Render(Ascii(columns));

            Assert.True(LongestLine(frame) <= columns, $"columns={columns}: '{frame}'");
        }
    }

    [Fact]
    public void BarChart_Sixel_ClearsValueColumnInEveryBarRow()
    {
        var chart = new BarChart();
        chart.Add("A", 4);
        chart.Add("B", 2);

        string frame = chart.Render(new WidgetEnvironment(true, 10, 20, columns: 60));

        int expected = chart.Items.Count * chart.Style.BarHeightRows;
        Assert.Equal(expected, frame.Split("\u001b[2X").Length - 1);
    }
}
