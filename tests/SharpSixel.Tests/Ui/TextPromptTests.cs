using System.Text.RegularExpressions;
using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class TextPromptTests
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment AnsiAscii = new WidgetEnvironment(false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);

    private static string Show(TextPrompt prompt, KeyScript keys, WidgetEnvironment env, out string result)
    {
        var writer = new StringWriter();
        result = prompt.Show(writer, env, keys.Read);
        return writer.ToString();
    }

    [Fact]
    public void TypedTextIsReturned()
    {
        Show(new TextPrompt { Title = "Name" }, new KeyScript().Type("Bob Ross").Enter(), AnsiAscii, out string result);
        Assert.Equal("Bob Ross", result);
    }

    [Fact]
    public void Backspace_RemovesLastCharacter()
    {
        Show(new TextPrompt(), new KeyScript().Type("abc").Backspace().Backspace().Type("x").Enter(), AnsiAscii, out string result);
        Assert.Equal("ax", result);
        Show(new TextPrompt(), new KeyScript().Backspace().Type("a").Enter(), AnsiAscii, out string leading);
        Assert.Equal("a", leading);
    }

    [Fact]
    public void Empty_IsRejectedUnlessAllowedOrDefault()
    {
        string output = Show(new TextPrompt(), new KeyScript().Enter().Type("a").Enter(), AnsiAscii, out string result);
        Assert.Equal("a", result);
        Assert.Contains("! A value is required.", output);

        Show(new TextPrompt { AllowEmpty = true }, new KeyScript().Enter(), AnsiAscii, out string empty);
        Assert.Equal(string.Empty, empty);

        string withDefault = Show(new TextPrompt { Title = "Name", DefaultValue = "Bob" }, new KeyScript().Enter(), AnsiAscii, out string value);
        Assert.Equal("Bob", value);
        Assert.Contains("Name (Bob) ", withDefault);
    }

    [Fact]
    public void TypedTextWinsOverDefault()
    {
        Show(new TextPrompt { DefaultValue = "Bob" }, new KeyScript().Type("Al").Enter(), AnsiAscii, out string result);
        Assert.Equal("Al", result);
    }

    [Fact]
    public void MinLength_ReportsAndRetries()
    {
        string output = Show(new TextPrompt { MinLength = 3 }, new KeyScript().Type("ab").Enter().Type("c").Enter(), AnsiAscii, out string result);
        Assert.Equal("abc", result);
        Assert.Contains("! Enter at least 3 characters.", output);
    }

    [Fact]
    public void MaxLength_StopsTyping()
    {
        Show(new TextPrompt { MaxLength = 3 }, new KeyScript().Type("abcdef").Enter(), AnsiAscii, out string result);
        Assert.Equal("abc", result);
    }

    [Fact]
    public void LongDefault_IsValidatedAgainstMaxLength()
    {
        string output = Show(
            new TextPrompt { DefaultValue = "toolong", MaxLength = 3 }, new KeyScript().Enter().Type("ok").Enter(), AnsiAscii, out string result);
        Assert.Equal("ok", result);
        Assert.Contains("! Enter at most 3 characters.", output);
    }

    [Fact]
    public void Pattern_UsesCustomOrDefaultMessage()
    {
        var digits = new Regex("^[0-9]+$");
        string custom = Show(
            new TextPrompt { Pattern = digits, PatternMessage = "Digits only." },
            new KeyScript().Type("a").Enter().Backspace().Type("1").Enter(),
            AnsiAscii,
            out string result);
        Assert.Equal("1", result);
        Assert.Contains("! Digits only.", custom);

        string standard = Show(new TextPrompt { Pattern = digits }, new KeyScript().Type("a").Enter().Backspace().Type("1").Enter(), AnsiAscii, out _);
        Assert.Contains("! The input has an invalid format.", standard);
    }

    [Fact]
    public void CustomValidator_RunsAfterBuiltInRules()
    {
        var prompt = new TextPrompt
        {
            MinLength = 2,
            Validator = v => v == "no" ? ValidationResult.Error("reserved") : ValidationResult.Success(),
        };
        string output = Show(prompt, new KeyScript().Type("no").Enter().Type("t").Enter(), AnsiAscii, out string result);
        Assert.Equal("not", result);
        Assert.Contains("! reserved", output);
    }

    [Fact]
    public void Secret_ShowsMaskButReturnsText()
    {
        string output = Show(new TextPrompt { Title = "Password", IsSecret = true, DefaultValue = "hidden" }, new KeyScript().Type("pw1").Enter(), AnsiAscii, out string result);
        Assert.Equal("pw1", result);
        Assert.DoesNotContain("pw1", output);
        Assert.DoesNotContain("hidden", output);
        Assert.Contains("***", output);
    }

    [Fact]
    public void Caret_IsReverseVideoOrUnderscore()
    {
        Assert.Contains("\u001b[7m \u001b[27m", Show(new TextPrompt(), new KeyScript().Type("a").Enter(), AnsiAscii, out _));
    }

    [Fact]
    public void Sixel_DrawsMarkerGraphic()
    {
        string output = Show(new TextPrompt { Title = "Name" }, new KeyScript().Type("a").Enter(), SixelEnv, out _);
        Assert.Contains("\u001bP", output);
        Assert.Contains("\u001b[4C", output);
    }

    [Fact]
    public void Plain_ReadsLineAndReportsErrors()
    {
        var prompt = new TextPrompt { Title = "Name", MinLength = 2 };
        string output = Show(prompt, new KeyScript().Type("a").Enter().Type("bc").Enter(), Plain, out string result);
        Assert.Equal("bc", result);
        Assert.Contains("Name ", output);
        Assert.Contains("! Enter at least 2 characters.", output);
        Assert.DoesNotContain("\u001b", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Plain_SecretEchoesMaskAndDefaultApplies()
    {
        string output = Show(new TextPrompt { IsSecret = true }, new KeyScript().Type("ab").Enter(), Plain, out string result);
        Assert.Equal("ab", result);
        Assert.DoesNotContain("ab", output);
        Assert.Contains("**", output);

        Show(new TextPrompt { DefaultValue = "Bob" }, new KeyScript().Enter(), Plain, out string value);
        Assert.Equal("Bob", value);
    }

    [Fact]
    public void InvalidConfiguration_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => Show(new TextPrompt { MinLength = 5, MaxLength = 3 }, new KeyScript(), AnsiAscii, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextPrompt().MinLength = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextPrompt().MaxLength = 0);
    }
}
