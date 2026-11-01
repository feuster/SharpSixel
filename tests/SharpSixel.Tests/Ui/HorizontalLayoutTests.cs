using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class HorizontalLayoutTests
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);

    [Fact]
    public void Ascii_PlacesSingleRowWidgetsSideBySide()
    {
        var layout = new HorizontalLayout();
        layout.Add(new Button("OK"));
        layout.Add(new CheckBox("Hi"));
        Assert.Equal("[ OK ]    [ ] Hi", layout.Render(Plain));
        Assert.Equal(16, layout.GetColumns(Plain));
        Assert.Equal(1, layout.GetRows(Plain));
    }

    [Fact]
    public void Ascii_MultiRow_PadsShorterChildrenAndKeepsTopAlignment()
    {
        var panel = new Panel();
        panel.AddLine("ab");
        var group = new RadioGroup();
        group.AddOption("x");
        group.AddOption("y");
        var layout = new HorizontalLayout { Spacing = 1 };
        layout.Add(panel);
        layout.Add(group);
        layout.Add(new Rule { Orientation = RuleOrientation.Vertical, Height = 3 });

        string expected = string.Join(
            "\n",
            "+----+   ( ) x |",
            "| ab |   ( ) y |",
            "+----+         |");
        Assert.Equal(expected, layout.Render(Plain));
        Assert.Equal(3, layout.GetRows(Plain));
        Assert.Equal(3, layout.GetRows(SixelEnv));
    }

    [Fact]
    public void Ascii_Ansi_PadsByVisibleWidthAndErasesRest()
    {
        var bar = new ProgressBar { Value = 50 };
        var layout = new HorizontalLayout();
        layout.Add(new Rule { Width = 3 });
        layout.Add(bar);
        string line = layout.Render(new WidgetEnvironment(false, useAnsi: true));
        Assert.StartsWith("\u001b[38;2;128;128;128m---\u001b[0m  ", line, StringComparison.Ordinal);
        Assert.EndsWith("\u001b[K", line, StringComparison.Ordinal);
        Assert.Equal(3 + 2 + bar.GetColumns(Plain), layout.GetColumns(Plain));
    }

    [Fact]
    public void Ascii_ChildGetsRemainingTerminalWidth()
    {
        var layout = new HorizontalLayout();
        layout.Add(new Button("OK"));
        layout.Add(new Rule());
        Assert.Equal("[ OK ]  " + new string('-', 12), layout.Render(new WidgetEnvironment(false, useAnsi: false, columns: 20)));
    }

    [Fact]
    public void Sixel_ShiftsEachChildFrameToItsColumnAndRestoresOrigin()
    {
        var layout = new HorizontalLayout();
        layout.Add(new Button("OK"));
        layout.Add(new Button("Cancel"));
        string frame = layout.Render(SixelEnv);
        Assert.StartsWith("\u001b7", frame, StringComparison.Ordinal);
        Assert.Contains("\u001b8\u001b[8C\u001b7", frame, StringComparison.Ordinal);
        Assert.EndsWith("\u001b8\u001b[8D\u001b7", frame, StringComparison.Ordinal);
        Assert.Equal(8 + 10, layout.GetColumns(SixelEnv));
    }

    [Fact]
    public void Sixel_SingleChild_NeedsNoMoves()
    {
        var layout = new HorizontalLayout();
        layout.Add(new Button("OK"));
        Assert.Equal(new Button("OK").Render(SixelEnv) + "\u001b7", layout.Render(SixelEnv));
    }

    [Fact]
    public void Sixel_MeasuresWidgetsWithDifferentSixelWidth()
    {
        var clock = new Clock();
        Assert.Equal(clock.ImageColumns(SixelEnv) + 1 + "00:00:00".Length, clock.GetColumns(SixelEnv));
        Assert.Equal("00:00:00".Length, clock.GetColumns(Plain));
        var spinner = new Spinner { Caption = "Go" };
        Assert.Equal(spinner.ImageColumns(SixelEnv) + 3, spinner.GetColumns(SixelEnv));
        var date = new DateDisplay { Date = new DateTime(2026, 10, 28) };
        Assert.Equal(date.ImageColumns(SixelEnv) + 1 + date.Style.Format(date.Date).Length, date.GetColumns(SixelEnv));
    }

    [Fact]
    public void Layouts_CanBeNested()
    {
        var inner = new HorizontalLayout { Spacing = 1 };
        inner.Add(new Rule { Width = 2 });
        inner.Add(new Rule { Width = 2 });
        var outer = new HorizontalLayout { Spacing = 1 };
        outer.Add(inner);
        outer.Add(new Rule { Width = 1 });
        Assert.Equal("-- -- -", outer.Render(Plain));
        Assert.Equal(7, outer.GetColumns(Plain));
    }

    [Fact]
    public void FixedWidth_KeepsColumnAndOffersTheWidth()
    {
        var layout = new HorizontalLayout();
        layout.Add(new CheckBox("Hi"), 12);
        layout.Add(new Button("OK"));
        Assert.Equal("  [ ] Hi" + new string(' ', 6) + "[ OK ]", layout.Render(Plain));
        Assert.Equal(20, layout.GetColumns(Plain));
        Assert.Contains("\u001b8\u001b[14C\u001b7", layout.Render(SixelEnv), StringComparison.Ordinal);

        var filled = new HorizontalLayout();
        filled.Add(new Rule(), 5);
        filled.Add(new Button("OK"));
        Assert.Equal("-----  [ OK ]", filled.Render(Plain));
    }

    [Fact]
    public void FixedWidth_IsForgottenWhenChildIsRemoved()
    {
        var box = new CheckBox("Hi");
        var layout = new HorizontalLayout();
        layout.Add(box, 12);
        layout.Children.Clear();
        layout.Add(box);
        Assert.Equal(8, layout.GetColumns(Plain));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.Add(box, 0));
    }

    [Fact]
    public void VerticalRuleWithoutHeight_StretchesToLayoutRows()
    {
        var panel = new Panel();
        panel.AddLine("ab");
        var layout = new HorizontalLayout();
        layout.Add(panel);
        layout.Add(new Rule { Orientation = RuleOrientation.Vertical });
        Assert.Equal("+----+  |\n| ab |  |\n+----+  |", layout.Render(Plain));
        Assert.Equal(3, layout.GetRows(SixelEnv));
        Assert.Contains("\u001b[2B\u001b[1X", layout.Render(SixelEnv), StringComparison.Ordinal);
        Assert.Equal(1, new Rule { Orientation = RuleOrientation.Vertical }.GetRows(Plain));
    }

    [Fact]
    public void Empty_RendersNothing()
    {
        var layout = new HorizontalLayout();
        Assert.Equal(string.Empty, layout.Render(Plain));
        Assert.Equal(0, layout.GetColumns(Plain));
        Assert.Equal(1, layout.GetRows(Plain));
    }

    [Fact]
    public void InvalidChildren_Throw()
    {
        var layout = new HorizontalLayout();
        Assert.Throws<ArgumentNullException>(() => layout.Add(null!));
        Assert.Throws<ArgumentException>(() => layout.Add(layout));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.Spacing = -1);
    }
}
