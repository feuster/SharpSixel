using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class PromptViewTests
{
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);
    private static readonly SixelColor Blue = new SixelColor(52, 152, 219);
    private static readonly SixelColor Gray = new SixelColor(149, 165, 166);

    // The legacy marker-only look: no frame, no highlight bar.
    private static PromptStyle Flat() => new PromptStyle { ShowFrame = false, HighlightColor = SixelColor.Transparent };

    private static PromptView View(params PromptRow[] rows)
    {
        var view = new PromptView(Flat());
        view.Rows.AddRange(rows);
        return view;
    }

    private static PromptRow Marker(MarkerShape shape, SixelColor color, bool filled) =>
        new PromptRow { Shape = shape, MarkerColor = color, Filled = filled, Text = "x" };

    // Center pixel of the marker band of the given row (band = image height / rows).
    private static SixelColor Center(SixelImage image, int rows, int row) =>
        image.GetPixel(image.Width / 2, (int)((double)image.Height / rows * (row + 0.5)));

    [Fact]
    public void Ascii_JoinsRowsWithPrefixAndCaret()
    {
        PromptView view = View(
            new PromptRow { AsciiPrefix = "> ", Text = "one" },
            new PromptRow { Text = "in", Caret = true });
        Assert.Equal("> one\nin_", view.Render(Plain));
        Assert.Equal(2, view.GetRows(Plain));
        Assert.Equal(2, view.GetRows(SixelEnv));
    }

    [Fact]
    public void Ascii_Ansi_ColorsTextAndDrawsReverseCaret()
    {
        PromptView view = View(new PromptRow { Text = "a", TextColor = Blue, Caret = true }, new PromptRow());
        Assert.Equal("\u001b[38;2;52;152;219ma\u001b[0m\u001b[7m \u001b[27m\n", view.Render(new WidgetEnvironment(false)));
    }

    [Fact]
    public void RowText_ReplacesControlCharacters()
    {
        var row = new PromptRow { AsciiPrefix = "\t", Text = "a\r\nb\u001b[31m" };
        Assert.Equal(" ", row.AsciiPrefix);
        Assert.Equal("a  b [31m", row.Text);
    }

    [Fact]
    public void Image_SizeFollowsMarkerColumnsAndRows()
    {
        SixelImage image = View(
            Marker(MarkerShape.Radio, Blue, true), new PromptRow(), new PromptRow()).CreateImage(SixelEnv);
        Assert.Equal(2 * 10, image.Width);
        Assert.Equal(SixelEnv.GetImageHeight(3), image.Height);
    }

    [Fact]
    public void Image_Radio_FilledHasDotUnfilledHasHole()
    {
        SixelImage image = View(
            Marker(MarkerShape.Radio, Blue, true), Marker(MarkerShape.Radio, Gray, false)).CreateImage(SixelEnv);
        Assert.Equal(Blue, Center(image, 2, 0));
        Assert.Equal(0, Center(image, 2, 1).A);
        Assert.True(HasColor(image, 2, 1, Gray));
    }

    [Fact]
    public void Image_Check_FilledIsSolidUnfilledHasHole()
    {
        SixelImage image = View(
            Marker(MarkerShape.Check, Blue, true), Marker(MarkerShape.Check, Gray, false)).CreateImage(SixelEnv);
        Assert.Equal(Blue, Center(image, 2, 0));
        Assert.Equal(0, Center(image, 2, 1).A);
        Assert.True(HasColor(image, 2, 1, Gray));
    }

    [Fact]
    public void Image_Dot_IsSolidAndRowsWithoutMarkerStayTransparent()
    {
        SixelImage image = View(Marker(MarkerShape.Dot, Blue, false), new PromptRow()).CreateImage(SixelEnv);
        Assert.Equal(Blue, Center(image, 2, 0));
        Assert.False(HasColor(image, 2, 1, Blue));
        Assert.Equal(0, Center(image, 2, 1).A);
    }

    [Fact]
    public void Image_OpaqueBackground_FillsAndPunchesHoleWithBackground()
    {
        var style = Flat();
        style.BackgroundColor = new SixelColor(10, 20, 30);
        var view = new PromptView(style);
        view.Rows.Add(Marker(MarkerShape.Radio, Gray, false));
        SixelImage image = view.CreateImage(SixelEnv);
        Assert.Equal(style.BackgroundColor, image.GetPixel(0, 0));
        Assert.Equal(style.BackgroundColor, Center(image, 1, 0));
    }

    [Fact]
    public void Sixel_ErasesRowsDrawsGraphicOnceThenPositionsText()
    {
        PromptView view = View(new PromptRow { Text = "title" }, Marker(MarkerShape.Radio, Blue, true), new PromptRow());
        string frame = view.Render(SixelEnv);
        Assert.StartsWith("\u001b7", frame);
        Assert.Contains("\u001b[1B\u001b[K\u001b8", frame);
        Assert.Contains("\u001b[1B\u001b[3Cx\u001b8", frame);
        Assert.Equal(1, frame.Split(new[] { "\u001bP" }, StringSplitOptions.None).Length - 1);
        Assert.True(frame.IndexOf("\u001bP", StringComparison.Ordinal) < frame.IndexOf("title", StringComparison.Ordinal));
        Assert.EndsWith("\u001b8", frame);
    }

    [Fact]
    public void Highlight_IsReverseVideoWithAnsiAndIgnoredWithout()
    {
        var row = new PromptRow { Text = "a[ B ]c", HighlightStart = 1, HighlightLength = 5, HighlightColor = Blue };
        PromptView view = View(row);
        Assert.Equal(
            "a\u001b[38;2;52;152;219m\u001b[7m[ B ]\u001b[27m\u001b[0mc",
            view.Render(new WidgetEnvironment(false)));
        Assert.Equal("a[ B ]c", view.Render(Plain));
    }

    [Fact]
    public void Sixel_IndentRowStartsAtTextColumnWithoutGraphic()
    {
        string frame = View(new PromptRow { Text = "msg", Indent = true }).Render(SixelEnv);
        Assert.Contains("\u001b[3Cmsg", frame, StringComparison.Ordinal);
        Assert.DoesNotContain("\u001bP", frame, StringComparison.Ordinal);
    }

    [Fact]
    public void Sixel_WithoutMarkers_DrawsNoGraphic()
    {
        string frame = View(new PromptRow { Text = "a" }, new PromptRow()).Render(SixelEnv);
        Assert.DoesNotContain("\u001bP", frame, StringComparison.Ordinal);
    }

    private static int Count(SixelImage image, int left, int top, int size, Func<SixelColor, bool> match)
    {
        int count = 0;
        for (int y = top; y < top + size; y++)
        {
            for (int x = left; x < left + size; x++)
            {
                if (match(image.GetPixel(x, y)))
                {
                    count++;
                }
            }
        }

        return count;
    }

    [Fact]
    public void Frame_AddsRowsAndColumnsDrawsRoundedLineAndShiftsText()
    {
        var view = new PromptView(new PromptStyle { HighlightColor = SixelColor.Transparent });
        view.Rows.Add(Marker(MarkerShape.Radio, Blue, true));
        Assert.Equal(3, view.GetRows(SixelEnv));
        Assert.Equal(1, view.GetRows(Plain));
        Assert.Equal(2, PromptView.ExtraRows(view.Style, SixelEnv));
        Assert.Equal(0, PromptView.ExtraRows(view.Style, Plain));

        SixelImage image = view.CreateImage(SixelEnv);
        Assert.Equal(6 * 10, image.Width);
        Assert.Equal(SixelEnv.GetImageHeight(3), image.Height);
        Assert.Equal(view.Style.FrameColor, image.GetPixel(4, 30));
        Assert.Equal(view.Style.FrameColor, image.GetPixel(30, 49));
        Assert.Equal(view.Style.FrameColor, image.GetPixel(30, 9));
        Assert.Equal(0, image.GetPixel(0, 0).A);
        Assert.Equal(0, image.GetPixel(45, 45).A);
        Assert.Contains("\u001b[1B\u001b[4Cx", view.Render(SixelEnv), StringComparison.Ordinal);
        Assert.Single(view.Render(Plain).Split('\n'));
    }

    [Fact]
    public void Frame_OpaqueBackgroundFillsPanelAndPaintsTextOnIt()
    {
        var style = new PromptStyle { BackgroundColor = new SixelColor(10, 20, 30), HighlightColor = SixelColor.Transparent };
        var view = new PromptView(style);
        view.Rows.Add(Marker(MarkerShape.Radio, Gray, false));
        SixelImage image = view.CreateImage(SixelEnv);
        Assert.Equal(style.BackgroundColor, image.GetPixel(0, 0));
        Assert.Equal(style.BackgroundColor, image.GetPixel(45, 30));
        Assert.Equal(style.FrameColor, image.GetPixel(4, 30));
        Assert.Contains("\u001b[48;2;10;20;30mx\u001b[0m", view.Render(SixelEnv), StringComparison.Ordinal);
    }

    [Fact]
    public void Bar_FillsMarkerCellsAndPadsTextOnBarColor()
    {
        var style = Flat();
        style.HighlightColor = new SixelColor(38, 52, 70);
        var view = new PromptView(style);
        PromptRow current = Marker(MarkerShape.Radio, Blue, true);
        current.Text = "ab";
        current.TextColor = Blue;
        current.Bar = true;
        view.Rows.Add(current);
        view.Rows.Add(new PromptRow { Shape = MarkerShape.Radio, MarkerColor = Gray, Text = "abcdef" });

        SixelImage image = view.CreateImage(SixelEnv);
        Assert.Equal(3 * 10, image.Width);
        Assert.Equal(style.HighlightColor, image.GetPixel(1, 1));
        Assert.Equal(style.HighlightColor, image.GetPixel(25, 10));
        Assert.Equal(0, image.GetPixel(25, 30).A);
        Assert.Contains(
            "\u001b[48;2;38;52;70m\u001b[38;2;52;152;219mab    \u001b[0m", view.Render(SixelEnv), StringComparison.Ordinal);
    }

    [Fact]
    public void Bar_TransparentHighlightColorTurnsItOff()
    {
        PromptRow row = Marker(MarkerShape.Radio, Blue, true);
        row.Bar = true;
        PromptView view = View(row);
        Assert.Equal(2 * 10, view.CreateImage(SixelEnv).Width);
        Assert.DoesNotContain("\u001b[48;", view.Render(SixelEnv), StringComparison.Ordinal);
    }

    [Fact]
    public void Chips_DrawRoundedFacesAndLabelsOnFaceColor()
    {
        var style = Flat();
        var view = new PromptView(style);
        var row = new PromptRow { Text = "[ OK ]", Indent = true };
        row.Chips.Add(new PromptChip
        {
            Start = 0, Length = 6, LabelStart = 2, LabelLength = 2, Color = Blue, TextColor = new SixelColor(255, 255, 255),
        });
        view.Rows.Add(row);

        SixelImage image = view.CreateImage(SixelEnv);
        Assert.Equal(9 * 10, image.Width);
        Assert.Equal(Blue, image.GetPixel(60, 10));
        Assert.Equal(0, image.GetPixel(30, 0).A);
        Assert.Equal(0, image.GetPixel(10, 10).A);
        string frame = view.Render(SixelEnv);
        Assert.Contains(
            "\u001b[3C\u001b[2C\u001b[38;2;255;255;255m\u001b[48;2;52;152;219mOK\u001b[0m", frame, StringComparison.Ordinal);
        Assert.DoesNotContain("[ OK ]", frame, StringComparison.Ordinal);
    }

    [Fact]
    public void Image_Check_FilledCutsCheckMarkOutOfTheBox()
    {
        SixelImage image = View(
            Marker(MarkerShape.Check, Blue, true), Marker(MarkerShape.Check, Blue, false)).CreateImage(SixelEnv);
        int filledBox = Count(image, 2, 2, 16, c => c == Blue);
        int filledCut = Count(image, 4, 4, 12, c => c.A == 0);
        int emptyBlue = Count(image, 2, 20, 16, c => c == Blue);
        Assert.True(filledBox > 120);
        Assert.True(filledCut >= 12);
        Assert.True(emptyBlue > 0 && emptyBlue < filledBox);
    }

    private static bool HasColor(SixelImage image, int rows, int row, SixelColor color)
    {
        int top = (int)((double)image.Height / rows * row);
        int bottom = (int)((double)image.Height / rows * (row + 1));
        for (int y = top; y < bottom; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                if (image.GetPixel(x, y) == color)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
