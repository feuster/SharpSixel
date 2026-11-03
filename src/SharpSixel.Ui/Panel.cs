using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SharpSixel.Ui;

/// <summary>
/// Framed box around lines of text with an optional title in the top edge. Sixel: the frame is one rounded outline
/// graphic (inside optionally filled), title and lines are terminal text. ASCII: frame of <c>+ - |</c>.
/// Both modes use the same rows: top edge, one row per line, bottom edge. The number of lines must not grow beyond the
/// rows reserved in a <see cref="LiveRegion"/> (<see cref="Widget.GetRows"/> at <see cref="LiveRegion.Begin"/>); a fixed
/// <see cref="Width"/> keeps the size stable while the lines change. Text is assumed to occupy one cell per char.
/// </summary>
public sealed class Panel : Widget
{
    private const int ColorAlphaLimit = 128;
    private const int TitleMargin = 4;
    private readonly Collection<string> _lines = new LineCollection();
    private int? _width;

    /// <summary>Creates a panel with the default style.</summary>
    public Panel()
        : this(new PanelStyle())
    {
    }

    /// <summary>Creates a panel with the given style.</summary>
    public Panel(PanelStyle style)
    {
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    /// <summary>Look of the panel; may be changed between frames.</summary>
    public PanelStyle Style { get; }

    /// <summary>Optional title shown in the top edge; cut with an ellipsis if it does not fit. Control characters are drawn as spaces.</summary>
    public string? Title { get; set; }

    /// <summary>
    /// Width of the text area in cells (without padding and frame); longer lines are cut with an ellipsis.
    /// Null (default) fits the widest line or the title. Must be at least 1.
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

    /// <summary>The text lines, one terminal row each (null lines are rejected; control characters are drawn as spaces).</summary>
    public IList<string> Lines => _lines;

    /// <summary>Adds a line (change <see cref="Lines"/> entries later to update the panel).</summary>
    public void AddLine(string text) => _lines.Add(text);

    private int Rows => 2 + _lines.Count;

    /// <inheritdoc />
    protected override int SixelRows => Rows;

    /// <inheritdoc />
    protected override int AsciiRows => Rows;

    /// <inheritdoc />
    protected override string RenderAscii(WidgetEnvironment environment)
    {
        Layout layout = BuildLayout(environment.Columns);
        bool ansi = environment.UseAnsi;
        string vertical = Tint(Style.AsciiVertical.ToString(), Style.BorderColor, ansi);
        string padding = new string(' ', Style.Padding);
        var lines = new List<string> { AsciiTop(layout, ansi) };
        foreach (string line in _lines)
        {
            lines.Add(vertical + padding + CellText(layout, line) + padding + vertical);
        }

        string corner = Style.AsciiCorner.ToString();
        lines.Add(Tint(corner + new string(Style.AsciiHorizontal, layout.Inner) + corner, Style.BorderColor, ansi));
        return string.Join("\n", lines);
    }

    /// <inheritdoc />
    protected override string RenderSixel(WidgetEnvironment environment)
    {
        Layout layout = BuildLayout(environment.Columns);
        var sb = new StringBuilder(AnsiCodes.SaveCursor);
        for (int row = 0; row < Rows; row++)
        {
            sb.Append(AnsiCodes.Down(row)).Append(AnsiCodes.EraseChars(layout.Total)).Append(AnsiCodes.Forward(layout.Total))
                .Append(AnsiCodes.EraseLineRest).Append(AnsiCodes.RestoreCursor);
        }

        sb.Append(new SixelEncoder().Encode(CreateImage(environment))).Append(AnsiCodes.RestoreCursor);
        if (layout.Title.Length > 0)
        {
            sb.Append(AnsiCodes.Forward(2)).Append(Paint(" " + layout.Title + " ", Style.TitleColor, SixelColor.Transparent))
                .Append(AnsiCodes.RestoreCursor);
        }

        if (layout.Content > 0)
        {
            for (int i = 0; i < _lines.Count; i++)
            {
                sb.Append(AnsiCodes.Down(1 + i)).Append(AnsiCodes.Forward(1 + Style.Padding))
                    .Append(Paint(CellText(layout, _lines[i]), SixelColor.Transparent, Style.BackgroundColor))
                    .Append(AnsiCodes.RestoreCursor);
            }
        }

        return sb.ToString();
    }

    internal SixelImage CreateImage(WidgetEnvironment environment)
    {
        Layout layout = BuildLayout(environment.Columns);
        int cellWidth = environment.CellWidth;
        int width = layout.Total * cellWidth;
        int height = environment.GetImageHeight(Rows);
        var image = new SixelImage(width, height);
        double band = (double)height / Rows;
        int thickness = Style.LineThickness;
        int half = thickness / 2;
        int top = (int)Math.Round(0.5 * band, MidpointRounding.AwayFromZero) - half;
        int bottom = (int)Math.Round((Rows - 0.5) * band, MidpointRounding.AwayFromZero) - half + thickness;
        int left = (int)Math.Round(0.5 * cellWidth, MidpointRounding.AwayFromZero) - half;
        int right = (int)Math.Round((layout.Total - 0.5) * cellWidth, MidpointRounding.AwayFromZero) - half + thickness;
        int radius = Style.CornerRadius;
        Raster.FillRoundedRect(image, left, top, right - left, bottom - top, radius, Style.BorderColor);
        Raster.FillRoundedRect(
            image,
            left + thickness,
            top + thickness,
            right - left - (2 * thickness),
            bottom - top - (2 * thickness),
            Math.Max(0, radius - thickness),
            Style.BackgroundColor);
        if (layout.Title.Length > 0)
        {
            Raster.FillRoundedRect(image, 2 * cellWidth, top, (TextFit.Width(layout.Title) + 2) * cellWidth, thickness, 0, SixelColor.Transparent);
        }

        return image;
    }

    private static string Tint(string text, SixelColor color, bool ansi) =>
        color.A >= ColorAlphaLimit ? AnsiCodes.Colorize(text, color, ansi) : text;

    private static string Paint(string text, SixelColor foreground, SixelColor background)
    {
        string prefix = (foreground.A >= ColorAlphaLimit ? AnsiCodes.Foreground(foreground) : string.Empty)
            + (background.A >= ColorAlphaLimit ? AnsiCodes.Background(background) : string.Empty);
        return prefix.Length == 0 ? text : prefix + text + AnsiCodes.Reset;
    }

    private static string Fit(string text, int width) => TextFit.Clip(text, width);

    private static string CellText(Layout layout, string line) =>
        TextFit.PadRight(Fit(PromptRow.Clean(line), layout.Content), layout.Content);

    private string AsciiTop(Layout layout, bool ansi)
    {
        char horizontal = Style.AsciiHorizontal;
        string corner = Style.AsciiCorner.ToString();
        if (layout.Title.Length == 0)
        {
            return Tint(corner + new string(horizontal, layout.Inner) + corner, Style.BorderColor, ansi);
        }

        string label = " " + layout.Title + " ";
        return Tint(corner + horizontal, Style.BorderColor, ansi)
            + Tint(label, Style.TitleColor, ansi)
            + Tint(new string(horizontal, layout.Inner - 1 - TextFit.Width(label)) + corner, Style.BorderColor, ansi);
    }

    private int FitContentWidth(string title)
    {
        int width = title.Length > 0 ? TextFit.Width(title) + TitleMargin - (2 * Style.Padding) : 0;
        foreach (string line in _lines)
        {
            width = Math.Max(width, TextFit.Width(PromptRow.Clean(line)));
        }

        return width;
    }

    private Layout BuildLayout(int maxColumns)
    {
        string title = PromptRow.Clean(Title);
        int content = _width ?? FitContentWidth(title);
        if (maxColumns > 0)
        {
            content = Math.Min(content, Math.Max(0, maxColumns - 2 - (2 * Style.Padding)));
        }
        int inner = content + (2 * Style.Padding);
        int titleSpace = inner - TitleMargin;
        return new Layout(content, inner, titleSpace > 0 ? Fit(title, titleSpace) : string.Empty);
    }

    private sealed class Layout
    {
        public Layout(int content, int inner, string title)
        {
            Content = content;
            Inner = inner;
            Title = title;
        }

        /// <summary>Width of the text area in cells.</summary>
        public int Content { get; }

        /// <summary>Cells between the left and right frame cell (padding and text area).</summary>
        public int Inner { get; }

        /// <summary>Title as drawn (already cut), empty if none.</summary>
        public string Title { get; }

        public int Total => Inner + 2;
    }

    private sealed class LineCollection : Collection<string>
    {
        protected override void InsertItem(int index, string item) => base.InsertItem(index, Check(item));

        protected override void SetItem(int index, string item) => base.SetItem(index, Check(item));

        private static string Check(string item) => item ?? throw new ArgumentNullException(nameof(item));
    }
}
