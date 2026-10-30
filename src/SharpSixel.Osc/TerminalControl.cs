using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpSixel.Osc;

/// <summary>
/// Controls the terminal with Operating System Commands (OSC) and cursor style sequences:
/// window title, foreground, background and cursor color, and cursor style.
/// </summary>
/// <remarks>
/// Every method writes to the given <see cref="TextWriter"/>; <c>null</c> means <see cref="Console.Out"/>.
/// The terminal must process VT sequences (enable them on Windows). Terminals ignore sequences they do not support.
/// The title backup is static state and not synchronized.
/// </remarks>
public static class TerminalControl
{
    private const string Esc = "\u001b";
    private const string Bel = "\u0007";
    private const int ForegroundCode = 10;
    private const int BackgroundCode = 11;
    private const int CursorColorCode = 12;
    private const int ResetOffset = 100;

    private static string? titleBackup;

    /// <summary>Sets the cursor style.</summary>
    /// <param name="style">New cursor style.</param>
    /// <param name="writer">Terminal output; <c>null</c> = <see cref="Console.Out"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="style"/> is not a defined value.</exception>
    public static void SetCursorStyle(CursorStyle style, TextWriter? writer = null)
    {
        if (!Enum.IsDefined(typeof(CursorStyle), style))
        {
            throw new ArgumentOutOfRangeException(nameof(style));
        }

        Out(writer).Write(Esc + "[" + (int)style + " q");
    }

    /// <summary>Sets the terminal title. Control characters are removed.</summary>
    /// <remarks>
    /// On Windows the previous console title is remembered (once, until <see cref="ResetTitle"/>) so it can be restored.
    /// Other platforms cannot read the title; <see cref="ResetTitle"/> does nothing there.
    /// </remarks>
    /// <param name="title">New title text.</param>
    /// <param name="writer">Terminal output; <c>null</c> = <see cref="Console.Out"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="title"/> is null or empty.</exception>
    public static void SetTitle(string title, TextWriter? writer = null)
    {
        if (string.IsNullOrEmpty(title))
        {
            throw new ArgumentException("The title must not be empty.", nameof(title));
        }

        if (string.IsNullOrEmpty(titleBackup))
        {
            titleBackup = ReadConsoleTitle();
        }

        Out(writer).Write(Esc + "]0;" + RemoveControlCharacters(title) + Bel);
    }

    /// <summary>Sets the foreground (text) color. The alpha channel is ignored.</summary>
    /// <param name="color">New color.</param>
    /// <param name="writer">Terminal output; <c>null</c> = <see cref="Console.Out"/>.</param>
    public static void SetForegroundColor(SixelColor color, TextWriter? writer = null)
    {
        SetColor(ForegroundCode, color, writer);
    }

    /// <summary>Sets the foreground (text) color.</summary>
    /// <param name="color">New color as <c>#RGB</c> or <c>#RRGGBB</c>.</param>
    /// <param name="writer">Terminal output; <c>null</c> = <see cref="Console.Out"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="color"/> has an invalid format.</exception>
    public static void SetForegroundColor(string color, TextWriter? writer = null)
    {
        SetColor(ForegroundCode, ParseColor(color, nameof(color)), writer);
    }

    /// <summary>Sets the background color. The alpha channel is ignored.</summary>
    /// <param name="color">New color.</param>
    /// <param name="writer">Terminal output; <c>null</c> = <see cref="Console.Out"/>.</param>
    public static void SetBackgroundColor(SixelColor color, TextWriter? writer = null)
    {
        SetColor(BackgroundCode, color, writer);
    }

    /// <summary>Sets the background color.</summary>
    /// <param name="color">New color as <c>#RGB</c> or <c>#RRGGBB</c>.</param>
    /// <param name="writer">Terminal output; <c>null</c> = <see cref="Console.Out"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="color"/> has an invalid format.</exception>
    public static void SetBackgroundColor(string color, TextWriter? writer = null)
    {
        SetColor(BackgroundCode, ParseColor(color, nameof(color)), writer);
    }

    /// <summary>Sets the cursor color. The alpha channel is ignored.</summary>
    /// <param name="color">New color.</param>
    /// <param name="writer">Terminal output; <c>null</c> = <see cref="Console.Out"/>.</param>
    public static void SetCursorColor(SixelColor color, TextWriter? writer = null)
    {
        SetColor(CursorColorCode, color, writer);
    }

    /// <summary>Sets the cursor color.</summary>
    /// <param name="color">New color as <c>#RGB</c> or <c>#RRGGBB</c>.</param>
    /// <param name="writer">Terminal output; <c>null</c> = <see cref="Console.Out"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="color"/> has an invalid format.</exception>
    public static void SetCursorColor(string color, TextWriter? writer = null)
    {
        SetColor(CursorColorCode, ParseColor(color, nameof(color)), writer);
    }

    /// <summary>Restores the title remembered by <see cref="SetTitle"/> (Windows only); does nothing without a remembered title.</summary>
    /// <param name="writer">Terminal output; <c>null</c> = <see cref="Console.Out"/>.</param>
    public static void ResetTitle(TextWriter? writer = null)
    {
        if (string.IsNullOrEmpty(titleBackup))
        {
            return;
        }

        Out(writer).Write(Esc + "]0;" + RemoveControlCharacters(titleBackup!) + Bel);
        titleBackup = null;
    }

    /// <summary>Resets the foreground color to the terminal default.</summary>
    /// <param name="writer">Terminal output; <c>null</c> = <see cref="Console.Out"/>.</param>
    public static void ResetForegroundColor(TextWriter? writer = null)
    {
        ResetColor(ForegroundCode, writer);
    }

    /// <summary>Resets the background color to the terminal default.</summary>
    /// <param name="writer">Terminal output; <c>null</c> = <see cref="Console.Out"/>.</param>
    public static void ResetBackgroundColor(TextWriter? writer = null)
    {
        ResetColor(BackgroundCode, writer);
    }

    /// <summary>Resets the cursor color to the terminal default.</summary>
    /// <param name="writer">Terminal output; <c>null</c> = <see cref="Console.Out"/>.</param>
    public static void ResetCursorColor(TextWriter? writer = null)
    {
        ResetColor(CursorColorCode, writer);
    }

    /// <summary>Resets the title (see <see cref="ResetTitle"/>), the three colors and the cursor style.</summary>
    /// <param name="writer">Terminal output; <c>null</c> = <see cref="Console.Out"/>.</param>
    public static void Reset(TextWriter? writer = null)
    {
        ResetTitle(writer);
        ResetForegroundColor(writer);
        ResetBackgroundColor(writer);
        ResetCursorColor(writer);
        SetCursorStyle(CursorStyle.Default, writer);
    }

    private static TextWriter Out(TextWriter? writer) => writer ?? Console.Out;

    private static void SetColor(int code, SixelColor color, TextWriter? writer)
    {
        string hex = "#" + color.R.ToString("X2", CultureInfo.InvariantCulture)
            + color.G.ToString("X2", CultureInfo.InvariantCulture)
            + color.B.ToString("X2", CultureInfo.InvariantCulture);
        Out(writer).Write(Esc + "]" + code + ";" + hex + Bel);
    }

    private static void ResetColor(int code, TextWriter? writer)
    {
        Out(writer).Write(Esc + "]" + (code + ResetOffset) + Bel);
    }

    /// <summary>Parses <c>#RGB</c> or <c>#RRGGBB</c>.</summary>
    private static SixelColor ParseColor(string? value, string parameterName)
    {
        if (value != null && (value.Length == 4 || value.Length == 7) && value[0] == '#')
        {
            int digits = (value.Length - 1) / 3;
            var channels = new byte[3];
            bool valid = true;
            for (int i = 0; i < channels.Length && valid; i++)
            {
                valid = byte.TryParse(value.Substring(1 + i * digits, digits), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out channels[i]);
                if (valid && digits == 1)
                {
                    channels[i] = (byte)(channels[i] * 17);
                }
            }

            if (valid)
            {
                return new SixelColor(channels[0], channels[1], channels[2]);
            }
        }

        throw new ArgumentException("Expected #RGB or #RRGGBB.", parameterName);
    }

    /// <summary>Removes control characters so a text cannot end the sequence early or start another one.</summary>
    private static string RemoveControlCharacters(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            if (!char.IsControl(c))
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private static string? ReadConsoleTitle()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }

        try
        {
            return Console.Title;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is PlatformNotSupportedException)
        {
            return null;
        }
    }
}
