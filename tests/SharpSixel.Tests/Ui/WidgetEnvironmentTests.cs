using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class WidgetEnvironmentTests
{
    private static SixelTerminalInfo Info(bool sixel, int cw = 0, int ch = 0) => new SixelTerminalInfo(sixel, 0, 0, cw, ch);

    [Fact]
    public void Auto_FollowsSixelSupport()
    {
        Assert.True(WidgetEnvironment.Create(RenderMode.Auto, Info(true)).UseSixel);
        Assert.False(WidgetEnvironment.Create(RenderMode.Auto, Info(false)).UseSixel);
        Assert.False(WidgetEnvironment.Create(RenderMode.Auto, null).UseSixel);
    }

    [Fact]
    public void ForcedModes_IgnoreTerminal()
    {
        Assert.True(WidgetEnvironment.Create(RenderMode.Sixel, Info(false)).UseSixel);
        Assert.False(WidgetEnvironment.Create(RenderMode.Ascii, Info(true)).UseSixel);
    }

    [Fact]
    public void CellSize_ComesFromTerminalOrFallsBack()
    {
        var reported = WidgetEnvironment.Create(RenderMode.Auto, Info(true, 8, 16));
        Assert.Equal(8, reported.CellWidth);
        Assert.Equal(16, reported.CellHeight);

        var fallback = WidgetEnvironment.Create(RenderMode.Auto, Info(true));
        Assert.Equal(WidgetEnvironment.DefaultCellWidth, fallback.CellWidth);
        Assert.Equal(WidgetEnvironment.DefaultCellHeight, fallback.CellHeight);
    }

    [Fact]
    public void UseAnsi_IsPassedThrough()
    {
        Assert.False(WidgetEnvironment.Create(RenderMode.Ascii, null, useAnsi: false).UseAnsi);
        Assert.True(WidgetEnvironment.Create(RenderMode.Ascii, null).UseAnsi);
    }

    [Theory]
    [InlineData(19, 1, 18)]
    [InlineData(20, 1, 18)]
    [InlineData(24, 1, 24)]
    [InlineData(20, 2, 36)]
    [InlineData(4, 1, 6)]
    public void GetImageHeight_IsLargestMultipleOfSixThatFits(int cellHeight, int rows, int expected) =>
        Assert.Equal(expected, new WidgetEnvironment(true, 10, cellHeight).GetImageHeight(rows));

    [Fact]
    public void InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WidgetEnvironment.Create((RenderMode)99, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WidgetEnvironment(true, cellWidth: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WidgetEnvironment(true, cellHeight: 0));
    }
}
