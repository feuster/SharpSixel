using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class VerticalLayoutTests
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);

    [Fact]
    public void Ascii_StacksChildrenWithSpacing()
    {
        var group = new RadioGroup();
        group.AddOption("x");
        group.AddOption("y");
        var layout = new VerticalLayout { Spacing = 1 };
        layout.Add(new CheckBox("A"));
        layout.Add(group);
        Assert.Equal("  [ ] A\n\n  ( ) x\n  ( ) y", layout.Render(Plain));
        Assert.Equal(4, layout.GetRows(Plain));
        Assert.Equal(4, layout.GetRows(SixelEnv));
        Assert.Equal(7, layout.GetColumns(Plain));
    }

    [Fact]
    public void Rule_FillsTheWidthOfTheOtherChildren()
    {
        var layout = new VerticalLayout();
        layout.Add(new CheckBox("Save"));
        layout.Add(new Rule());
        Assert.Equal("  [ ] Save\n" + new string('-', 10), layout.Render(Plain));
        Assert.Equal(10, layout.GetColumns(Plain));
        Assert.Equal(7, layout.GetColumns(SixelEnv));

        var alone = new VerticalLayout();
        alone.Add(new Rule());
        Assert.Equal(40, alone.GetColumns(Plain));
    }

    [Fact]
    public void Rule_WithOwnWidthKeepsIt()
    {
        var layout = new VerticalLayout();
        layout.Add(new CheckBox("Save"));
        layout.Add(new Rule { Width = 4 });
        Assert.Equal("  [ ] Save\n----", layout.Render(Plain));
    }

    [Fact]
    public void Sixel_MovesEachChildFrameDownAndRestoresOrigin()
    {
        var layout = new VerticalLayout();
        layout.Add(new Button("OK"));
        layout.Add(new Button("Cancel"));
        string frame = layout.Render(SixelEnv);
        Assert.StartsWith("\u001b7", frame, StringComparison.Ordinal);
        Assert.Contains("\u001b8\u001b[1B\u001b7", frame, StringComparison.Ordinal);
        Assert.EndsWith("\u001b8\u001b[1A\u001b7", frame, StringComparison.Ordinal);
    }

    [Fact]
    public void AsColumnInHorizontalLayout_NextToStretchedRule()
    {
        var column = new VerticalLayout();
        column.Add(new Button("OK"));
        column.Add(new Button("Cancel"));
        var layout = new HorizontalLayout();
        layout.Add(column);
        layout.Add(new Rule { Orientation = RuleOrientation.Vertical });
        Assert.Equal("[ OK ]      |\n[ Cancel ]  |", layout.Render(Plain));
        string frame = layout.Render(SixelEnv);
        Assert.Contains("\u001b8\u001b[1B\u001b7", frame, StringComparison.Ordinal);
        Assert.Contains("\u001b[12C", frame, StringComparison.Ordinal);
        Assert.EndsWith("\u001b8\u001b[12D\u001b7", frame, StringComparison.Ordinal);
    }

    [Fact]
    public void VerticalRuleWithoutHeight_DoesNotStretchInsideStack()
    {
        var layout = new VerticalLayout();
        layout.Add(new Rule { Orientation = RuleOrientation.Vertical });
        layout.Add(new CheckBox("A"));
        Assert.Equal(2, layout.GetRows(Plain));
        Assert.Equal("|\n  [ ] A", layout.Render(Plain));
    }

    [Fact]
    public void Empty_RendersNothing()
    {
        var layout = new VerticalLayout();
        Assert.Equal(string.Empty, layout.Render(Plain));
        Assert.Equal(0, layout.GetColumns(Plain));
        Assert.Equal(1, layout.GetRows(Plain));
    }

    [Fact]
    public void InvalidChildren_Throw()
    {
        var layout = new VerticalLayout();
        Assert.Throws<ArgumentNullException>(() => layout.Add(null!));
        Assert.Throws<ArgumentException>(() => layout.Add(layout));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.Spacing = -1);
    }
}