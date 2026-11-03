using System;
using System.Text;

namespace SharpSixel.Ui;

/// <summary>
/// Separator line. Horizontal: one row, <see cref="Width"/> cells wide (default: the terminal width, 40 if unknown), with an
/// optional centered <see cref="Title"/>. Vertical: one cell wide and <see cref="Height"/> rows high, for example between
/// widgets of a <see cref="HorizontalLayout"/>. Sixel: thin pixel line (title cells without line, title as terminal text).
/// ASCII: <c>-----</c> / <c>--- Title ---</c> or <c>|</c> per row.
/// </summary>
public sealed class Rule : Widget
{
    private const int ColorAlphaLimit = 128;
    private const int DefaultWidth = 40;
    private const int TitleMargin = 4;
    private int? _width;
    private int? _height;

    /// <summary>Creates a rule with the default style.</summary>
    public Rule()
        : this(new RuleStyle())
    {
    }

    /// <summary>Creates a rule with the given style.</summary>
    public Rule(RuleStyle style)
    {
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    /// <summary>Look of the rule; may be changed between frames.</summary>
    public RuleStyle Style { get; }

    /// <summary>Direction of the line. Default <see cref="RuleOrientation.Horizontal"/>.</summary>
    public RuleOrientation Orientation { get; set; }

    /// <summary>Optional title in the middle of a horizontal rule; cut with an ellipsis if it does not fit. Control characters are drawn as spaces.</summary>
    public string? Title { get; set; }

    /// <summary>
    /// Length of a horizontal rule in cells (limited to the terminal width if known). Null (default) uses the terminal width,
    /// or 40 if it is unknown. Must be at least 1.
    /// </summary>
    public int? Width
    {
        get => _width;
        set
        {
            if (value.HasValue)
            {
                Guard.AtLeast(value.Value, 1, nameof(value));
            }

            _width = value;
        }
    }

    /// <summary>
    /// Length of a vertical rule in rows (at least 1). Null (default) stretches it to the height of the layout it is placed in,
    /// or 1 row if there is none.
    /// </summary>
    public int? Height
    {
        get => _height;
        set
        {
            if (value.HasValue)
            {
                Guard.AtLeast(value.Value, 1, nameof(value));
            }

            _height = value;
        }
    }

    private bool Vertical => Orientation == RuleOrientation.Vertical;

    /// <inheritdoc />
    protected override int SixelRows => Vertical ? _height ?? 1 : 1;

    /// <inheritdoc />
    protected override int AsciiRows => SixelRows;

    internal override bool FillsWidth => !Vertical && _width == null;

    /// <inheritdoc />
    protected override string RenderAscii(WidgetEnvironment environment)
    {
        bool ansi = environment.UseAnsi;
        if (Vertical)
        {
            string line = Tint(Style.AsciiVertical.ToString(), Style.Color, ansi);
            return Repeat(line, VerticalRows(environment));
        }

        int columns = Columns(environment);
        string label = Label(columns);
        char horizontal = Style.AsciiHorizontal;
        if (label.Length == 0)
        {
            return Tint(new string(horizontal, columns), Style.Color, ansi);
        }

        int left = (columns - TextFit.Width(label)) / 2;
        return Tint(new string(horizontal, left), Style.Color, ansi)
            + Tint(label, Style.TitleColor, ansi)
            + Tint(new string(horizontal, columns - TextFit.Width(label) - left), Style.Color, ansi);
    }

    /// <inheritdoc />
    protected override string RenderSixel(WidgetEnvironment environment)
    {
        int columns = Vertical ? 1 : Columns(environment);
        var sb = new StringBuilder(AnsiCodes.SaveCursor);
        int rows = Vertical ? VerticalRows(environment) : 1;
        for (int row = 0; row < rows; row++)
        {
            sb.Append(AnsiCodes.Down(row)).Append(AnsiCodes.EraseChars(columns)).Append(AnsiCodes.RestoreCursor);
        }

        sb.Append(new SixelEncoder().Encode(CreateImage(environment))).Append(AnsiCodes.RestoreCursor);
        string label = Vertical ? string.Empty : Label(columns);
        if (label.Length > 0)
        {
            string text = Style.TitleColor.A >= ColorAlphaLimit ? AnsiCodes.Foreground(Style.TitleColor) + label + AnsiCodes.Reset : label;
            sb.Append(AnsiCodes.Forward((columns - TextFit.Width(label)) / 2)).Append(text).Append(AnsiCodes.RestoreCursor);
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    protected override int MeasureColumns(WidgetEnvironment environment) => Vertical ? 1 : Columns(environment);

    internal SixelImage CreateImage(WidgetEnvironment environment)
    {
        int cellWidth = environment.CellWidth;
        int thickness = Style.LineThickness;
        if (Vertical)
        {
            var vertical = new SixelImage(cellWidth, environment.GetImageHeight(VerticalRows(environment)));
            Raster.FillRoundedRect(vertical, (cellWidth - thickness) / 2, 0, thickness, vertical.Height, 0, Style.Color);
            return vertical;
        }

        int columns = Columns(environment);
        var image = new SixelImage(columns * cellWidth, environment.GetImageHeight(1));
        int top = (image.Height - thickness) / 2;
        Raster.FillRoundedRect(image, 0, top, image.Width, thickness, 0, Style.Color);
        string label = Label(columns);
        if (label.Length > 0)
        {
            int start = (columns - TextFit.Width(label)) / 2;
            Raster.FillRoundedRect(image, start * cellWidth, top, TextFit.Width(label) * cellWidth, thickness, 0, SixelColor.Transparent);
        }

        return image;
    }

    private int VerticalRows(WidgetEnvironment environment) => _height ?? Math.Max(1, environment.Rows);

    private static string Repeat(string line, int count)
    {
        var lines = new string[count];
        for (int i = 0; i < count; i++)
        {
            lines[i] = line;
        }

        return string.Join("\n", lines);
    }

    private static string Tint(string text, SixelColor color, bool ansi) =>
        color.A >= ColorAlphaLimit ? AnsiCodes.Colorize(text, color, ansi) : text;

    private int Columns(WidgetEnvironment environment)
    {
        int width = _width ?? (environment.Columns > 0 ? environment.Columns : DefaultWidth);
        return environment.Columns > 0 ? Math.Min(width, environment.Columns) : width;
    }

    /// <summary>The title with a space on each side, or empty if there is none or no room (one line cell stays free on each side).</summary>
    private string Label(int columns)
    {
        string title = PromptRow.Clean(Title);
        int room = columns - TitleMargin;
        return title.Length > 0 && room > 0 ? " " + TextFit.Clip(title, room) + " " : string.Empty;
    }
}
