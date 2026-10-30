using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class MultiSelectionPromptTests
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment AnsiAscii = new WidgetEnvironment(false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);

    private static MultiSelectionPrompt<string> Prompt()
    {
        var prompt = new MultiSelectionPrompt<string> { Title = "Toppings" };
        prompt.AddChoice("A");
        prompt.AddChoice("B");
        prompt.AddChoice("C");
        return prompt;
    }

    private static string Show(MultiSelectionPrompt<string> prompt, KeyScript keys, WidgetEnvironment env, out IReadOnlyList<string> result)
    {
        var writer = new StringWriter();
        result = prompt.Show(writer, env, keys.Read);
        return writer.ToString();
    }

    [Fact]
    public void SpaceTogglesAndOrderFollowsChoices()
    {
        Show(Prompt(), new KeyScript().Down(2).Space().Up().Up().Space().Enter(), AnsiAscii, out IReadOnlyList<string> result);
        Assert.Equal(new[] { "A", "C" }, result);
    }

    [Fact]
    public void SpaceTwice_Deselects()
    {
        Show(Prompt(), new KeyScript().Space().Space().Enter(), AnsiAscii, out IReadOnlyList<string> result);
        Assert.Empty(result);
    }

    [Fact]
    public void Preselected_AreReturnedAndStateResetsPerShow()
    {
        var prompt = new MultiSelectionPrompt<string>();
        prompt.AddChoice("A", selected: true);
        prompt.AddChoice("B");
        Show(prompt, new KeyScript().Space().Down().Space().Enter(), AnsiAscii, out IReadOnlyList<string> first);
        Assert.Equal(new[] { "B" }, first);
        Show(prompt, new KeyScript().Enter(), AnsiAscii, out IReadOnlyList<string> second);
        Assert.Equal(new[] { "A" }, second);
    }

    [Fact]
    public void MinSelected_BlocksUntilSatisfied()
    {
        MultiSelectionPrompt<string> prompt = Prompt();
        prompt.MinSelected = 2;
        string output = Show(prompt, new KeyScript().Enter().Space().Down().Space().Enter(), AnsiAscii, out IReadOnlyList<string> result);
        Assert.Equal(new[] { "A", "B" }, result);
        Assert.Contains("! Select at least 2 items.", output);
    }

    [Fact]
    public void MaxSelected_BlocksUntilSatisfied()
    {
        MultiSelectionPrompt<string> prompt = Prompt();
        prompt.MaxSelected = 1;
        string output = Show(prompt, new KeyScript().Space().Down().Space().Enter().Space().Enter(), AnsiAscii, out IReadOnlyList<string> result);
        Assert.Equal(new[] { "A" }, result);
        Assert.Contains("! Select at most 1 item.", output);
    }

    [Fact]
    public void CustomValidator_RunsAfterBuiltInRules()
    {
        MultiSelectionPrompt<string> prompt = Prompt();
        prompt.Validator = items => items.Contains("B") ? ValidationResult.Error("no B") : ValidationResult.Success();
        string output = Show(prompt, new KeyScript().Down().Space().Enter().Space().Enter(), AnsiAscii, out IReadOnlyList<string> result);
        Assert.Empty(result);
        Assert.Contains("! no B", output);
    }

    [Fact]
    public void InvalidConfiguration_Throws()
    {
        MultiSelectionPrompt<string> tooMany = Prompt();
        tooMany.MinSelected = 4;
        Assert.Throws<InvalidOperationException>(() => Show(tooMany, new KeyScript(), AnsiAscii, out _));

        MultiSelectionPrompt<string> inverted = Prompt();
        inverted.MinSelected = 2;
        inverted.MaxSelected = 1;
        Assert.Throws<InvalidOperationException>(() => Show(inverted, new KeyScript(), AnsiAscii, out _));

        Assert.Throws<InvalidOperationException>(
            () => Show(new MultiSelectionPrompt<string>(), new KeyScript(), AnsiAscii, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => Prompt().MaxSelected = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => Prompt().MinSelected = -1);
    }

    [Fact]
    public void Ascii_ShowsCheckMarkers()
    {
        var prompt = new MultiSelectionPrompt<string>();
        prompt.AddChoice("A", selected: true);
        prompt.AddChoice("B");
        string output = Show(prompt, new KeyScript().Enter(), AnsiAscii, out _);
        Assert.Contains("> [x] A", output);
        Assert.Contains("  [ ] B", output);
    }

    [Fact]
    public void Summary_ListsSelectedLabels()
    {
        string output = Show(Prompt(), new KeyScript().Space().Down().Down().Space().Enter(), AnsiAscii, out _);
        Assert.Contains("A, C", output);
    }

    [Fact]
    public void Sixel_DrawsMarkerGraphic()
    {
        Assert.Contains("\u001bP", Show(Prompt(), new KeyScript().Enter(), SixelEnv, out _));
    }

    [Fact]
    public void Plain_ReadsNumbersAndEmptyKeepsPreselection()
    {
        Show(Prompt(), new KeyScript().Type("3, 1").Enter(), Plain, out IReadOnlyList<string> numbers);
        Assert.Equal(new[] { "A", "C" }, numbers);

        var prompt = new MultiSelectionPrompt<string>();
        prompt.AddChoice("A", selected: true);
        prompt.AddChoice("B");
        Show(prompt, new KeyScript().Enter(), Plain, out IReadOnlyList<string> kept);
        Assert.Equal(new[] { "A" }, kept);
    }

    [Fact]
    public void Plain_ReportsInvalidInputAndCountRules()
    {
        MultiSelectionPrompt<string> prompt = Prompt();
        prompt.MinSelected = 1;
        string output = Show(prompt, new KeyScript().Type("9").Enter().Enter().Type("2").Enter(), Plain, out IReadOnlyList<string> result);
        Assert.Equal(new[] { "B" }, result);
        Assert.Contains("! Enter numbers between 1 and 3, separated by commas.", output);
        Assert.Contains("! Select at least 1 item.", output);
        Assert.DoesNotContain("\u001b", output, StringComparison.Ordinal);
    }
}
