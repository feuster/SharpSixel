using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class WideTextTests
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);

    private static int[] Widths(string frame)
    {
        string[] lines = frame.Split('\n');
        var widths = new int[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            widths[i] = TextFit.VisibleLength(lines[i]);
        }

        return widths;
    }

    [Fact]
    public void Button_CentersWideLabelByCells()
    {
        var button = new Button("確認");
        Assert.Equal(8, button.Columns);
        Assert.Equal("[ 確認 ]", button.Render(Plain));
        Assert.Equal(8, TextFit.VisibleLength(new Button("確認") { State = ButtonState.Focused }.Render(Plain)));
        Assert.Contains("\u001b[2C", button.Render(SixelEnv), StringComparison.Ordinal);
    }

    [Fact]
    public void Panel_AlignsFrameAroundWideLines()
    {
        var panel = new Panel { Title = "設定" };
        panel.AddLine("日本語");
        panel.AddLine("ab");
        string[] lines = panel.Render(Plain).Split('\n');
        Assert.Equal("| 日本語 |", lines[1]);
        Assert.Equal("| ab     |", lines[2]);
        Assert.All(Widths(panel.Render(Plain)), width => Assert.Equal(10, width));
    }

    [Fact]
    public void Table_KeepsAllLinesEquallyWide()
    {
        var table = new Table();
        table.AddColumn("名前");
        table.AddColumn("Age");
        table.AddRow("山田", "42");
        table.AddRow("Bob", "7");
        int[] widths = Widths(table.Render(Plain));
        Assert.All(widths, width => Assert.Equal(widths[0], width));
    }

    [Fact]
    public void BarChart_KeepsLabelColumnAligned()
    {
        var chart = new BarChart();
        chart.Add("日本語", 5);
        chart.Add("ab", 3);
        int[] widths = Widths(chart.Render(Plain));
        Assert.Equal(widths[0], widths[1]);
    }

    [Fact]
    public void Rule_CentersWideTitle()
    {
        var rule = new Rule { Width = 20, Title = "設定" };
        Assert.Equal(20, TextFit.VisibleLength(rule.Render(Plain)));
        Assert.Equal("------- 設定 -------", rule.Render(Plain));
    }

    [Fact]
    public void CheckBox_MeasuresWideLabel()
    {
        var box = new CheckBox("確認");
        Assert.Equal(10, box.GetColumns(Plain));
        Assert.Equal(7, box.GetColumns(SixelEnv));
    }

    [Fact]
    public void Layout_PlacesNextChildAfterWideContent()
    {
        var layout = new HorizontalLayout();
        layout.Add(new Button("確認"));
        layout.Add(new Button("OK"));
        Assert.Equal(8 + 2 + 6, layout.GetColumns(Plain));
        Assert.Equal("[ 確認 ]  [ OK ]", layout.Render(Plain));
        Assert.Contains("\u001b8\u001b[10C\u001b7", layout.Render(SixelEnv), StringComparison.Ordinal);
    }
}