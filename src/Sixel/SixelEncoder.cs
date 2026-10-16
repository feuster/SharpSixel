using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sixel;

/// <summary>Encodes a <see cref="SixelImage"/> as a DEC sixel string (7-bit DCS ... ST).</summary>
public sealed class SixelEncoder
{
    private const int SixelHeight = 6;
    private const int MinRunForRepeat = 4;
    private const string Introducer = "\u001bP";
    private const string Terminator = "\u001b\\";

    private readonly SixelEncoderOptions _options;

    /// <summary>Creates an encoder with default options.</summary>
    public SixelEncoder()
        : this(new SixelEncoderOptions())
    {
    }

    /// <summary>Creates an encoder with custom options.</summary>
    public SixelEncoder(SixelEncoderOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        options.Validate();
        _options = options;
    }

    /// <summary>Encodes the image and returns the complete sixel sequence.</summary>
    public string Encode(SixelImage image)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        QuantizedImage quantized = ColorQuantizer.Quantize(image, _options.MaxColors, _options.AlphaThreshold);
        var sb = new StringBuilder();
        AppendHeader(sb, image, quantized);
        AppendPalette(sb, quantized.Palette);
        AppendBands(sb, quantized.Indices, image.Width, image.Height, quantized.Palette.Length);
        sb.Append(Terminator);
        return sb.ToString();
    }

    /// <summary>Encodes the image and writes the sequence to <paramref name="writer"/>.</summary>
    public void Encode(SixelImage image, TextWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        writer.Write(Encode(image));
    }

    /// <summary>Encodes the image and returns the sequence as ASCII bytes.</summary>
    public byte[] EncodeToBytes(SixelImage image) => Encoding.ASCII.GetBytes(Encode(image));

    private void AppendHeader(StringBuilder sb, SixelImage image, QuantizedImage quantized)
    {
        // P1 = 0 defers to the raster attributes; without them 7 selects square pixels. P2 = 1 keeps unset pixels.
        int macro = _options.EmitRasterAttributes ? 0 : 7;
        int background = quantized.HasTransparentPixels ? 1 : 0;
        sb.Append(Introducer).Append(macro).Append(';').Append(background).Append(";0q");

        if (_options.EmitRasterAttributes)
        {
            sb.Append('"')
                .Append(image.PixelAspectVertical).Append(';')
                .Append(image.PixelAspectHorizontal).Append(';')
                .Append(image.Width).Append(';')
                .Append(image.Height);
        }
    }

    private static void AppendPalette(StringBuilder sb, SixelColor[] palette)
    {
        for (int i = 0; i < palette.Length; i++)
        {
            sb.Append('#').Append(i).Append(";2;")
                .Append(ColorMath.ByteToPercent(palette[i].R)).Append(';')
                .Append(ColorMath.ByteToPercent(palette[i].G)).Append(';')
                .Append(ColorMath.ByteToPercent(palette[i].B));
        }
    }

    private static void AppendBands(StringBuilder sb, short[] indices, int width, int height, int colorCount)
    {
        var rows = new byte[colorCount][];
        var used = new bool[colorCount];
        var usedColors = new List<int>();
        int bandCount = (height + SixelHeight - 1) / SixelHeight;

        for (int band = 0; band < bandCount; band++)
        {
            CollectBand(indices, width, height, band, rows, used, usedColors);
            usedColors.Sort();
            for (int k = 0; k < usedColors.Count; k++)
            {
                int color = usedColors[k];
                if (k > 0)
                {
                    sb.Append('$');
                }

                AppendColorRow(sb, color, rows[color]);
                Array.Clear(rows[color], 0, width);
                used[color] = false;
            }

            usedColors.Clear();
            if (band < bandCount - 1)
            {
                sb.Append('-');
            }
        }
    }

    /// <summary>Builds, per color used in the band, the sixel bit pattern for every column.</summary>
    private static void CollectBand(
        short[] indices,
        int width,
        int height,
        int band,
        byte[][] rows,
        bool[] used,
        List<int> usedColors)
    {
        int top = band * SixelHeight;
        int rowCount = Math.Min(SixelHeight, height - top);
        for (int r = 0; r < rowCount; r++)
        {
            int offset = (top + r) * width;
            byte bit = (byte)(1 << r);
            for (int x = 0; x < width; x++)
            {
                int color = indices[offset + x];
                if (color < 0)
                {
                    continue;
                }

                if (!used[color])
                {
                    used[color] = true;
                    usedColors.Add(color);
                    if (rows[color] == null)
                    {
                        rows[color] = new byte[width];
                    }
                }

                rows[color][x] |= bit;
            }
        }
    }

    private static void AppendColorRow(StringBuilder sb, int color, byte[] row)
    {
        sb.Append('#').Append(color);

        int last = row.Length - 1;
        while (last >= 0 && row[last] == 0)
        {
            last--;
        }

        int x = 0;
        while (x <= last)
        {
            byte value = row[x];
            int run = 1;
            while (x + run <= last && row[x + run] == value)
            {
                run++;
            }

            AppendRun(sb, (char)(value + 63), run);
            x += run;
        }
    }

    private static void AppendRun(StringBuilder sb, char sixel, int run)
    {
        if (run >= MinRunForRepeat)
        {
            sb.Append('!').Append(run).Append(sixel);
        }
        else
        {
            sb.Append(sixel, run);
        }
    }
}
