using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class MouseInputTests
{
    private static readonly WidgetEnvironment Ansi = new WidgetEnvironment(false);
    private static readonly WidgetEnvironment Sixel = new WidgetEnvironment(true, 10, 20);

    private static VtInputParser Parser(string text)
    {
        int index = 0;
        return new VtInputParser(
            () => index < text.Length ? text[index++] : -1,
            () => index < text.Length ? text[index++] : -1);
    }

    private static Func<InputEvent> Script(params InputEvent[] events)
    {
        var queue = new Queue<InputEvent>(events);
        return () => queue.Count > 0 ? queue.Dequeue() : throw new InvalidOperationException("The input script is exhausted.");
    }

    private static InputEvent Origin(int row = 5) => InputEvent.FromCursorPosition(row, 0);

    private static InputEvent Click(int column, int row, MouseButton button = MouseButton.Left, bool press = true) =>
        InputEvent.FromMouse(new MouseEvent(column, row, button, press));

    private static InputEvent Key(ConsoleKey key, char c = '\0') =>
        InputEvent.FromKey(new ConsoleKeyInfo(c, key, false, false, false));

    private static InputEvent Enter() => Key(ConsoleKey.Enter, '\r');

    // ---- VtInputParser ----

    [Fact]
    public void Parser_PlainKeys()
    {
        VtInputParser parser = Parser("a\r\t \u007f");
        InputEvent a = parser.Read();
        Assert.Equal(InputKind.Key, a.Kind);
        Assert.Equal('a', a.Key.KeyChar);
        Assert.Equal(ConsoleKey.Enter, parser.Read().Key.Key);
        Assert.Equal(ConsoleKey.Tab, parser.Read().Key.Key);
        Assert.Equal(ConsoleKey.Spacebar, parser.Read().Key.Key);
        Assert.Equal(ConsoleKey.Backspace, parser.Read().Key.Key);
    }

    [Fact]
    public void Parser_NavigationKeys()
    {
        VtInputParser parser = Parser("\u001b[A\u001b[B\u001b[C\u001b[D\u001b[H\u001b[F\u001b[3~\u001b[5~\u001b[6~\u001bOA\u001b[Z");
        ConsoleKey[] expected =
        {
            ConsoleKey.UpArrow, ConsoleKey.DownArrow, ConsoleKey.RightArrow, ConsoleKey.LeftArrow, ConsoleKey.Home,
            ConsoleKey.End, ConsoleKey.Delete, ConsoleKey.PageUp, ConsoleKey.PageDown, ConsoleKey.UpArrow, ConsoleKey.Tab,
        };
        foreach (ConsoleKey key in expected)
        {
            Assert.Equal(key, parser.Read().Key.Key);
        }
    }

    [Fact]
    public void Parser_LoneEscapeAndCtrlC()
    {
        Assert.Equal(ConsoleKey.Escape, Parser("\u001b").Read().Key.Key);
        ConsoleKeyInfo ctrlC = Parser("\u0003").Read().Key;
        Assert.Equal(ConsoleKey.C, ctrlC.Key);
        Assert.NotEqual((ConsoleModifiers)0, ctrlC.Modifiers & ConsoleModifiers.Control);
    }

    [Fact]
    public void Parser_CursorPositionReport_IsZeroBased()
    {
        InputEvent report = Parser("\u001b[12;5R").Read();
        Assert.Equal(InputKind.CursorPosition, report.Kind);
        Assert.Equal(11, report.Row);
        Assert.Equal(4, report.Column);
    }

    [Theory]
    [InlineData("\u001b[<0;10;5M", MouseButton.Left, true)]
    [InlineData("\u001b[<0;10;5m", MouseButton.Left, false)]
    [InlineData("\u001b[<1;10;5M", MouseButton.Middle, true)]
    [InlineData("\u001b[<2;10;5M", MouseButton.Right, true)]
    [InlineData("\u001b[<64;10;5M", MouseButton.WheelUp, true)]
    [InlineData("\u001b[<65;10;5M", MouseButton.WheelDown, true)]
    [InlineData("\u001b[<4;10;5M", MouseButton.Left, true)]
    public void Parser_SgrMouse(string text, MouseButton button, bool press)
    {
        InputEvent e = Parser(text).Read();
        Assert.Equal(InputKind.Mouse, e.Kind);
        Assert.Equal(9, e.Mouse!.Column);
        Assert.Equal(4, e.Mouse.Row);
        Assert.Equal(button, e.Mouse.Button);
        Assert.Equal(press, e.Mouse.IsPress);
    }

    [Fact]
    public void Parser_SkipsMotionAndUnknownSequences()
    {
        Assert.Equal('x', Parser("\u001b[<35;1;1Mx").Read().Key.KeyChar);
        Assert.Equal('q', Parser("\u001b[99zq").Read().Key.KeyChar);
    }

    [Fact]
    public void Parser_EndOfInput_Throws() =>
        Assert.Throws<EndOfStreamException>(() => Parser(string.Empty).Read());

    [Fact]
    public void Parser_NullFunctions_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new VtInputParser(null!, () => -1));
        Assert.Throws<ArgumentNullException>(() => new VtInputParser(() => -1, null!));
    }

    // ---- LiveRegion ----

    [Fact]
    public void LiveRegion_MapsMouseToLocalCoordinatesAndTurnsReportingOnAndOff()
    {
        var writer = new StringWriter();
        var region = new LiveRegion(writer, Ansi);
        region.Begin(3);
        region.EnableMouse(Script(Key(ConsoleKey.A, 'a'), InputEvent.FromCursorPosition(7, 2)));
        Assert.Contains("\u001b[?1000h\u001b[?1006h\u001b[6n", writer.ToString(), StringComparison.Ordinal);

        Assert.True(region.TryGetLocal(new MouseEvent(6, 8, MouseButton.Left, true), out int column, out int row));
        Assert.Equal(4, column);
        Assert.Equal(1, row);
        Assert.False(region.TryGetLocal(new MouseEvent(6, 10, MouseButton.Left, true), out _, out _));
        Assert.False(region.TryGetLocal(new MouseEvent(1, 8, MouseButton.Left, true), out _, out _));
        Assert.False(region.TryGetLocal(new MouseEvent(6, 6, MouseButton.Left, true), out _, out _));

        region.End();
        Assert.Contains("\u001b[?1006l\u001b[?1000l", writer.ToString(), StringComparison.Ordinal);
        Assert.False(region.TryGetLocal(new MouseEvent(6, 8, MouseButton.Left, true), out _, out _));
    }

    [Fact]
    public void LiveRegion_EnableMouse_ValidatesUsage()
    {
        var region = new LiveRegion(new StringWriter(), Ansi);
        Assert.Throws<ArgumentNullException>(() => region.EnableMouse(null!));
        Assert.Throws<InvalidOperationException>(() => region.EnableMouse(Script(Origin())));
        Assert.Throws<ArgumentNullException>(() => region.TryGetLocal(null!, out _, out _));
    }

    // ---- Button ----

    [Fact]
    public void Button_Contains_UsesColumnsAndRows()
    {
        var button = new Button("OK");
        button.Style.HeightRows = 2;
        Assert.True(button.Contains(Sixel, 0, 0));
        Assert.True(button.Contains(Sixel, 5, 1));
        Assert.False(button.Contains(Sixel, 6, 0));
        Assert.False(button.Contains(Sixel, 0, 2));
        Assert.False(button.Contains(Sixel, -1, 0));
        Assert.True(button.Contains(Ansi, 5, 0));
        Assert.False(button.Contains(Ansi, 0, 1));
        Assert.Throws<ArgumentNullException>(() => button.Contains(null!, 0, 0));
    }

    // ---- Dialog ----

    private static Dialog YesNo()
    {
        var dialog = new Dialog { Message = "Q" };
        dialog.SetButtons("Yes", "No");
        return dialog;
    }

    [Fact]
    public void Dialog_ClickOnButton_ChoosesIt()
    {
        var writer = new StringWriter();
        int result = YesNo().ShowWithMouse(writer, Ansi, Script(Origin(9), Click(12, 10)));
        Assert.Equal(1, result);
        Assert.Contains("\u001b[?1000h", writer.ToString(), StringComparison.Ordinal);
        Assert.Contains("\u001b[?1000l", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Dialog_ClickInSixelMode_UsesFrameAndMarkerOffset()
    {
        Assert.Equal(1, YesNo().ShowWithMouse(new StringWriter(), Sixel, Script(Origin(9), Click(14, 11))));
        Assert.Equal(0, YesNo().ShowWithMouse(new StringWriter(), Sixel, Script(Origin(9), Click(4, 11))));
    }

    [Fact]
    public void Dialog_ClickElsewhere_IsIgnored()
    {
        int result = YesNo().ShowWithMouse(
            new StringWriter(),
            Ansi,
            Script(
                Origin(9),
                Click(10, 10),                                   // gap between the buttons
                Click(12, 9),                                    // message row
                Click(12, 4),                                    // above the dialog
                Click(12, 10, MouseButton.Right),                // wrong button
                Click(12, 10, press: false),                     // release
                Enter()));
        Assert.Equal(0, result);
    }

    [Fact]
    public void Dialog_RejectedClick_ShowsErrorAndContinues()
    {
        Dialog dialog = YesNo();
        dialog.Validator = value => value == 1 ? ValidationResult.Error("No is not allowed.") : ValidationResult.Success();
        var writer = new StringWriter();
        int result = dialog.ShowWithMouse(writer, Ansi, Script(Origin(9), Click(12, 10), Click(3, 10)));
        Assert.Equal(0, result);
        Assert.Contains("No is not allowed.", writer.ToString(), StringComparison.Ordinal);
    }

    // ---- SelectionPrompt / MultiSelectionPrompt ----

    private static SelectionPrompt<string> Letters()
    {
        var prompt = new SelectionPrompt<string>();
        prompt.AddChoices(new[] { "A", "B", "C" });
        return prompt;
    }

    [Fact]
    public void Selection_ClickChoosesItem() =>
        Assert.Equal("B", Letters().ShowWithMouse(new StringWriter(), Ansi, Script(Origin(), Click(3, 6))));

    [Fact]
    public void Selection_ClickWithTitleAccountsForTitleRow()
    {
        SelectionPrompt<string> prompt = Letters();
        prompt.Title = "Pick";
        Assert.Equal("A", prompt.ShowWithMouse(new StringWriter(), Ansi, Script(Origin(), Click(3, 6))));
        Assert.Equal("C", prompt.ShowWithMouse(new StringWriter(), Ansi, Script(Origin(), Click(3, 8))));
    }

    [Fact]
    public void Selection_ClickOutsideItems_IsIgnored() =>
        Assert.Equal("A", Letters().ShowWithMouse(new StringWriter(), Ansi, Script(Origin(), Click(3, 8), Click(3, 4), Enter())));

    [Fact]
    public void Selection_WheelMovesCursorOnlyInsideRegion() =>
        Assert.Equal(
            "B",
            Letters().ShowWithMouse(
                new StringWriter(),
                Ansi,
                Script(
                    Origin(),
                    Click(3, 0, MouseButton.WheelDown),
                    Click(3, 5, MouseButton.WheelDown),
                    Enter())));

    [Fact]
    public void Selection_RejectedClick_ShowsErrorAndContinues()
    {
        SelectionPrompt<string> prompt = Letters();
        prompt.Validator = value => value == "B" ? ValidationResult.Error("No B.") : ValidationResult.Success();
        var writer = new StringWriter();
        Assert.Equal("C", prompt.ShowWithMouse(writer, Ansi, Script(Origin(), Click(3, 6), Click(3, 7))));
        Assert.Contains("No B.", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void MultiSelection_ClickTogglesItemsAndEnterConfirms()
    {
        var prompt = new MultiSelectionPrompt<string>();
        prompt.AddChoice("A");
        prompt.AddChoice("B", selected: true);
        prompt.AddChoice("C");
        IReadOnlyList<string> result = prompt.ShowWithMouse(
            new StringWriter(), Ansi, Script(Origin(), Click(3, 5), Click(3, 7), Click(3, 6), Enter()));
        Assert.Equal(new[] { "A", "C" }, result);
    }

    // ---- no mouse without the input overload ----

    [Fact]
    public void KeyOnlyShow_DoesNotEnableMouse()
    {
        var writer = new StringWriter();
        var keys = new Queue<ConsoleKeyInfo>();
        keys.Enqueue(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
        Assert.Equal("A", Letters().Show(writer, Ansi, keys.Dequeue));
        Assert.DoesNotContain("?1000h", writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void InputShow_Plain_ReadsOnlyKeys()
    {
        var prompt = Letters();
        var events = Script(Click(1, 1), Key(ConsoleKey.D2, '2'), Enter());
        Assert.Equal("B", prompt.ShowWithMouse(new StringWriter(), new WidgetEnvironment(false, useAnsi: false), events));
    }
}
