using SharpSixel;
using SharpSixelUI;

namespace SharpSixelUI.Tests;

public class UiCliAppTests
{
    private sealed class Run
    {
        public int Code;
        public string Output = string.Empty;
        public string Error = string.Empty;
        public int Delays;
        public bool ModeEntered;
        public bool ModeDisposed;
    }

    private sealed class Disposable : IDisposable
    {
        private readonly Action _onDispose;

        public Disposable(Action onDispose) => _onDispose = onDispose;

        public void Dispose() => _onDispose();
    }

    private static readonly SixelTerminalInfo SixelInfo = new SixelTerminalInfo(true, 0, 0, 10, 20);
    private static readonly SixelTerminalInfo NoSixelInfo = new SixelTerminalInfo(false, 0, 0, 10, 20);
    private static readonly DateTime FixedNow = new DateTime(2026, 10, 28, 14, 5, 9);

    private static Run Execute(string[] args, bool terminal, SixelTerminalInfo? info = null, int cancelAfterChecks = int.MaxValue)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var run = new Run();
        int checks = 0;
        var env = new UiCliEnvironment(
            output,
            error,
            () => terminal,
            () => info,
            () =>
            {
                run.ModeEntered = true;
                return new Disposable(() => run.ModeDisposed = true);
            },
            _ => run.Delays++,
            () => ++checks > cancelAfterChecks,
            now: () => FixedNow,
            culture: System.Globalization.CultureInfo.GetCultureInfo("en-US"));
        run.Code = UiCliApp.Run(args, env);
        run.Output = output.ToString();
        run.Error = error.ToString();
        return run;
    }

    [Fact]
    public void Help_PrintsUsage()
    {
        Run run = Execute(new[] { "--help" }, false);
        Assert.Equal(0, run.Code);
        Assert.Contains("Usage: SharpSixelUI", run.Output);
    }

    [Fact]
    public void MissingOrUnknownCommand_Fails()
    {
        Run none = Execute(new string[0], false);
        Assert.Equal(1, none.Code);
        Assert.Contains("No command", none.Error);

        Run unknown = Execute(new[] { "bogus" }, false);
        Assert.Equal(1, unknown.Code);
        Assert.Contains("Unknown command bogus", unknown.Error);
    }

    [Fact]
    public void InvalidArguments_FailWithMessage()
    {
        Run run = Execute(new[] { "progress", "--mode", "x" }, false);
        Assert.Equal(1, run.Code);
        Assert.Contains("--mode", run.Error);
        Assert.Equal(string.Empty, run.Output);
    }

    [Fact]
    public void InvalidStyleValue_FailsWithoutCrash()
    {
        Run run = Execute(new[] { "progress", "--width", "2" }, false);
        Assert.Equal(1, run.Code);
        Assert.NotEqual(string.Empty, run.Error);
    }

    [Fact]
    public void Redirected_Progress_PrintsOneAsciiLine()
    {
        Run run = Execute(new[] { "progress", "--caption", "Job" }, false, SixelInfo);
        Assert.Equal(0, run.Code);
        Assert.StartsWith("Job [", run.Output);
        Assert.Contains("100%", run.Output);
        Assert.DoesNotContain('\u001b', run.Output);
        Assert.Equal(0, run.Delays);
        Assert.False(run.ModeEntered);
    }

    [Fact]
    public void Redirected_Spinner_PrintsOneFrame()
    {
        Run run = Execute(new[] { "spinner", "--mode", "sixel" }, false, SixelInfo);
        Assert.Equal(0, run.Code);
        Assert.Equal("|" + Environment.NewLine, run.Output);
    }

    [Fact]
    public void Terminal_AsciiMode_AnimatesAndRestoresCursor()
    {
        Run run = Execute(new[] { "progress", "--mode", "ascii", "--duration", "1" }, true, SixelInfo);
        Assert.Equal(0, run.Code);
        Assert.Equal(21, run.Delays);
        Assert.StartsWith("\u001b[?25l", run.Output);
        Assert.Contains("100%", run.Output);
        Assert.EndsWith("\u001b[?25h", run.Output);
        Assert.DoesNotContain("\u001bP", run.Output);
        Assert.True(run.ModeEntered);
        Assert.True(run.ModeDisposed);
    }

    [Fact]
    public void Terminal_Auto_UsesSixelOnlyIfSupported()
    {
        Assert.Contains("\u001bP", Execute(new[] { "progress", "--duration", "1" }, true, SixelInfo).Output);
        Assert.DoesNotContain("\u001bP", Execute(new[] { "progress", "--duration", "1" }, true, NoSixelInfo).Output);
        Assert.DoesNotContain("\u001bP", Execute(new[] { "progress", "--duration", "1" }, true, null).Output);
    }

    [Fact]
    public void Terminal_Spinner_RunsExpectedFrames()
    {
        Run run = Execute(new[] { "spinner", "--mode", "ascii", "--duration", "1", "--caption", "Wait", "--no-ansi" }, true);
        Assert.Equal(0, run.Code);
        Assert.Equal(10, run.Delays);
        Assert.Contains("| Wait", run.Output);
        Assert.Contains("/ Wait", run.Output);
        Assert.Contains("\u001b[2K", run.Output);
    }

    [Fact]
    public void Terminal_Progress_KeepsFinalFrame() =>
        Assert.DoesNotContain("\u001b[2K", Execute(new[] { "progress", "--mode", "ascii", "--duration", "1" }, true).Output);

    [Fact]
    public void Demo_RunsAllWidgetsWithDefaultCaptions()
    {
        Run run = Execute(new[] { "demo", "--mode", "ascii", "--duration", "1" }, true);
        Assert.Equal(0, run.Code);
        Assert.Equal(90, run.Delays);
        Assert.Contains("Download", run.Output);
        Assert.Contains("Loading", run.Output);
        Assert.Contains("Connecting to server done", run.Output);
        Assert.Contains("Fruit sales", run.Output);
    }

    [Fact]
    public void Redirected_Chart_PrintsPlainAsciiLines()
    {
        Run run = Execute(new[] { "chart", "--caption", "Sales", "--width", "10" }, false, SixelInfo);
        Assert.Equal(0, run.Code);
        Assert.Equal(0, run.Delays);
        Assert.DoesNotContain('\u001b', run.Output);
        string[] lines = run.Output.TrimEnd().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        Assert.Equal(5, lines.Length);
        Assert.Equal("Sales", lines[0]);
        Assert.Equal("Apples   ######     42", lines[1]);
        Assert.Equal("Cherries ########## 65", lines[3]);
    }

    [Fact]
    public void Terminal_Chart_AnimatesPlacesLinesAndRestoresCursor()
    {
        Run run = Execute(new[] { "chart", "--mode", "ascii", "--duration", "1", "--no-ansi" }, true);
        Assert.Equal(0, run.Code);
        Assert.Equal(21, run.Delays);
        Assert.Contains("Cherries ", run.Output);
        Assert.Contains("\u001b[3B", run.Output);
        Assert.EndsWith("\u001b[?25h", run.Output);
    }

    [Fact]
    public void InvalidChartRows_FailWithoutCrash()
    {
        Run run = Execute(new[] { "chart", "--rows", "11" }, false);
        Assert.Equal(1, run.Code);
        Assert.NotEqual(string.Empty, run.Error);
    }

    [Fact]
    public void Redirected_Table_PrintsPlainAsciiGrid()
    {
        Run run = Execute(new[] { "table", "--caption", "Stock" }, false, SixelInfo);
        Assert.Equal(0, run.Code);
        Assert.Equal(0, run.Delays);
        Assert.DoesNotContain('\u001b', run.Output);
        string[] lines = run.Output.TrimEnd().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        Assert.Equal(9, lines.Length);
        Assert.Equal("Stock", lines[0]);
        Assert.Equal("+----------+----------+-------+", lines[1]);
        Assert.Equal("| Apples   |       42 |  4.20 |", lines[4]);
    }

    [Fact]
    public void Terminal_Table_DrawsOnceAndRestoresCursor()
    {
        Run run = Execute(new[] { "table", "--mode", "ascii", "--no-ansi" }, true);
        Assert.Equal(0, run.Code);
        Assert.Equal(0, run.Delays);
        Assert.Contains("| Cherries |", run.Output);
        Assert.Contains("\u001b[4B", run.Output);
        Assert.EndsWith("\u001b[?25h", run.Output);
    }

    [Fact]
    public void Demo_ShowsTableWithDefaultCaption()
    {
        Run run = Execute(new[] { "demo", "--mode", "ascii", "--duration", "1", "--no-ansi" }, true);
        Assert.Equal(0, run.Code);
        Assert.Contains("Fruit stock", run.Output);
    }

    [Fact]
    public void Redirected_Button_PrintsAllStatesAsPlainAscii()
    {
        Run run = Execute(new[] { "button", "--caption", "Save" }, false, SixelInfo);
        Assert.Equal(0, run.Code);
        Assert.Equal(0, run.Delays);
        Assert.DoesNotContain('\u001b', run.Output);
        string[] lines = run.Output.TrimEnd().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        Assert.Equal(new[] { "[ Save ]", "[>Save<]", "[#Save#]", "[-Save-]" }, lines);
    }

    [Fact]
    public void Terminal_Button_CyclesStatesAndRestoresCursor()
    {
        Run run = Execute(new[] { "button", "--mode", "ascii", "--duration", "1", "--no-ansi" }, true);
        Assert.Equal(0, run.Code);
        Assert.Equal(20, run.Delays);
        Assert.Contains("[>OK<]", run.Output);
        Assert.Contains("[-OK-]", run.Output);
        Assert.EndsWith("\u001b[?25h", run.Output);
    }

    [Fact]
    public void InvalidButtonRows_FailWithoutCrash()
    {
        Run run = Execute(new[] { "button", "--rows", "11" }, false);
        Assert.Equal(1, run.Code);
        Assert.NotEqual(string.Empty, run.Error);
    }

    [Fact]
    public void Redirected_Panel_PrintsPlainAsciiFrame()
    {
        Run run = Execute(new[] { "panel", "--caption", "Info" }, false, SixelInfo);
        Assert.Equal(0, run.Code);
        Assert.Equal(0, run.Delays);
        Assert.DoesNotContain('\u001b', run.Output);
        string[] lines = run.Output.TrimEnd().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        Assert.Equal(
            new[] { "+- Info ---------+", "| Server: online |", "| Files: 42      |", "| Errors: 0      |", "+----------------+" },
            lines);
    }

    [Fact]
    public void Terminal_Panel_DrawsOnceWithColorsAndRestoresCursor()
    {
        Run run = Execute(new[] { "panel", "--mode", "ascii", "--caption", "Info", "--active", "#FF0000" }, true);
        Assert.Equal(0, run.Code);
        Assert.Equal(0, run.Delays);
        Assert.Contains("\u001b[38;2;255;0;0m Info \u001b[0m", run.Output);
        Assert.Contains(" Errors: 0      ", run.Output);
        Assert.Contains("\u001b[3B", run.Output);
        Assert.EndsWith("\u001b[?25h", run.Output);
    }

    [Fact]
    public void Terminal_Panel_FixedWidthCutsLines()
    {
        Run run = Execute(new[] { "panel", "--mode", "ascii", "--no-ansi", "--width", "8" }, true);
        Assert.Equal(0, run.Code);
        Assert.Contains("| Server:\u2026 |", run.Output);
    }

    [Fact]
    public void Demo_ShowsPanelWithDefaultTitle()
    {
        Run run = Execute(new[] { "demo", "--mode", "ascii", "--duration", "1", "--no-ansi" }, true);
        Assert.Equal(0, run.Code);
        Assert.Contains("+- Status -", run.Output);
    }

    [Fact]
    public void InvalidPanelWidth_FailsWithoutCrash()
    {
        Run run = Execute(new[] { "panel", "--width", "0" }, false);
        Assert.Equal(1, run.Code);
        Assert.NotEqual(string.Empty, run.Error);
    }

    [Fact]
    public void Redirected_Clock_PrintsTimeAsPlainText()
    {
        Run run = Execute(new[] { "clock" }, false, SixelInfo);
        Assert.Equal(0, run.Code);
        Assert.Equal(0, run.Delays);
        Assert.Equal("14:05:09" + Environment.NewLine, run.Output);
        Assert.Equal("Now 14:05:09" + Environment.NewLine, Execute(new[] { "clock", "--caption", "Now" }, false).Output);
    }

    [Fact]
    public void Terminal_Clock_UpdatesEveryFrameAndRestoresCursor()
    {
        Run run = Execute(new[] { "clock", "--mode", "ascii", "--duration", "1", "--no-ansi" }, true);
        Assert.Equal(0, run.Code);
        Assert.Equal(10, run.Delays);
        Assert.Contains("14:05:09", run.Output);
        Assert.EndsWith("\u001b[?25h", run.Output);
    }

    [Fact]
    public void Terminal_Clock_RedrawsOnlyWhenTheSecondChanges()
    {
        Run run = Execute(new[] { "clock", "--mode", "ascii", "--duration", "1", "--no-ansi" }, true);
        Assert.Equal(10, run.Delays);
        Assert.Equal(1, run.Output.Split(new[] { "14:05:09" }, StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void Terminal_Clock_DrawsSixelGraphicIfSupported()
    {
        Assert.Contains("\u001bP", Execute(new[] { "clock", "--duration", "1", "--rows", "2" }, true, SixelInfo).Output);
        Assert.DoesNotContain("\u001bP", Execute(new[] { "clock", "--duration", "1" }, true, NoSixelInfo).Output);
    }

    [Fact]
    public void Cancel_StopsClock()
    {
        Run run = Execute(new[] { "clock", "--mode", "ascii", "--duration", "1" }, true, null, cancelAfterChecks: 3);
        Assert.Equal(0, run.Code);
        Assert.Equal(2, run.Delays);
        Assert.EndsWith("\u001b[?25h", run.Output);
    }

    [Fact]
    public void InvalidClockRows_FailWithoutCrash()
    {
        Run run = Execute(new[] { "clock", "--rows", "21" }, false);
        Assert.Equal(1, run.Code);
        Assert.NotEqual(string.Empty, run.Error);
    }

    [Fact]
    public void Redirected_Date_PrintsDateAsPlainText()
    {
        Run run = Execute(new[] { "date" }, false, SixelInfo);
        Assert.Equal(0, run.Code);
        Assert.Equal(0, run.Delays);
        Assert.Equal("Wed, 10/28/2026" + Environment.NewLine, run.Output);
    }

    [Fact]
    public void Terminal_Date_DrawsOnceAndRestoresCursor()
    {
        Run run = Execute(new[] { "date", "--mode", "ascii", "--no-ansi", "--caption", "Today" }, true);
        Assert.Equal(0, run.Code);
        Assert.Equal(0, run.Delays);
        Assert.Contains("Today Wed, 10/28/2026", run.Output);
        Assert.EndsWith("\u001b[?25h", run.Output);
        Assert.Contains("\u001bP", Execute(new[] { "date" }, true, SixelInfo).Output);
    }

    [Fact]
    public void InvalidDateRadius_FailsWithoutCrash()
    {
        Run run = Execute(new[] { "date", "--radius", "-1" }, false);
        Assert.Equal(1, run.Code);
        Assert.NotEqual(string.Empty, run.Error);
    }

    [Fact]
    public void Demo_ShowsClockAndDate()
    {
        Run run = Execute(new[] { "demo", "--mode", "ascii", "--duration", "1", "--no-ansi" }, true);
        Assert.Equal(0, run.Code);
        Assert.Contains("14:05:09", run.Output);
        Assert.Contains("Wed, 10/28/2026", run.Output);
    }

    [Fact]
    public void Cancel_StopsLoop()
    {
        Run run = Execute(new[] { "progress", "--mode", "ascii", "--duration", "1" }, true, null, cancelAfterChecks: 3);
        Assert.Equal(0, run.Code);
        Assert.Equal(3, run.Delays);
        Assert.EndsWith("\u001b[?25h", run.Output);
    }

    [Fact]
    public void Redirected_Status_PrintsStagesAsPlainLines()
    {
        Run run = Execute(new[] { "status" }, false, SixelInfo);
        Assert.Equal(0, run.Code);
        Assert.Equal(0, run.Delays);
        Assert.DoesNotContain('\u001b', run.Output);
        Assert.Contains("| Connecting to server", run.Output);
        Assert.Contains("Processing records done", run.Output);
    }

    [Fact]
    public void Terminal_Status_RunsStagesAndRestoresCursor()
    {
        Run run = Execute(new[] { "status", "--mode", "ascii", "--duration", "1" }, true);
        Assert.Equal(0, run.Code);
        Assert.Equal(4 * 2, run.Delays);
        Assert.Contains("Loading configuration", run.Output);
        Assert.Contains("Fetching data done", run.Output);
        Assert.EndsWith("\u001b[?25h", run.Output);
    }

    [Fact]
    public void Terminal_Status_WithCaption_UsesSingleStage()
    {
        Run run = Execute(new[] { "status", "--mode", "ascii", "--duration", "1", "--caption", "Wait" }, true);
        Assert.Equal(10, run.Delays);
        Assert.Contains("Wait done", run.Output);
        Assert.DoesNotContain("Fetching data", run.Output);
    }

    [Fact]
    public void Cancel_StopsStatus()
    {
        Run run = Execute(new[] { "status", "--mode", "ascii", "--duration", "1" }, true, null, cancelAfterChecks: 2);
        Assert.Equal(0, run.Code);
        Assert.True(run.Delays < 8);
        Assert.EndsWith("\u001b[?25h", run.Output);
    }

    [Fact]
    public void Cancel_InDemo_SkipsSpinner()
    {
        Run run = Execute(new[] { "demo", "--mode", "ascii" }, true, null, cancelAfterChecks: 0);
        Assert.Equal(0, run.Delays);
        Assert.DoesNotContain("Loading", run.Output);
        Assert.DoesNotContain("done", run.Output);
    }

    [Fact]
    public void Redirected_Tree_PrintsPlainAsciiTree()
    {
        Run run = Execute(new[] { "tree", "--caption", "Project" }, false, SixelInfo);
        Assert.Equal(0, run.Code);
        Assert.Equal(0, run.Delays);
        Assert.DoesNotContain('\u001b', run.Output);
        string[] lines = run.Output.TrimEnd().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        Assert.Equal(
            new[]
            {
                "Project", "+-- src", "|   +-- Program.cs", "|   `-- Ui", "|       +-- Tree.cs", "|       `-- Clock.cs",
                "+-- docs", "|   `-- README.md", "`-- tests [+]",
            },
            lines);
    }

    [Fact]
    public void Terminal_Tree_DrawsOnceWithColorsAndRestoresCursor()
    {
        Run run = Execute(new[] { "tree", "--mode", "ascii", "--active", "#FF0000" }, true);
        Assert.Equal(0, run.Code);
        Assert.Equal(0, run.Delays);
        Assert.Contains("\u001b[38;2;255;0;0mFiles\u001b[0m", run.Output);
        Assert.Contains("tests [+]", run.Output);
        Assert.Contains("\u001b[3B", run.Output);
        Assert.EndsWith("\u001b[?25h", run.Output);
        Assert.Contains("`-- tests [+]", Execute(new[] { "tree", "--mode", "ascii", "--no-ansi" }, true).Output);
    }

    [Fact]
    public void Terminal_Tree_DrawsSixelGuidesIfSupported()
    {
        Assert.Contains("\u001bP", Execute(new[] { "tree" }, true, SixelInfo).Output, StringComparison.Ordinal);
        Assert.DoesNotContain("\u001bP", Execute(new[] { "tree" }, true, NoSixelInfo).Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Demo_ShowsTreeWithDefaultTitle()
    {
        Run run = Execute(new[] { "demo", "--mode", "ascii", "--duration", "1", "--no-ansi" }, true);
        Assert.Equal(0, run.Code);
        Assert.Contains("Files", run.Output);
        Assert.Contains("`-- tests [+]", run.Output);
    }

    [Fact]
    public void Colors_AreAppliedToAsciiWidgets()
    {
        string colored = Execute(new[] { "progress", "--mode", "ascii", "--duration", "1", "--fill", "#FF0000" }, true).Output;
        Assert.Contains("\u001b[38;2;255;0;0m", colored);

        string plain = Execute(new[] { "progress", "--mode", "ascii", "--duration", "1", "--no-ansi" }, true).Output;
        Assert.DoesNotContain("\u001b[38;2", plain);
    }

    private const string OscResetTail = "\u001b]110\u0007\u001b]111\u0007\u001b]112\u0007\u001b[0 q";

    [Fact]
    public void Osc_SetsTerminalAndResetsAfterwards()
    {
        Run run = Execute(new[] { "osc", "--duration", "1", "--caption", "Demo", "--fill", "#112233" }, true);

        Assert.Equal(0, run.Code);
        Assert.Contains("\u001b]0;Demo\u0007", run.Output, StringComparison.Ordinal);
        Assert.Contains("\u001b]10;#112233\u0007", run.Output, StringComparison.Ordinal);
        Assert.Contains("\u001b]11;#102040\u0007", run.Output, StringComparison.Ordinal);
        Assert.Contains("\u001b]12;#FF4040\u0007", run.Output, StringComparison.Ordinal);
        Assert.Contains("\u001b[5 q", run.Output, StringComparison.Ordinal);
        Assert.Equal(6, run.Delays);
        Assert.EndsWith(OscResetTail, run.Output, StringComparison.Ordinal);
        Assert.True(run.ModeEntered && run.ModeDisposed);
    }

    [Fact]
    public void Osc_Cancelled_StillResets()
    {
        Run run = Execute(new[] { "osc", "--duration", "1" }, true, cancelAfterChecks: 2);

        Assert.Equal(0, run.Code);
        Assert.Equal(2, run.Delays);
        Assert.EndsWith(OscResetTail, run.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Osc_WithoutTerminal_Fails()
    {
        Run run = Execute(new[] { "osc" }, false);

        Assert.Equal(1, run.Code);
        Assert.Contains("needs a terminal", run.Error);
        Assert.Equal(string.Empty, run.Output);
    }
}
