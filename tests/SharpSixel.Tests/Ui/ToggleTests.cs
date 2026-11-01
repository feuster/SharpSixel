using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class ToggleTests
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);

    [Fact]
    public void CheckBox_Ascii_ShowsStateAndFocus()
    {
        var box = new CheckBox("Save");
        Assert.Equal("  [ ] Save", box.Render(Plain));
        box.Checked = true;
        Assert.Equal("  [x] Save", box.Render(Plain));
        box.Focused = true;
        Assert.Equal("> [x] Save", box.Render(Plain));
        box.Enabled = false;
        Assert.Equal("  [x] Save", box.Render(Plain));
    }

    [Fact]
    public void CheckBox_Ascii_UsesColorsAndCustomMarkers()
    {
        var box = new CheckBox("A") { Checked = true, Focused = true };
        box.Style.AsciiCheckOn = "[+]";
        Assert.Equal("\u001b[38;2;52;152;219m> [+] A\u001b[0m", box.Render(new WidgetEnvironment(false)));
    }

    [Fact]
    public void CheckBox_Measures_BothModes()
    {
        var box = new CheckBox("Save");
        Assert.Equal(1, box.GetRows(Plain));
        Assert.Equal(10, box.GetColumns(Plain));
        Assert.Equal(7, box.GetColumns(SixelEnv));
    }

    [Fact]
    public void CheckBox_Sixel_DrawsMarkerGraphicAndText()
    {
        string frame = new CheckBox("Save") { Checked = true }.Render(SixelEnv);
        Assert.Contains("\u001bP", frame, StringComparison.Ordinal);
        Assert.Contains("\u001b[3CSave", frame, StringComparison.Ordinal);
    }

    [Fact]
    public void CheckBox_Contains_HitsLabelAndMarker()
    {
        var box = new CheckBox("Save");
        Assert.True(box.Contains(SixelEnv, 0, 0));
        Assert.True(box.Contains(SixelEnv, 6, 0));
        Assert.False(box.Contains(SixelEnv, 7, 0));
        Assert.False(box.Contains(SixelEnv, 0, 1));
        Assert.False(box.Contains(SixelEnv, -1, 0));
    }

    [Fact]
    public void RadioGroup_Ascii_ShowsSelectionAndFocus()
    {
        var group = new RadioGroup { SelectedIndex = 1, FocusedIndex = 0 };
        group.AddOption("A");
        group.AddOption("B");
        Assert.Equal("> ( ) A\n  (*) B", group.Render(Plain));
        Assert.Equal(2, group.GetRows(Plain));
        Assert.Equal(2, group.GetRows(SixelEnv));
        Assert.Equal(7, group.GetColumns(Plain));
        Assert.Equal(4, group.GetColumns(SixelEnv));
    }

    [Fact]
    public void RadioGroup_IndexOutOfRange_SelectsNothing()
    {
        var group = new RadioGroup { SelectedIndex = 5 };
        group.AddOption("A");
        Assert.Equal("  ( ) A", group.Render(Plain));
    }

    [Fact]
    public void RadioGroup_Empty_StillHasOneRow()
    {
        var group = new RadioGroup();
        Assert.Equal(1, group.GetRows(Plain));
        Assert.Equal(string.Empty, group.Render(Plain));
    }

    [Fact]
    public void RadioGroup_Sixel_DrawsGraphicAndAllLabels()
    {
        var group = new RadioGroup { SelectedIndex = 0, FocusedIndex = 0 };
        group.AddOption("One");
        group.AddOption("Two");
        string frame = group.Render(SixelEnv);
        Assert.Contains("\u001bP", frame, StringComparison.Ordinal);
        Assert.Contains("One", frame, StringComparison.Ordinal);
        Assert.Contains("\u001b[1B\u001b[3CTwo", frame, StringComparison.Ordinal);
    }

    [Fact]
    public void RadioGroup_IndexAt_MapsRowsToOptions()
    {
        var group = new RadioGroup();
        group.AddOption("One");
        group.AddOption("Two");
        Assert.Equal(0, group.IndexAt(SixelEnv, 2, 0));
        Assert.Equal(1, group.IndexAt(SixelEnv, 0, 1));
        Assert.Equal(-1, group.IndexAt(SixelEnv, 0, 2));
        Assert.Equal(-1, group.IndexAt(SixelEnv, 6, 0));
        Assert.Equal(-1, group.IndexAt(SixelEnv, -1, 0));
    }

    [Fact]
    public void InvalidValues_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new CheckBox(null!));
        Assert.Throws<ArgumentNullException>(() => new RadioGroup().Options.Add(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RadioGroup().SelectedIndex = -2);
        Assert.Throws<ArgumentOutOfRangeException>(() => new RadioGroup().FocusedIndex = -2);
        Assert.Throws<ArgumentException>(() => new ToggleStyle().AsciiRadioOn = string.Empty);
    }

    [Fact]
    public void VisibleLength_IgnoresAnsiSequences()
    {
        Assert.Equal(3, TextFit.VisibleLength("\u001b[38;2;1;2;3mabc\u001b[0m\u001b[K"));
        Assert.Equal(0, TextFit.VisibleLength(string.Empty));
        Assert.Equal(1, TextFit.VisibleLength("a\u001b[3"));
    }
}
