using System;
using System.IO;
using System.Text;

namespace SharpSixel.Ui;

/// <summary>Reads one line key by key and echoes it (used by the prompts without ANSI/sixel support).</summary>
internal static class LineReader
{
    /// <summary>Reads until Enter; Backspace removes the last character. <paramref name="mask"/> is echoed instead of the typed character.</summary>
    public static string ReadLine(TextWriter writer, Func<ConsoleKeyInfo> readKey, char? mask)
    {
        var sb = new StringBuilder();
        while (true)
        {
            ConsoleKeyInfo key = readKey();
            if (key.Key == ConsoleKey.Enter)
            {
                writer.WriteLine();
                writer.Flush();
                return sb.ToString();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (sb.Length > 0)
                {
                    sb.Length--;
                    writer.Write("\b \b");
                    writer.Flush();
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                sb.Append(key.KeyChar);
                writer.Write(mask ?? key.KeyChar);
                writer.Flush();
            }
        }
    }
}
