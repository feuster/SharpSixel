using System;
using System.Collections.Generic;
using System.Text;

namespace SharpSixel.Ui;

/// <summary>
/// Renders the rows of a prompt. Sixel mode: one graphic (optional rounded frame around the prompt, highlight bar behind the
/// current row, marker per row: radio, check box with check mark, dot, rounded button faces) drawn first, then the text of the
/// rows on top as terminal text (on the color of the bar, panel or button under it, because text in a cell replaces the
/// graphic below it). ASCII mode: one line per row. The number of rows must stay constant while the prompt is shown.
/// </summary>
internal sealed class PromptView : Widget
{
    /// <summary>Cells the marker graphic is wide; the text of marker rows starts one cell after it.</summary>
    internal const int MarkerColumns = 2;

    private const double MarkerScale = 0.8;

    public PromptView(PromptStyle style)
    {
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    public PromptStyle Style { get; }

    public List<PromptRow> Rows { get; } = new List<PromptRow>();

    /// <summary>Rows (and columns) the frame adds around the content in the given environment: 2 with a sixel frame, else 0.</summary>
    internal static int ExtraRows(PromptStyle style, WidgetEnvironment environment) =>
        environment.UseSixel && style.ShowFrame ? 2 : 0;

    protected override int SixelRows => Math.Max(1, Rows.Count) + (Style.ShowFrame ? 2 : 0);

    protected override int AsciiRows => Math.Max(1, Rows.Count);

    protected override int MeasureColumns(WidgetEnvironment environment) =>
        environment.UseSixel ? ContentColumns() + ExtraRows(Style, environment) : base.MeasureColumns(environment);

    protected override string RenderAscii(WidgetEnvironment environment)
    {
        var lines = new string[Rows.Count];
        for (int i = 0; i < lines.Length; i++)
        {
            PromptRow row = Rows[i];
            lines[i] = Paint(row.AsciiPrefix + Body(row, environment), row.TextColor, environment) + CaretText(row, environment);
        }

        return string.Join("\n", lines);
    }

    protected override string RenderSixel(WidgetEnvironment environment)
    {
        int offset = Style.ShowFrame ? 1 : 0;
        int content = ContentColumns();
        var sb = new StringBuilder(AnsiCodes.SaveCursor);
        for (int i = 0; i < SixelRows; i++)
        {
            sb.Append(AnsiCodes.Down(i)).Append(AnsiCodes.EraseLineRest).Append(AnsiCodes.RestoreCursor);
        }

        if (NeedsImage())
        {
            sb.Append(new SixelEncoder().Encode(CreateImage(environment))).Append(AnsiCodes.RestoreCursor);
        }

        for (int i = 0; i < Rows.Count; i++)
        {
            AppendText(sb, Rows[i], offset + i, offset, content, environment);
        }

        return sb.ToString();
    }

    internal SixelImage CreateImage(WidgetEnvironment environment)
    {
        bool framed = Style.ShowFrame;
        int offset = framed ? 1 : 0;
        int totalRows = SixelRows;
        int content = ContentColumns();
        bool anyBar = false;
        bool anyChip = false;
        foreach (PromptRow row in Rows)
        {
            anyBar |= BarActive(row);
            anyChip |= row.Chips.Count > 0;
        }

        int columns = framed ? content + 2 : Math.Max(MarkerColumns + (anyBar ? 1 : 0), anyChip ? content : 0);
        int cellWidth = environment.CellWidth;
        int cellHeight = environment.CellHeight;
        var image = new SixelImage(columns * cellWidth, environment.GetImageHeight(totalRows));
        SixelColor panel = Style.BackgroundColor;
        if (panel.A > 0)
        {
            Raster.Fill(image, panel);
        }

        if (framed)
        {
            DrawFrame(image, environment, totalRows, panel);
        }

        int markerWidth = MarkerColumns * cellWidth;
        int size = Math.Max(1, Math.Min((int)Math.Round(cellHeight * MarkerScale, MidpointRounding.AwayFromZero), markerWidth));
        for (int i = 0; i < Rows.Count; i++)
        {
            PromptRow row = Rows[i];
            int top = (offset + i) * cellHeight;
            SixelColor hole = panel;
            if (BarActive(row))
            {
                Raster.FillRoundedRect(image, offset * cellWidth, top, (MarkerColumns + 1) * cellWidth, cellHeight, 0, Style.HighlightColor);
                hole = Style.HighlightColor;
            }

            int x = (offset * cellWidth) + ((markerWidth - size) / 2);
            int y = Math.Max(0, Math.Min(top + ((cellHeight - size) / 2), image.Height - size));
            DrawMarker(image, row, x, y, size, hole);
            foreach (PromptChip chip in row.Chips)
            {
                Raster.FillRoundedRect(
                    image, (offset + MarkerColumns + 1 + chip.Start) * cellWidth, top, chip.Length * cellWidth, cellHeight, Style.CornerRadius, chip.Color);
            }
        }

        return image;
    }

    private static bool Indented(PromptRow row) => row.Shape != MarkerShape.None || row.Indent;

    private bool BarActive(PromptRow row) => row.Bar && Style.HighlightColor.A > 0;

    private bool NeedsImage()
    {
        if (Style.ShowFrame)
        {
            return true;
        }

        foreach (PromptRow row in Rows)
        {
            if (row.Shape != MarkerShape.None || BarActive(row) || row.Chips.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Width of the widest row in cells (text start included), at least 1.</summary>
    private int ContentColumns()
    {
        int content = 1;
        foreach (PromptRow row in Rows)
        {
            content = Math.Max(content, (Indented(row) ? MarkerColumns + 1 : 0) + TextFit.Width(row.Text) + (row.Caret ? 1 : 0));
        }

        return content;
    }

    /// <summary>Rounded frame whose line runs through the middle of the border cells, so the text keeps some distance.</summary>
    private void DrawFrame(SixelImage image, WidgetEnvironment environment, int totalRows, SixelColor panel)
    {
        int cellWidth = environment.CellWidth;
        int cellHeight = environment.CellHeight;
        int thickness = Math.Max(1, cellHeight / 10);
        int x = (cellWidth / 2) - (thickness / 2);
        int y = (cellHeight / 2) - (thickness / 2);
        int right = image.Width - (cellWidth / 2) + (thickness / 2);
        int bottom = Math.Min(image.Height, ((totalRows - 1) * cellHeight) + (cellHeight / 2) + (thickness / 2));
        int width = right - x;
        int height = bottom - y;
        int radius = Style.CornerRadius;
        Raster.FillRoundedRect(image, x, y, width, height, radius, Style.FrameColor);
        Raster.FillRoundedRect(
            image, x + thickness, y + thickness, width - (2 * thickness), height - (2 * thickness), Math.Max(0, radius - thickness), panel);
    }

    private void AppendText(StringBuilder sb, PromptRow row, int screenRow, int offset, int content, WidgetEnvironment environment)
    {
        int textStart = Indented(row) ? MarkerColumns + 1 : 0;
        sb.Append(AnsiCodes.Down(screenRow)).Append(AnsiCodes.Forward(offset + textStart));
        if (row.Chips.Count > 0)
        {
            int position = 0;
            foreach (PromptChip chip in row.Chips)
            {
                // LabelStart and LabelLength are char indexes into the text; the cursor moves in cells.
                string label = row.Text.Substring(chip.LabelStart, chip.LabelLength);
                int labelCell = TextFit.Width(row.Text.Substring(0, chip.LabelStart));
                sb.Append(AnsiCodes.Forward(labelCell - position))
                    .Append(AnsiCodes.Foreground(chip.TextColor)).Append(AnsiCodes.Background(chip.Color))
                    .Append(label).Append(AnsiCodes.Reset);
                position = labelCell + TextFit.Width(label);
            }
        }
        else
        {
            bool bar = BarActive(row);
            SixelColor? back = bar ? Style.HighlightColor : (Style.BackgroundColor.A > 0 ? (SixelColor?)Style.BackgroundColor : null);
            string body = Body(row, environment);
            if (back.HasValue)
            {
                int pad = bar ? Math.Max(0, content - textStart - TextFit.Width(row.Text)) : 0;
                if (body.Length > 0 || pad > 0)
                {
                    sb.Append(AnsiCodes.Background(back.Value));
                    if (row.TextColor.HasValue)
                    {
                        sb.Append(AnsiCodes.Foreground(row.TextColor.Value));
                    }

                    sb.Append(body).Append(' ', pad).Append(AnsiCodes.Reset);
                }
            }
            else
            {
                sb.Append(Paint(body, row.TextColor, environment));
            }

            sb.Append(CaretText(row, environment));
        }

        sb.Append(AnsiCodes.RestoreCursor);
    }

    private void DrawMarker(SixelImage image, PromptRow row, int x, int y, int size, SixelColor hole)
    {
        SixelColor color = row.MarkerColor;
        int ring = Math.Max(1, size / 6);
        int inner = size - (2 * ring);
        switch (row.Shape)
        {
            case MarkerShape.Radio:
                Raster.FillRoundedRect(image, x, y, size, size, size / 2, color);
                Raster.FillRoundedRect(image, x + ring, y + ring, inner, inner, inner / 2, hole);
                if (row.Filled)
                {
                    int dot = Math.Max(1, size / 4);
                    Raster.FillRoundedRect(image, x + dot, y + dot, size - (2 * dot), size - (2 * dot), (size - (2 * dot)) / 2, color);
                }

                break;
            case MarkerShape.Check:
                int radius = Math.Max(1, size / 5);
                Raster.FillRoundedRect(image, x, y, size, size, radius, color);
                if (row.Filled)
                {
                    // The check mark is cut out of the solid box (drawn in the hole color).
                    double stroke = Math.Max(1.5, size / 8.0);
                    Raster.FillLine(image, x + (size * 0.24), y + (size * 0.54), x + (size * 0.43), y + (size * 0.73), stroke, hole);
                    Raster.FillLine(image, x + (size * 0.43), y + (size * 0.73), x + (size * 0.78), y + (size * 0.30), stroke, hole);
                }
                else
                {
                    Raster.FillRoundedRect(image, x + ring, y + ring, inner, inner, Math.Max(0, radius - ring), hole);
                }

                break;
            case MarkerShape.Dot:
                Raster.FillRoundedRect(image, x, y, size, size, size / 2, color);
                break;
        }
    }

    /// <summary>Row text; the highlight range is drawn in reverse video with ANSI (ASCII brackets carry it otherwise).</summary>
    private static string Body(PromptRow row, WidgetEnvironment environment)
    {
        string text = row.Text;
        int start = Math.Max(0, Math.Min(row.HighlightStart, text.Length));
        int length = Math.Max(0, Math.Min(row.HighlightLength, text.Length - start));
        if (length == 0 || !environment.UseAnsi)
        {
            return text;
        }

        string marked = AnsiCodes.Colorize(
            AnsiCodes.ReverseOn + text.Substring(start, length) + AnsiCodes.ReverseOff, row.HighlightColor, true);
        return text.Substring(0, start) + marked + text.Substring(start + length);
    }

    private static string Paint(string text, SixelColor? color, WidgetEnvironment environment) =>
        color.HasValue ? AnsiCodes.Colorize(text, color.Value, environment.UseAnsi) : text;

    private static string CaretText(PromptRow row, WidgetEnvironment environment)
    {
        if (!row.Caret)
        {
            return string.Empty;
        }

        return environment.UseAnsi ? AnsiCodes.ReverseOn + " " + AnsiCodes.ReverseOff : "_";
    }
}
