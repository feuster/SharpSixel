using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class DialogTests
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment AnsiAscii = new WidgetEnvironment(false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);

    private static Dialog YesNo(string? message = "Really?")
    {
        var dialog = new Dialog { Title = "Confirm", Message = message };
        dialog.SetButtons("Yes", "No");
        return dialog;
    }

    private static string Show(Dialog dialog, KeyScript keys, WidgetEnvironment env, out int result)
    {
        var writer = new StringWriter();
        result = dialog.Show(writer, env, keys.Read);
        return writer.ToString();
    }

    [Fact]
    public void Enter_ReturnsDefaultButtonIndex()
    {
        Show(YesNo(), new KeyScript().Enter(), AnsiAscii, out int result);
        Assert.Equal(0, result);

        Dialog second = YesNo();
        second.DefaultButton = 1;
        Show(second, new KeyScript().Enter(), AnsiAscii, out int defaultSecond);
        Assert.Equal(1, defaultSecond);
    }

    [Fact]
    public void SingleOkButtonByDefault()
    {
        var dialog = new Dialog { Message = "Done." };
        string output = Show(dialog, new KeyScript().Enter(), AnsiAscii, out int result);
        Assert.Equal(0, result);
        Assert.Contains("[ OK ]", output);
    }

    [Fact]
    public void LeftRightTabHomeEnd_MoveAndWrap()
    {
        Dialog three = YesNo();
        three.SetButtons("A", "B", "C");
        Show(three, new KeyScript().Press(ConsoleKey.RightArrow).Enter(), AnsiAscii, out int right);
        Assert.Equal(1, right);
        Show(three, new KeyScript().Press(ConsoleKey.LeftArrow).Enter(), AnsiAscii, out int left);
        Assert.Equal(2, left);
        Show(three, new KeyScript().Press(ConsoleKey.Tab).Press(ConsoleKey.Tab).Press(ConsoleKey.Tab).Enter(), AnsiAscii, out int tab);
        Assert.Equal(0, tab);
        Show(three, new KeyScript().Press(ConsoleKey.End).Enter(), AnsiAscii, out int end);
        Assert.Equal(2, end);
        Show(three, new KeyScript().Press(ConsoleKey.End).Press(ConsoleKey.Home).Enter(), AnsiAscii, out int home);
        Assert.Equal(0, home);
    }

    [Fact]
    public void Ascii_ShowsTitleMessageLinesAndSelectedButton()
    {
        string output = Show(YesNo("Line 1\r\nLine 2"), new KeyScript().Press(ConsoleKey.RightArrow).Enter(), AnsiAscii, out _);
        Assert.Contains("Confirm", output);
        Assert.Contains("  Line 1", output);
        Assert.Contains("  Line 2", output);
        Assert.Contains("[ Yes ]", output);   // first frame
        Assert.Contains("[ No ]", output);    // after Right
        Assert.Contains("\u001b[7m", output); // reverse video highlight
    }

    [Fact]
    public void Summary_PrintsTitleAndButton()
    {
        string output = Show(YesNo(), new KeyScript().Press(ConsoleKey.RightArrow).Enter(), AnsiAscii, out _);
        Assert.EndsWith("No\u001b[0m" + Environment.NewLine, output);
        Assert.Contains("Confirm ", output);
    }

    [Fact]
    public void Validator_RejectsButton()
    {
        Dialog dialog = YesNo();
        dialog.Validator = i => i == 0 ? ValidationResult.Error("Not allowed.") : ValidationResult.Success();
        string output = Show(dialog, new KeyScript().Enter().Press(ConsoleKey.RightArrow).Enter(), AnsiAscii, out int result);
        Assert.Equal(1, result);
        Assert.Contains("! Not allowed.", output);
    }

    [Fact]
    public void NoMessageNoTitle_StillWorks()
    {
        var dialog = new Dialog();
        Show(dialog, new KeyScript().Enter(), AnsiAscii, out int result);
        Assert.Equal(0, result);
    }

    [Fact]
    public void InvalidConfiguration_Throws()
    {
        Dialog none = YesNo();
        none.Buttons.Clear();
        Assert.Throws<InvalidOperationException>(() => Show(none, new KeyScript(), AnsiAscii, out _));

        Dialog outOfRange = YesNo();
        outOfRange.DefaultButton = 2;
        Assert.Throws<InvalidOperationException>(() => Show(outOfRange, new KeyScript(), AnsiAscii, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Dialog().DefaultButton = -1);
        Assert.Throws<ArgumentNullException>(() => new Dialog().SetButtons(null!));
    }

    [Fact]
    public void Sixel_DrawsTitleMarkerAndIndentsMessage()
    {
        string output = Show(YesNo("Hi"), new KeyScript().Enter(), SixelEnv, out _);
        Assert.Contains("\u001bP", output, StringComparison.Ordinal);
        Assert.Contains("\u001b[4CHi", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Sixel_DrawsButtonsAsFacesWithLabelsOnFaceColor()
    {
        string output = Show(YesNo("Hi"), new KeyScript().Enter(), SixelEnv, out _);
        Assert.Contains("\u001b[38;2;255;255;255m\u001b[48;2;52;152;219mYes\u001b[0m", output, StringComparison.Ordinal);
        Assert.Contains("\u001b[38;2;255;255;255m\u001b[48;2;64;64;64mNo\u001b[0m", output, StringComparison.Ordinal);
        Assert.DoesNotContain("[ Yes ]", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Plain_NumbersButtonsAndEmptyUsesDefault()
    {
        string output = Show(YesNo(), new KeyScript().Enter(), Plain, out int result);
        Assert.Equal(0, result);
        Assert.Contains("  1) Yes", output);
        Assert.Contains("  2) No", output);
        Assert.DoesNotContain("\u001b", output, StringComparison.Ordinal);

        Show(YesNo(), new KeyScript().Type("2").Enter(), Plain, out int second);
        Assert.Equal(1, second);
    }

    [Fact]
    public void Plain_ReportsInvalidInputAndValidator()
    {
        Dialog dialog = YesNo();
        dialog.Validator = i => i == 0 ? ValidationResult.Error("Not allowed.") : ValidationResult.Success();
        string output = Show(dialog, new KeyScript().Type("x").Enter().Enter().Type("2").Enter(), Plain, out int result);
        Assert.Equal(1, result);
        Assert.Contains("! Enter a number between 1 and 2.", output);
        Assert.Contains("! Not allowed.", output);
    }
}
