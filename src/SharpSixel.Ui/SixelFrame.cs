using System.Text;

namespace SharpSixel.Ui;

/// <summary>
/// Assembles a sixel widget frame: optional caption, the graphic and optional trailing text on the vertically centered row.
/// The frame saves the cursor at its origin and addresses everything relative to it, because the cursor position
/// after a sixel image differs between terminals. Text is assumed to occupy one cell per char.
/// </summary>
internal static class SixelFrame
{
    public static string Build(SixelImage image, int rows, int imageColumns, string caption, string trailing, bool eraseBehind)
    {
        int textRow = (rows - 1) / 2;
        int imageColumn = caption.Length;
        var sb = new StringBuilder();
        sb.Append(AnsiCodes.SaveCursor);

        if (caption.Length > 0)
        {
            sb.Append(AnsiCodes.Down(textRow)).Append(caption).Append(AnsiCodes.RestoreCursor);
        }

        if (eraseBehind)
        {
            for (int row = 0; row < rows; row++)
            {
                sb.Append(AnsiCodes.Down(row)).Append(AnsiCodes.Forward(imageColumn))
                    .Append(AnsiCodes.EraseChars(imageColumns)).Append(AnsiCodes.RestoreCursor);
            }
        }

        sb.Append(AnsiCodes.Forward(imageColumn)).Append(new SixelEncoder().Encode(image)).Append(AnsiCodes.RestoreCursor);

        if (trailing.Length > 0)
        {
            sb.Append(AnsiCodes.Down(textRow)).Append(AnsiCodes.Forward(imageColumn + imageColumns))
                .Append(trailing).Append(AnsiCodes.EraseLineRest).Append(AnsiCodes.RestoreCursor);
        }

        return sb.ToString();
    }
}
