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

    private const char Escape = '\u001b';
    private const char Csi8Bit = '\u009b';
    private const int SixelAttribute = 4;
    private const int MaxResponseLength = 128;

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
