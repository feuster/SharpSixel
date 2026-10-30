using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class SelectionPromptTests
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment AnsiAscii = new WidgetEnvironment(false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);

    private static SelectionPrompt<string> Prompt(params string[] choices)
    {
        var prompt = new SelectionPrompt<string> { Title = "Pick" };
        prompt.AddChoices(choices);
        return prompt;
    }

    private static string Show(SelectionPrompt<string> prompt, KeyScript keys, WidgetEnvironment env, out string result)
    {
        var writer = new StringWriter();
        result = prompt.Show(writer, env, keys.Read);
        return writer.ToString();
    }

    [Fact]
    public void Enter_ReturnsFirstChoiceByDefault()
    {
        Show(Prompt("A", "B"), new KeyScript().Enter(), AnsiAscii, out string result);
        Assert.Equal("A", result);
    }

    [Fact]
    public void DownAndUp_Wrap()
    {
        Show(Prompt("A", "B", "C"), new KeyScript().Up().Enter(), AnsiAscii, out string up);
        Assert.Equal("C", up);
        Show(Prompt("A", "B", "C"), new KeyScript().Down(3).Enter(), AnsiAscii, out string down);
        Assert.Equal("A", down);
    }

    [Fact]
    public void HomeEndAndPaging_Move()
    {
        SelectionPrompt<string> prompt = Prompt("A", "B", "C", "D", "E");
        prompt.PageSize = 2;
        Show(prompt, new KeyScript().End().Enter(), AnsiAscii, out string end);
        Assert.Equal("E", end);
        Show(prompt, new KeyScript().PageDown().PageDown().Enter(), AnsiAscii, out string paged);
        Assert.Equal("E", paged);
        Show(prompt, new KeyScript().End().Home().Enter(), AnsiAscii, out string home);
        Assert.Equal("A", home);
        Show(prompt, new KeyScript().End().PageUp().Enter(), AnsiAscii, out string pageUp);
        Assert.Equal("C", pageUp);
    }

    [Fact]
    public void NoChoices_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Show(Prompt(), new KeyScript(), AnsiAscii, out _));
    }

    [Fact]
    public void Validator_RejectsChoiceAndShowsMessage()
    {
        SelectionPrompt<string> prompt = Prompt("A", "B");
        prompt.Validator = c => c == "A" ? ValidationResult.Error("not A") : ValidationResult.Success();
        string output = Show(prompt, new KeyScript().Enter().Down().Enter(), AnsiAscii, out string result);
        Assert.Equal("B", result);
        Assert.Contains("! not A", output);
    }

    [Fact]
    public void Converter_ProvidesDisplayText()
    {
        var prompt = new SelectionPrompt<int> { Title = "Number", Converter = n => "#" + n };
        prompt.AddChoices(new[] { 1, 2 });
        var writer = new StringWriter();
        int result = prompt.Show(writer, AnsiAscii, new KeyScript().Down().Enter().Read);
        Assert.Equal(2, result);
        Assert.Contains("> (*) #2", writer.ToString());
        Assert.Contains("  ( ) #1", writer.ToString());
    }

    [Fact]
    public void Ascii_ShowsPointerMarkersAndInstructions()
    {
        string output = Show(Prompt("A", "B"), new KeyScript().Enter(), AnsiAscii, out _);
        Assert.Contains("Pick", output);
        Assert.Contains("> (*) A", output);
        Assert.Contains("  ( ) B", output);
        Assert.Contains("Enter to select", output);
    }

    [Fact]
    public void Instructions_CanBeHidden()
    {
        SelectionPrompt<string> prompt = Prompt("A");
        prompt.Instructions = null;
        Assert.DoesNotContain("Enter to select", Show(prompt, new KeyScript().Enter(), AnsiAscii, out _));
    }

    [Fact]
    public void Live_HidesCursorClearsRegionAndPrintsSummary()
    {
        string output = Show(Prompt("A", "B"), new KeyScript().Down().Enter(), AnsiAscii, out _);
        Assert.StartsWith("\u001b[?25l", output);
        Assert.Contains("\u001b[?25h", output);
        Assert.Contains("Pick ", output);
        Assert.EndsWith(Environment.NewLine, output);
        Assert.EndsWith("B\u001b[0m" + Environment.NewLine, output);
    }

    [Fact]
    public void CancelledKeySource_RestoresCursor()
    {
        var writer = new StringWriter();
        Assert.Throws<OperationCanceledException>(
            () => Prompt("A").Show(writer, AnsiAscii, () => throw new OperationCanceledException()));
        Assert.EndsWith("\u001b[?25h", writer.ToString());
    }

    [Fact]
    public void Sixel_DrawsMarkerGraphic()
    {
        string output = Show(Prompt("A", "B"), new KeyScript().Enter(), SixelEnv, out _);
        Assert.Contains("\u001bP", output);
        Assert.Contains("\u001b[4C", output);
    }

    [Fact]
    public void RowCount_IncludesTitleVisibleItemsInstructionsAndError()
    {
        SelectionPrompt<string> prompt = Prompt("A", "B", "C");
        prompt.PageSize = 2;
        var writer = new StringWriter();
        prompt.Show(writer, AnsiAscii, new KeyScript().Enter().Read);
        // 1 title + 2 visible + 1 instructions + 1 error row: Begin reserves 5 rows (4 newlines).
        Assert.Contains("\r\n\n\n\n\u001b[4A", writer.ToString());
    }

    [Fact]
    public void Plain_ReadsNumber()
    {
        string output = Show(Prompt("A", "B"), new KeyScript().Type("2").Enter(), Plain, out string result);
        Assert.Equal("B", result);
        Assert.Contains("  1) A", output);
        Assert.Contains("  2) B", output);
        Assert.DoesNotContain("\u001b", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Plain_InvalidNumberAndValidatorAreReported()
    {
        SelectionPrompt<string> prompt = Prompt("A", "B");
        prompt.Validator = c => c == "A" ? ValidationResult.Error("not A") : ValidationResult.Success();
        string output = Show(prompt, new KeyScript().Type("x").Enter().Type("1").Enter().Type("2").Enter(), Plain, out string result);
        Assert.Equal("B", result);
        Assert.Contains("! Enter a number between 1 and 2.", output);
        Assert.Contains("! not A", output);
    }

    [Fact]
    public void ControlCharactersInLabels_AreReplaced()
    {
        string output = Show(Prompt("a\nb"), new KeyScript().Enter(), AnsiAscii, out _);
        Assert.Contains("a b", output);
    }
}
