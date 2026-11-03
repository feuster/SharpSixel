using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class ProgressIndeterminateTests
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);

    private static ProgressBar Bar(ProgressBarStyle? style = null)
    {
        style ??= new ProgressBarStyle();
        style.Width = 12;
        style.AsciiFill = '#';
        style.AsciiEmpty = '-';
        style.IndeterminateSteps = 7;
        return new ProgressBar(style) { IsIndeterminate = true };
    }

    [Theory]
    [InlineData(0, "[###-------]")]
    [InlineData(3, "[---###----]")]
    [InlineData(7, "[-------###]")]
    [InlineData(8, "[------###-]")]
    [InlineData(14, "[###-------]")]
    public void Ascii_MovesSegmentBackAndForth(int frames, string expected)
    {
        ProgressBar bar = Bar();
        for (int i = 0; i < frames; i++)
        {
            bar.Advance();
        }

        Assert.Equal(expected, bar.Render(Plain));
        Assert.Equal(frames, bar.Frame);
    }

    [Fact]
    public void Ascii_IgnoresValueAndShowsNoProgressText()
    {
        ProgressBar bar = Bar();
        bar.Value = 80;
        bar.ProgressText = ProgressTextMode.Percent;
        Assert.Equal("[###-------]", bar.Render(Plain));
        bar.IsIndeterminate = false;
        Assert.Equal("[########--]  80%", bar.Render(Plain));
    }

    [Fact]
    public void SegmentStyle_ChangesSegmentLength()
    {
        ProgressBar bar = Bar();
        bar.Style.IndeterminateSegment = 0.5;
        Assert.Equal("[#####-----]", bar.Render(Plain));
        Assert.Throws<ArgumentOutOfRangeException>(() => bar.Style.IndeterminateSegment = 0.95);
        Assert.Throws<ArgumentOutOfRangeException>(() => bar.Style.IndeterminateSteps = 0);
    }

    [Fact]
    public void Sixel_DrawsMovingSegment()
    {
        ProgressBar bar = Bar();
        SixelImage first = bar.CreateImage(SixelEnv);
        Assert.Equal(bar.Style.FillColor, first.GetPixel(10, 9));
        Assert.Equal(bar.Style.TrackColor, first.GetPixel(100, 9));
        for (int i = 0; i < 7; i++)
        {
            bar.Advance();
        }

        SixelImage last = bar.CreateImage(SixelEnv);
        Assert.Equal(bar.Style.TrackColor, last.GetPixel(10, 9));
        Assert.Equal(bar.Style.FillColor, last.GetPixel(100, 9));
    }

    [Fact]
    public void Group_AlignsCaptionsAndAnimatesOnlyIndeterminateTasks()
    {
        var group = new ProgressGroup();
        group.Style.Width = 12;
        group.Style.AsciiFill = '#';
        group.Style.AsciiEmpty = '-';
        group.Style.IndeterminateSteps = 7;
        ProgressBar download = group.AddTask("A");
        download.Value = 50;
        ProgressBar scan = group.AddTask("Longer");
        scan.IsIndeterminate = true;

        Assert.Equal("A      [#####-----]  50%\nLonger [###-------]", group.Render(Plain));
        Assert.Equal(2, group.GetRows(Plain));
        Assert.Equal(2, group.GetRows(SixelEnv));

        group.Advance();
        Assert.Equal(0, download.Frame);
        Assert.Equal(1, scan.Frame);
        Assert.EndsWith("Longer [-###------]", group.Render(Plain), StringComparison.Ordinal);
    }

    [Fact]
    public void Group_Sixel_StacksBarsAndMeasuresWidth()
    {
        var group = new ProgressGroup();
        group.Style.Width = 12;
        group.AddTask("A");
        group.AddTask("Longer");
        string frame = group.Render(SixelEnv);
        Assert.StartsWith("\u001b7", frame, StringComparison.Ordinal);
        Assert.Contains("\u001b8\u001b[1B\u001b7", frame, StringComparison.Ordinal);
        Assert.EndsWith("\u001b8\u001b[1A\u001b7", frame, StringComparison.Ordinal);
        Assert.Equal(7 + 12 + 5, group.GetColumns(SixelEnv));
    }

    [Fact]
    public void Group_TasksShareTheStyleAndRejectNull()
    {
        var group = new ProgressGroup();
        ProgressBar task = group.AddTask("A", 10);
        Assert.Same(group.Style, task.Style);
        Assert.Equal(10, task.Maximum);
        Assert.Throws<ArgumentNullException>(() => group.Add(null!));
        Assert.Throws<ArgumentNullException>(() => group.Tasks.Add(null!));
        Assert.Throws<ArgumentNullException>(() => new ProgressGroup(null!));
        Assert.Equal(string.Empty, new ProgressGroup().Render(Plain));
        Assert.Equal(1, new ProgressGroup().GetRows(Plain));
    }
}