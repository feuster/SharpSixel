using System;
using System.IO;
using System.Text;

namespace Sixel;

/// <summary>
/// Detects sixel support of a terminal via the Primary Device Attributes query (DA1):
/// the terminal answers <c>CSI ? Ps ; Ps ... c</c> and lists attribute 4 if it can display sixel graphics.
/// This class is I/O agnostic; the caller provides a character source (with timeout) and puts the terminal in raw mode.
/// </summary>
public static class SixelTerminal
{
    /// <summary>The DA1 request (<c>ESC [ c</c>) that has to be written to the terminal.</summary>
    public const string DeviceAttributesRequest = "\u001b[c";

    /// <summary>Requests the text area size in pixels (<c>CSI 14 t</c>) and the cell size in pixels (<c>CSI 16 t</c>).</summary>
    public const string PixelSizeRequest = "\u001b[14t\u001b[16t";

    private const char Escape = '\u001b';
    private const char Csi8Bit = '\u009b';
    private const int SixelAttribute = 4;
    private const int MaxResponseLength = 128;
    private const int MaxQueryReplies = 8;
    private const int TextAreaReport = 4;
    private const int CellReport = 6;

    /// <summary>Returns true if the DA1 response lists the sixel attribute (4).</summary>
    /// <param name="response">Complete DA1 response, e.g. <c>ESC [ ? 6 2 ; 4 c</c>; null or malformed input yields false.</param>
    public static bool IsSixelSupported(string? response)
    {
        if (!TryExtractParameters(response, out string[] parameters))
        {
            return false;
        }

        // The first parameter is the terminal class (e.g. 62); features follow.
        for (int i = 1; i < parameters.Length; i++)
        {
            if (parameters[i] == SixelAttribute.ToString())
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Sends the DA1 request and evaluates the answer.</summary>
    /// <param name="output">Terminal output; receives <see cref="DeviceAttributesRequest"/> and is flushed.</param>
    /// <param name="readChar">Returns the next input char (0-255), or -1 when no more input arrives in time.</param>
    /// <returns>True if the terminal reports sixel support; false if not, or if it did not answer.</returns>
    public static bool Detect(TextWriter output, Func<int> readChar)
    {
        if (output == null)
        {
            throw new ArgumentNullException(nameof(output));
        }

        if (readChar == null)
        {
            throw new ArgumentNullException(nameof(readChar));
        }

        output.Write(DeviceAttributesRequest);
        output.Flush();
        return IsSixelSupported(ReadResponse(readChar));
    }

    /// <summary>
    /// Reads one DA1 response from the character source: skips leading typed-ahead input, then reads up to the final 'c'.
    /// </summary>
    /// <returns>The response, or null on timeout, end of input or an overlong response.</returns>
    public static string? ReadResponse(Func<int> readChar)
    {
        if (readChar == null)
        {
            throw new ArgumentNullException(nameof(readChar));
        }

        var response = new StringBuilder();
        while (response.Length < MaxResponseLength)
        {
            int next = readChar();
            if (next < 0)
            {
                return null;
            }

            if (response.Length == 0 && next != Escape && next != Csi8Bit)
            {
                continue;
            }

            response.Append((char)next);
            if (next == 'c')
            {
                return response.ToString();
            }
        }

        return null;
    }

    /// <summary>
    /// Queries sixel support and pixel geometry in one round trip: text area size (<c>CSI 14 t</c>) and cell size
    /// (<c>CSI 16 t</c>) followed by DA1. Terminals that ignore the size requests still answer DA1, so the call
    /// returns promptly; unreported values are 0.
    /// </summary>
    /// <param name="output">Terminal output; receives the requests and is flushed.</param>
    /// <param name="readChar">Returns the next input char (0-255), or -1 when no more input arrives in time.</param>
    public static SixelTerminalInfo Query(TextWriter output, Func<int> readChar)
    {
        if (output == null)
        {
            throw new ArgumentNullException(nameof(output));
        }

        if (readChar == null)
        {
            throw new ArgumentNullException(nameof(readChar));
        }

        output.Write(PixelSizeRequest + DeviceAttributesRequest);
        output.Flush();

        bool sixel = false;
        int textWidth = 0, textHeight = 0, cellWidth = 0, cellHeight = 0;
        for (int i = 0; i < MaxQueryReplies; i++)
        {
            string? reply = ReadControlSequence(readChar);
            if (reply == null)
            {
                break;
            }

            if (reply.EndsWith("c", StringComparison.Ordinal))
            {
                sixel = IsSixelSupported(reply);
                break;
            }

            if (TryParseSizeReport(reply, out int kind, out int height, out int width))
            {
                if (kind == TextAreaReport)
                {
                    textWidth = width;
                    textHeight = height;
                }
                else if (kind == CellReport)
                {
                    cellWidth = width;
                    cellHeight = height;
                }
            }
        }

        return new SixelTerminalInfo(sixel, textWidth, textHeight, cellWidth, cellHeight);
    }

    /// <summary>Parses a window report <c>CSI kind ; height ; width t</c>.</summary>
    internal static bool TryParseSizeReport(string? reply, out int kind, out int height, out int width)
    {
        kind = height = width = 0;
        if (reply == null || !reply.EndsWith("t", StringComparison.Ordinal))
        {
            return false;
        }

        int prefixLength = reply.StartsWith("\u001b[", StringComparison.Ordinal) ? 2
            : reply.StartsWith(Csi8Bit.ToString(), StringComparison.Ordinal) ? 1 : 0;
        if (prefixLength == 0)
        {
            return false;
        }

        string[] parts = reply.Substring(prefixLength, reply.Length - prefixLength - 1).Split(';');
        return parts.Length == 3
               && int.TryParse(parts[0], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out kind)
               && int.TryParse(parts[1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out height)
               && int.TryParse(parts[2], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out width);
    }

    /// <summary>Reads one control sequence (ESC [ ... final byte); typed-ahead input before it is skipped.</summary>
    private static string? ReadControlSequence(Func<int> readChar)
    {
        int next;
        do
        {
            next = readChar();
            if (next < 0)
            {
                return null;
            }
        }
        while (next != Escape && next != Csi8Bit);

        var sequence = new StringBuilder();
        sequence.Append((char)next);
        if (next == Escape)
        {
            next = readChar();
            if (next < 0)
            {
                return null;
            }

            sequence.Append((char)next);
            if (next != '[')
            {
                return sequence.ToString();
            }
        }

        while (sequence.Length < MaxResponseLength)
        {
            next = readChar();
            if (next < 0)
            {
                return null;
            }

            sequence.Append((char)next);
            if (next >= 0x40 && next <= 0x7E)
            {
                return sequence.ToString();
            }
        }

        return null;
    }

    private static bool TryExtractParameters(string? response, out string[] parameters)
    {
        parameters = new string[0];
        if (response == null)
        {
            return false;
        }

        int prefixLength = 3;
        int start = response.IndexOf("\u001b[?", StringComparison.Ordinal);
        if (start < 0)
        {
            prefixLength = 2;
            start = response.IndexOf(Csi8Bit + "?", StringComparison.Ordinal);
        }

        if (start < 0)
        {
            return false;
        }

        int end = response.IndexOf('c', start + prefixLength);
        if (end < 0)
        {
            return false;
        }

        parameters = response.Substring(start + prefixLength, end - start - prefixLength).Split(';');
        return true;
    }
}
