using System.Threading;
using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class StatusDisplayTests
{
    private const string Hide = "\u001b[?25l";
    private const string Show = "\u001b[?25h";
    private static readonly string Nl = Environment.NewLine;
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment Ansi = new WidgetEnvironment(false);
    private static readonly WidgetEnvironment Sixel = new WidgetEnvironment(true);

    private static string Frame(string glyph, string text) =>
        "\u001b[38;2;52;152;219m" + glyph + "\u001b[0m " + text + "\u001b[K";

    private static StatusDisplay Manual(TextWriter writer, WidgetEnvironment env, Func<long>? clock = null) =>
        new StatusDisplay(writer, env, clock ?? (() => 0)) { AutoRefresh = false };

    [Fact]
    public void Plain_PrintsOneLinePerTextChange()
    {
        var writer = new StringWriter();
        using (var status = new StatusDisplay(writer, Plain))
        {
            status.Start("Load");
            status.Text = "Next";
        }

        Assert.Equal("| Load" + Nl + "| Next" + Nl, writer.ToString());
    }

    [Fact]
    public void Ansi_ReservesDrawsAndRemovesStatus()
    {
        var writer = new StringWriter();
        StatusDisplay status = Manual(writer, Ansi);
        status.Start("Work");
        status.Stop();
        Assert.Equal(
            Hide + "\r\u001b7" + "\u001b8" + Frame("|", "Work") + "\u001b8" + "\u001b8\u001b[2K\u001b8\r" + Show,
            writer.ToString());
    }

    [Fact]
    public void Refresh_AdvancesSpinnerByElapsedTime_AndShowsNewText()
    {
        long now = 0;
        var writer = new StringWriter();
        StatusDisplay status = Manual(writer, Ansi, () => now);
        status.Start("A");
        now = 250;
        status.Text = "B";
        status.Refresh();
        Assert.Contains(Frame("-", "B"), writer.ToString());
    }

    [Fact]
    public void WriteLine_PrintsAboveStatus()
    {
        var writer = new StringWriter();
        StatusDisplay status = Manual(writer, Ansi);
        status.Start("Work");
        status.WriteLine("hello");
        Assert.Contains("\u001b8\u001b[2K\u001b8\rhello" + Nl + "\r\u001b7", writer.ToString());
    }

    [Fact]
    public void WriteLine_WithoutAnsi_JustWrites()
    {
        var writer = new StringWriter();
        using (var status = new StatusDisplay(writer, Plain))
        {
            status.Start("Work");
            status.WriteLine("hello");
        }

        Assert.Equal("| Work" + Nl + "hello" + Nl, writer.ToString());
    }

    [Fact]
    public void Sixel_DrawsGraphicAndReservesGuardRow()
    {
        var writer = new StringWriter();
        StatusDisplay status = Manual(writer, Sixel);
        status.Start("Work");
        status.Stop();
        string output = writer.ToString();
        Assert.StartsWith(Hide + "\r\n\u001b[1A\u001b7", output);
        Assert.Contains("\u001bP", output);
        Assert.EndsWith(Show, output);
    }

    [Fact]
    public void Dispose_StopsAndIsIdempotent()
    {
        var writer = new StringWriter();
        StatusDisplay status = Manual(writer, Ansi);
        status.Start("x");
        Assert.True(status.IsRunning);
        status.Dispose();
        status.Stop();
        Assert.False(status.IsRunning);
        Assert.EndsWith(Show, writer.ToString());
        Assert.Equal(1, CountOccurrences(writer.ToString(), Show));
    }

    [Fact]
    public void Start_CanBeRepeatedAfterStop()
    {
        var writer = new StringWriter();
        StatusDisplay status = Manual(writer, Ansi);
        status.Start("one");
        status.Stop();
        status.Start("two");
        status.Stop();
        Assert.Contains(Frame("|", "two"), writer.ToString());
    }

    [Fact]
    public void AutoRefresh_AnimatesOnTimer()
    {
        var writer = new StringWriter();
        var status = new StatusDisplay(writer, Ansi) { RefreshInterval = TimeSpan.FromMilliseconds(20) };
        status.Start("Work");
        Thread.Sleep(400);
        status.Stop();
        string output = writer.ToString();
        Assert.Contains(Frame("/", "Work"), output);
        Assert.Contains(Frame("-", "Work"), output);
    }

    [Fact]
    public void AfterStop_NothingIsDrawn()
    {
        var writer = new StringWriter();
        StatusDisplay status = Manual(writer, Ansi);
        status.Start("x");
        status.Stop();
        int length = writer.ToString().Length;
        status.Text = "late";
        status.Refresh();
        Assert.Equal(length, writer.ToString().Length);
    }

    [Fact]
    public void TallerStyleWhileRunning_IsSkippedWithoutThrowing()
    {
        var writer = new StringWriter();
        StatusDisplay status = Manual(writer, Sixel);
        status.Start("x");
        status.SpinnerStyle.HeightRows = 3;
        status.Refresh();
        status.Stop();
        Assert.EndsWith(Show, writer.ToString());
    }

    [Fact]
    public void NullText_BecomesEmpty()
    {
        var status = new StatusDisplay(new StringWriter(), Plain);
        status.Text = null!;
        Assert.Equal(string.Empty, status.Text);
    }

    [Fact]
    public void Misuse_Throws()
    {
        var writer = new StringWriter();
        Assert.Throws<ArgumentNullException>(() => new StatusDisplay(null!, Plain));
        Assert.Throws<ArgumentNullException>(() => new StatusDisplay(writer, null!));
        StatusDisplay status = Manual(writer, Plain);
        Assert.Throws<ArgumentOutOfRangeException>(() => status.RefreshInterval = TimeSpan.FromMilliseconds(5));
        status.Start("x");
        Assert.Throws<InvalidOperationException>(() => status.Start("y"));
    }

    private static int CountOccurrences(string text, string part)
    {
        int count = 0;
        for (int i = text.IndexOf(part, StringComparison.Ordinal); i >= 0; i = text.IndexOf(part, i + part.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
