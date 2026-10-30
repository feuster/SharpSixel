using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class LiveRegionTests
{
    private static readonly WidgetEnvironment Ascii = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment Sixel = new WidgetEnvironment(true);

    [Fact]
    public void Begin_ReservesRowsAndSavesOrigin()
    {
        var writer = new StringWriter();
        new LiveRegion(writer, Sixel).Begin(3);
        Assert.Equal("\r\n\n\n\u001b[3A\u001b7", writer.ToString());
    }

    [Fact]
    public void SixelMode_ReservesGuardRowBelowRegion()
    {
        var writer = new StringWriter();
        var region = new LiveRegion(writer, Sixel);
        region.Begin(1);
        Assert.Equal("\r\n\u001b[1A\u001b7", writer.ToString());
        writer.GetStringBuilder().Clear();
        region.End();
        Assert.Equal("\u001b8\u001b[1B\r", writer.ToString());
    }

    [Fact]
    public void Begin_SingleRow_ReservesNoNewline()
    {
        var writer = new StringWriter();
        new LiveRegion(writer, Ascii).Begin(1);
        Assert.Equal("\r\u001b7", writer.ToString());
    }

    [Fact]
    public void Update_RestoresOriginAroundFrame()
    {
        var writer = new StringWriter();
        var region = new LiveRegion(writer, Ascii);
        region.Begin(1);
        writer.GetStringBuilder().Clear();
        var bar = new ProgressBar { ProgressText = ProgressTextMode.None };
        bar.Style.Width = 5;
        region.Update(bar);
        Assert.Equal("\u001b8[---]\u001b8", writer.ToString());
    }

    [Fact]
    public void End_MovesBelowRegion_AndIsIdempotent()
    {
        var writer = new StringWriter();
        var region = new LiveRegion(writer, Sixel);
        region.Begin(3);
        writer.GetStringBuilder().Clear();
        region.End();
        region.End();
        Assert.Equal("\u001b8\u001b[3B\r", writer.ToString());
    }

    [Fact]
    public void End_WithClear_ErasesRowsAndStaysAtOrigin()
    {
        var single = new StringWriter();
        var ascii = new LiveRegion(single, Ascii);
        ascii.Begin(1);
        single.GetStringBuilder().Clear();
        ascii.End(clear: true);
        Assert.Equal("\u001b8\u001b[2K\u001b8\r", single.ToString());

        var multi = new StringWriter();
        var sixel = new LiveRegion(multi, Sixel);
        sixel.Begin(3);
        multi.GetStringBuilder().Clear();
        sixel.End(clear: true);
        sixel.End(clear: true);
        Assert.Equal(
            "\u001b8\u001b[2K\u001b8\u001b[1B\u001b[2K\u001b8\u001b[2B\u001b[2K\u001b8\r",
            multi.ToString());
    }

    [Fact]
    public void Dispose_EndsRegion()
    {
        var writer = new StringWriter();
        using (var region = new LiveRegion(writer, Ascii))
        {
            region.Begin(1);
            writer.GetStringBuilder().Clear();
        }

        Assert.Equal("\u001b8\r\n", writer.ToString());
    }

    [Fact]
    public void Misuse_Throws()
    {
        var writer = new StringWriter();
        var region = new LiveRegion(writer, Ascii);
        var bar = new ProgressBar();
        Assert.Throws<InvalidOperationException>(() => region.Update(bar));
        region.Begin(1);
        Assert.Throws<InvalidOperationException>(() => region.Begin(1));
        Assert.Throws<ArgumentNullException>(() => region.Update(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LiveRegion(writer, Ascii).Begin(0));
        Assert.Throws<ArgumentNullException>(() => new LiveRegion(null!, Ascii));
        Assert.Throws<ArgumentNullException>(() => new LiveRegion(writer, null!));
    }

    [Fact]
    public void Update_WidgetTallerThanRegion_Throws()
    {
        var region = new LiveRegion(new StringWriter(), Sixel);
        region.Begin(1);
        var bar = new ProgressBar();
        bar.Style.HeightRows = 2;
        Assert.Throws<ArgumentException>(() => region.Update(bar));
    }
}
