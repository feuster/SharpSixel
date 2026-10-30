using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class PromptSupportTests
{
    private static ConsoleKeyInfo Key(ConsoleKey key) => new ConsoleKeyInfo('\0', key, false, false, false);

    [Fact]
    public void ValidationResult_SuccessAndError()
    {
        Assert.True(ValidationResult.Success().IsValid);
        Assert.Null(ValidationResult.Success().Message);
        ValidationResult error = ValidationResult.Error("bad");
        Assert.False(error.IsValid);
        Assert.Equal("bad", error.Message);
        Assert.Throws<ArgumentException>(() => ValidationResult.Error(" "));
    }

    [Fact]
    public void PromptStyle_RejectsEmptyMarkersAndFormatsPrefix()
    {
        var style = new PromptStyle();
        Assert.Throws<ArgumentException>(() => style.AsciiRadioOn = string.Empty);
        Assert.Throws<ArgumentNullException>(() => style.AsciiCheckOff = null!);
        Assert.Equal("> (*) ", style.AsciiPrefix(true, style.AsciiRadioOn));
        Assert.Equal("  [ ] ", style.AsciiPrefix(false, style.AsciiCheckOff));
    }

    [Fact]
    public void ChoiceNavigator_ScrollsWindowWithCursor()
    {
        var navigator = new ChoiceNavigator();
        navigator.Reset(5, 2);
        Assert.Equal(2, navigator.Visible);

        navigator.Handle(Key(ConsoleKey.DownArrow));
        Assert.Equal((1, 0), (navigator.Cursor, navigator.Top));
        navigator.Handle(Key(ConsoleKey.DownArrow));
        Assert.Equal((2, 1), (navigator.Cursor, navigator.Top));
        navigator.Handle(Key(ConsoleKey.End));
        Assert.Equal((4, 3), (navigator.Cursor, navigator.Top));
        navigator.Handle(Key(ConsoleKey.DownArrow));
        Assert.Equal((0, 0), (navigator.Cursor, navigator.Top));
        navigator.Handle(Key(ConsoleKey.UpArrow));
        Assert.Equal((4, 3), (navigator.Cursor, navigator.Top));
        navigator.Handle(Key(ConsoleKey.Home));
        Assert.Equal((0, 0), (navigator.Cursor, navigator.Top));
    }

    [Fact]
    public void ChoiceNavigator_IgnoresOtherKeysAndShortLists()
    {
        var navigator = new ChoiceNavigator();
        navigator.Reset(2, 10);
        Assert.Equal(2, navigator.Visible);
        Assert.False(navigator.Handle(Key(ConsoleKey.Enter)));
        navigator.Handle(Key(ConsoleKey.PageDown));
        Assert.Equal((1, 0), (navigator.Cursor, navigator.Top));
    }

    [Fact]
    public void LineReader_EchoesBackspacesAndMasks()
    {
        var keys = new KeyScript().Type("abc").Backspace().Type("d").Enter();
        var writer = new StringWriter();
        Assert.Equal("abd", LineReader.ReadLine(writer, keys.Read, null));
        Assert.Equal("abc\b \bd" + Environment.NewLine, writer.ToString());

        var masked = new StringWriter();
        Assert.Equal("xy", LineReader.ReadLine(masked, new KeyScript().Type("xy").Enter().Read, '*'));
        Assert.Equal("**" + Environment.NewLine, masked.ToString());
    }

    [Fact]
    public void LineReader_IgnoresControlKeysAndLeadingBackspace()
    {
        var keys = new KeyScript().Backspace().Up().Type("a").Enter();
        Assert.Equal("a", LineReader.ReadLine(new StringWriter(), keys.Read, null));
    }
}
