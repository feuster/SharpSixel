using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class ButtonTests
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment AnsiAscii = new WidgetEnvironment(false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);

    private static Button Ok(ButtonState state = ButtonState.Normal) => new Button("OK") { State = state };

    [Theory]
    [InlineData(ButtonState.Normal, "[ OK ]")]
    [InlineData(ButtonState.Focused, "[>OK<]")]
    [InlineData(ButtonState.Pressed, "[#OK#]")]
    [InlineData(ButtonState.Disabled, "[-OK-]")]
    public void Ascii_DrawsStateMarkers(ButtonState state, string expected) =>
        Assert.Equal(expected, Ok(state).Render(Plain));

    [Fact]
    public void Ascii_CustomCharacters_AreUsed()
    {
        Button button = Ok(ButtonState.Focused);
        button.Style.AsciiBorderLeft = '(';
        button.Style.AsciiBorderRight = ')';
        button.Style.AsciiFocusLeft = '*';
        button.Style.AsciiFocusRight = '*';
        Assert.Equal("(*OK*)", button.Render(Plain));
    }

    [Fact]
    public void Ascii_MinWidth_CentersLabel()
    {
        Button button = Ok();
        button.Style.MinWidth = 10;
        Assert.Equal(10, button.Columns);
        Assert.Equal("[   OK   ]", button.Render(Plain));

        button.Label = "ABC";
        Assert.Equal("[  ABC   ]", button.Render(Plain));
    }

    [Fact]
    public void Ascii_PaddingAndControlCharacters()
    {
        var button = new Button("A\nB");
        Assert.Equal("[ A B ]", button.Render(Plain));
        button.Style.Padding = 2;
        Assert.Equal(9, button.Columns);
        Assert.Equal("[  A B  ]", button.Render(Plain));
    }

    [Fact]
    public void Ascii_EmptyLabel_DrawsEmptyButton() =>
        Assert.Equal("[  ]", new Button(string.Empty).Render(Plain));

    [Fact]
    public void Ascii_WithAnsi_ColorsStatesExceptNormal()
    {
        Assert.Equal("[ OK ]\u001b[K", Ok().Render(AnsiAscii));
        Assert.Equal("\u001b[38;2;52;152;219m[>OK<]\u001b[0m\u001b[K", Ok(ButtonState.Focused).Render(AnsiAscii));
        Assert.Equal("\u001b[38;2;41;128;185m[#OK#]\u001b[0m\u001b[K", Ok(ButtonState.Pressed).Render(AnsiAscii));
        Assert.Equal("\u001b[38;2;128;128;128m[-OK-]\u001b[0m\u001b[K", Ok(ButtonState.Disabled).Render(AnsiAscii));
    }

    [Fact]
    public void Ascii_WithoutAnsi_ContainsNoEscapes()
    {
        foreach (ButtonState state in new[] { ButtonState.Normal, ButtonState.Focused, ButtonState.Pressed, ButtonState.Disabled })
        {
            Assert.DoesNotContain('\u001b', Ok(state).Render(Plain));
        }
    }

    [Fact]
    public void GetRows_DependsOnMode()
    {
        Button button = Ok();
        button.Style.HeightRows = 3;
        Assert.Equal(1, button.GetRows(Plain));
        Assert.Equal(3, button.GetRows(SixelEnv));
    }

    [Fact]
    public void InvalidValues_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new Button(null!));
        Assert.Throws<ArgumentNullException>(() => new Button("x", null!));
        Assert.Throws<ArgumentNullException>(() => new Button("x").Label = null!);
        Assert.Throws<ArgumentNullException>(() => new Button("x").Render(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ButtonStyle().Padding = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ButtonStyle().MinWidth = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ButtonStyle().HeightRows = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ButtonStyle().HeightRows = 11);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ButtonStyle().CornerRadius = -1);
    }

    [Fact]
    public void Image_HasCellBasedSize()
    {
        SixelImage image = Ok().CreateImage(SixelEnv);
        Assert.Equal(60, image.Width);
        Assert.Equal(18, image.Height);
    }

    [Fact]
    public void Image_FaceColorFollowsState()
    {
        Button button = Ok();
        Assert.Equal(button.Style.NormalColor, button.CreateImage(SixelEnv).GetPixel(30, 9));
        button.State = ButtonState.Focused;
        Assert.Equal(button.Style.FocusedColor, button.CreateImage(SixelEnv).GetPixel(30, 9));
        button.State = ButtonState.Pressed;
        Assert.Equal(button.Style.PressedColor, button.CreateImage(SixelEnv).GetPixel(30, 9));
        button.State = ButtonState.Disabled;
        Assert.Equal(button.Style.DisabledColor, button.CreateImage(SixelEnv).GetPixel(30, 9));
    }

    [Fact]
    public void Image_Corners_AreRoundedOrSquare()
    {
        Button button = Ok();
        Assert.Equal(0, button.CreateImage(SixelEnv).GetPixel(0, 0).A);
        button.Style.CornerRadius = 0;
        Assert.Equal(button.Style.NormalColor, button.CreateImage(SixelEnv).GetPixel(0, 0));
    }

    [Fact]
    public void Image_OpaqueBackground_FillsCorners()
    {
        Button button = Ok();
        button.Style.BackgroundColor = new SixelColor(1, 2, 3);
        Assert.Equal(new SixelColor(1, 2, 3), button.CreateImage(SixelEnv).GetPixel(0, 0));
    }

    [Fact]
    public void SixelFrame_ContainsGraphicAndCenteredLabelOnFace()
    {
        string frame = Ok().Render(SixelEnv);
        Assert.StartsWith("\u001b7", frame, StringComparison.Ordinal);
        Assert.EndsWith("\u001b8", frame, StringComparison.Ordinal);
        Assert.Contains("\u001bP", frame, StringComparison.Ordinal);
        Assert.Contains("\u001b[6X", frame, StringComparison.Ordinal);
        Assert.Contains("\u001b[2C\u001b[38;2;255;255;255m\u001b[48;2;64;64;64mOK\u001b[0m\u001b8", frame, StringComparison.Ordinal);
    }

    [Fact]
    public void SixelFrame_DisabledUsesDisabledColors()
    {
        Assert.Contains(
            "\u001b[38;2;128;128;128m\u001b[48;2;40;40;40mOK",
            Ok(ButtonState.Disabled).Render(SixelEnv),
            StringComparison.Ordinal);
    }

    [Fact]
    public void SixelFrame_OpaqueBackground_SkipsErase()
    {
        Button button = Ok();
        button.Style.BackgroundColor = new SixelColor(0, 0, 0);
        Assert.DoesNotContain("[6X", button.Render(SixelEnv), StringComparison.Ordinal);
    }

    [Fact]
    public void SixelFrame_MultiRow_CentersLabelRow()
    {
        Button button = Ok();
        button.Style.HeightRows = 3;
        Assert.Contains("\u001b[1B\u001b[2C\u001b[38;2;", button.Render(SixelEnv), StringComparison.Ordinal);
    }

    [Fact]
    public void SixelFrame_EmptyLabel_DrawsNoText() =>
        Assert.DoesNotContain("\u001b[38;2;", new Button(string.Empty).Render(SixelEnv), StringComparison.Ordinal);
}
