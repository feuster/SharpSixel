using System;
using System.Text;

namespace SharpSixel.Ui;

/// <summary>
/// Button with a label and a visual <see cref="State"/> (normal, focused, pressed, disabled). Drawn as sixel graphic
/// (rounded face in the color of the state, label centered as terminal text on the face) or as ASCII
/// (<c>[ OK ]</c>, with state markers: <c>[&gt;OK&lt;]</c> focused, <c>[#OK#]</c> pressed, <c>[-OK-]</c> disabled).
/// The button only draws; key handling and click actions belong to the caller, who changes <see cref="State"/> between
/// frames (see <see cref="Dialog"/> for an interactive row of buttons). The label should fit on one terminal row.
/// </summary>
public sealed class Button : Widget
{
    private const int TransparentAlphaLimit = 128;
    private string _label;

    /// <summary>Creates a button with the default style.</summary>
    /// <param name="label">Button text.</param>
    public Button(string label)
        : this(label, new ButtonStyle())
    {
    }

    /// <summary>Creates a button with the given style.</summary>
    /// <param name="label">Button text.</param>
    /// <param name="style">Look of the button.</param>
    public Button(string label, ButtonStyle style)
    {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    /// <summary>Look of the button; may be changed between frames.</summary>
    public ButtonStyle Style { get; }

    /// <summary>Button text; control characters are drawn as spaces.</summary>
    public string Label
    {
        get => _label;
        set => _label = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Visual state; may be changed between frames. Default <see cref="ButtonState.Normal"/>.</summary>
    public ButtonState State { get; set; }

    /// <summary>Width of the button in terminal cells: the label plus padding and edges, at least <see cref="ButtonStyle.MinWidth"/>.</summary>
    public int Columns => Math.Max(Style.MinWidth, TextFit.Width(PromptRow.Clean(_label)) + (2 * (Style.Padding + 1)));

    /// <summary>
    /// True if a position (column and row relative to the origin of the button, for example from
    /// <see cref="LiveRegion.TryGetLocal"/>) lies on the button. Use it to turn mouse events into clicks.
    /// </summary>
    public bool Contains(WidgetEnvironment environment, int column, int row)
    {
        if (environment == null)
        {
            throw new ArgumentNullException(nameof(environment));
        }

        return column >= 0 && column < Columns && row >= 0 && row < GetRows(environment);
    }

    /// <inheritdoc />
    protected override int SixelRows => Style.HeightRows;

    /// <inheritdoc />
    protected override string RenderAscii(WidgetEnvironment environment)
    {
        string label = PromptRow.Clean(_label);
        int inner = Columns - 2;
        string[] cells = LayOut(label, inner);
        char left = ' ';
        char right = ' ';
        switch (State)
        {
            case ButtonState.Focused:
                left = Style.AsciiFocusLeft;
                right = Style.AsciiFocusRight;
                break;
            case ButtonState.Pressed:
                left = right = Style.AsciiPressedMarker;
                break;
            case ButtonState.Disabled:
                left = right = Style.AsciiDisabledMarker;
                break;
        }

        SetCell(cells, 0, left);
        SetCell(cells, inner - 1, right);
        string text = Style.AsciiBorderLeft + string.Concat(cells) + Style.AsciiBorderRight;
        string frame = State == ButtonState.Normal
            ? text
            : AnsiCodes.Colorize(text, AsciiColor(), environment.UseAnsi);
        return environment.UseAnsi ? frame + AnsiCodes.EraseLineRest : frame;
    }

    /// <inheritdoc />
    protected override string RenderSixel(WidgetEnvironment environment)
    {
        string label = PromptRow.Clean(_label);
        int columns = Columns;
        int textRow = (Style.HeightRows - 1) / 2;
        var sb = new StringBuilder(AnsiCodes.SaveCursor);
        if (Style.BackgroundColor.A < TransparentAlphaLimit)
        {
            for (int row = 0; row < Style.HeightRows; row++)
            {
                sb.Append(AnsiCodes.Down(row)).Append(AnsiCodes.EraseChars(columns)).Append(AnsiCodes.RestoreCursor);
            }
        }

        sb.Append(new SixelEncoder().Encode(CreateImage(environment))).Append(AnsiCodes.RestoreCursor);
        if (label.Length > 0)
        {
            SixelColor text = State == ButtonState.Disabled ? Style.DisabledTextColor : Style.TextColor;
            sb.Append(AnsiCodes.Down(textRow)).Append(AnsiCodes.Forward((columns - TextFit.Width(label)) / 2))
                .Append(AnsiCodes.Foreground(text)).Append(AnsiCodes.Background(Style.FaceColor(State)))
                .Append(label).Append(AnsiCodes.Reset).Append(AnsiCodes.RestoreCursor);
        }

        return sb.ToString();
    }

    /// <summary>The label centered in <paramref name="inner"/> cells; a wide character takes its cell and an empty continuation cell.</summary>
    private static string[] LayOut(string label, int inner)
    {
        var cells = new string[inner];
        for (int i = 0; i < inner; i++)
        {
            cells[i] = " ";
        }

        int position = Math.Max(0, (inner - TextFit.Width(label)) / 2);
        for (int i = 0; i < label.Length && position < inner; i += char.IsHighSurrogate(label[i]) && i + 1 < label.Length ? 2 : 1)
        {
            string character = label.Substring(i, char.IsHighSurrogate(label[i]) && i + 1 < label.Length ? 2 : 1);
            int cellCount = TextFit.Width(character);
            if (cellCount == 0)
            {
                if (position > 0)
                {
                    cells[position - 1] += character;
                }

                continue;
            }

            if (position + cellCount > inner)
            {
                break;
            }

            cells[position] = character;
            if (cellCount == 2)
            {
                cells[position + 1] = string.Empty;
            }

            position += cellCount;
        }

        return cells;
    }

    /// <summary>Puts a marker into a cell; a wide character it cuts in half is replaced by a space.</summary>
    private static void SetCell(string[] cells, int index, char marker)
    {
        if (cells[index].Length == 0 && index > 0)
        {
            cells[index - 1] = " ";
        }
        else if (index + 1 < cells.Length && cells[index + 1].Length == 0)
        {
            cells[index + 1] = " ";
        }

        cells[index] = marker.ToString();
    }

    internal SixelImage CreateImage(WidgetEnvironment environment)
    {
        int width = Columns * environment.CellWidth;
        int height = environment.GetImageHeight(Style.HeightRows);
        var image = new SixelImage(width, height);
        if (Style.BackgroundColor.A > 0)
        {
            Raster.Fill(image, Style.BackgroundColor);
        }

        Raster.FillRoundedRect(image, 0, 0, width, height, Style.CornerRadius, Style.FaceColor(State));
        return image;
    }

    private SixelColor AsciiColor()
    {
        switch (State)
        {
            case ButtonState.Focused: return Style.FocusedColor;
            case ButtonState.Pressed: return Style.PressedColor;
            default: return Style.DisabledTextColor;
        }
    }
}
