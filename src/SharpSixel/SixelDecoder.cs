using System;
using System.IO;

namespace SharpSixel;

/// <summary>Decodes DEC sixel graphics (DECSIXEL) into a <see cref="SixelImage"/>.</summary>
public sealed class SixelDecoder
{
    private const byte Esc = 0x1B;
    private const byte Dcs8 = 0x90;

    private readonly SixelDecoderOptions _options;

    /// <summary>Creates a decoder with default limits.</summary>
    public SixelDecoder()
        : this(new SixelDecoderOptions())
    {
    }

    /// <summary>Creates a decoder with custom options.</summary>
    public SixelDecoder(SixelDecoderOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        options.Validate();
        _options = options;
    }

    /// <summary>Decodes a sixel string. Chars 0-255 are treated as bytes (so U+0090 is the 8-bit DCS).</summary>
    public SixelImage Decode(string sixel)
    {
        if (sixel == null)
        {
            throw new ArgumentNullException(nameof(sixel));
        }

        return Decode(ByteInput.FromLatin1(sixel));
    }

    /// <summary>Decodes sixel data read from a stream.</summary>
    public SixelImage Decode(Stream stream) => Decode(ByteInput.ReadAll(stream));

    /// <summary>
    /// Decodes sixel bytes. The data may start with a 7-bit or 8-bit DCS header; without a header the whole
    /// input is interpreted as sixel data.
    /// </summary>
    /// <exception cref="SixelException">The data contains no image or exceeds the configured limits.</exception>
    public SixelImage Decode(byte[] data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        int start = FindDataStart(data, out int macroParameter, out int backgroundSelect);
        return new DecodeSession(_options, macroParameter, backgroundSelect).Run(data, start);
    }

    /// <summary>
    /// Returns true if the data starts (after optional white space) with a sixel DCS header, i.e. <c>ESC P ... q</c>
    /// or the 8-bit form. Headerless sixel data is deliberately not recognized, so ordinary text is never taken for sixel.
    /// </summary>
    public static bool LooksLikeSixel(byte[] data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        int position = 0;
        while (position < data.Length && data[position] <= 0x20 && data[position] != Esc)
        {
            position++;
        }

        if (position >= data.Length)
        {
            return false;
        }

        if (data[position] == Dcs8)
        {
            position++;
        }
        else if (data[position] == Esc && position + 1 < data.Length && data[position + 1] == (byte)'P')
        {
            position += 2;
        }
        else
        {
            return false;
        }

        return TryParseHeader(data, ref position, out _, out _);
    }

    private static int FindDataStart(byte[] data, out int macroParameter, out int backgroundSelect)
    {
        for (int i = 0; i < data.Length; i++)
        {
            int position;
            if (data[i] == Dcs8)
            {
                position = i + 1;
            }
            else if (data[i] == Esc && i + 1 < data.Length && data[i + 1] == (byte)'P')
            {
                position = i + 2;
            }
            else
            {
                continue;
            }

            if (TryParseHeader(data, ref position, out macroParameter, out backgroundSelect))
            {
                return position;
            }
        }

        macroParameter = -1;
        backgroundSelect = 0;
        return 0;
    }

    /// <summary>Parses "P1;P2;P3 q" after the DCS introducer; on success <paramref name="position"/> is behind 'q'.</summary>
    private static bool TryParseHeader(byte[] data, ref int position, out int p1, out int p2)
    {
        var values = new[] { -1, -1, -1 };
        int field = 0;
        long current = -1;
        p1 = -1;
        p2 = 0;

        for (; position < data.Length; position++)
        {
            byte b = data[position];
            if (b >= '0' && b <= '9')
            {
                current = Math.Min(Math.Max(current, 0) * 10 + (b - '0'), int.MaxValue);
            }
            else if (b == ';')
            {
                if (field < values.Length)
                {
                    values[field] = (int)current;
                }

                field++;
                current = -1;
            }
            else if (b == (byte)'q')
            {
                if (field < values.Length)
                {
                    values[field] = (int)current;
                }

                position++;
                p1 = values[0];
                p2 = Math.Max(values[1], 0);
                return true;
            }
            else if (b < 0x20 || b > 0x2F)
            {
                return false;
            }
        }

        return false;
    }
}
