using System;
using System.IO;

namespace Sixel;

/// <summary>
/// Minimal BMP reader/writer. Reads uncompressed 1/4/8-bit palette and 16/24/32-bit images
/// (BI_RGB, BI_BITFIELDS, BI_ALPHABITFIELDS); writes 24-bit or, with transparency, 32-bit BGRA (V4 header).
/// </summary>
public static class BmpCodec
{
    private const int FileHeaderSize = 14;
    private const int InfoHeaderSize = 40;
    private const int V4HeaderSize = 108;
    private const long MaxPixels = 64L * 1024 * 1024;
    private const int CompressionRgb = 0;
    private const int CompressionRle8 = 1;
    private const int CompressionRle4 = 2;
    private const int CompressionBitfields = 3;
    private const int CompressionAlphaBitfields = 6;
    private const int SrgbColorSpace = 0x73524742;

    /// <summary>Returns true if the data starts with the BMP signature "BM".</summary>
    public static bool IsBmp(byte[] data) => data != null && data.Length >= 2 && data[0] == 'B' && data[1] == 'M';

    /// <summary>Decodes a BMP stream.</summary>
    public static SixelImage Decode(Stream stream) => Decode(ByteInput.ReadAll(stream));

    /// <summary>Decodes BMP bytes.</summary>
    /// <exception cref="SixelException">The data is not a valid or supported BMP.</exception>
    public static SixelImage Decode(byte[] data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        BmpHeader header = BmpHeader.Parse(data);
        SixelColor[] palette = header.BitsPerPixel <= 8 ? ReadPalette(data, header) : new SixelColor[0];
        var pixels = new byte[header.Width * header.Height * SixelImage.BytesPerPixel];

        for (int y = 0; y < header.Height; y++)
        {
            int sourceRow = header.TopDown ? y : header.Height - 1 - y;
            int rowOffset = header.DataOffset + sourceRow * header.Stride;
            for (int x = 0; x < header.Width; x++)
            {
                SixelColor color = ReadPixel(data, rowOffset, x, header, palette);
                int o = (y * header.Width + x) * SixelImage.BytesPerPixel;
                pixels[o] = color.R;
                pixels[o + 1] = color.G;
                pixels[o + 2] = color.B;
                pixels[o + 3] = color.A;
            }
        }

        return new SixelImage(header.Width, header.Height, pixels);
    }

    /// <summary>Encodes the image as BMP and returns the bytes.</summary>
    public static byte[] Encode(SixelImage image)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        bool alpha = image.HasTransparency;
        int bitsPerPixel = alpha ? 32 : 24;
        int headerSize = alpha ? V4HeaderSize : InfoHeaderSize;
        long stride = ((long)image.Width * bitsPerPixel + 31) / 32 * 4;
        long total = FileHeaderSize + headerSize + stride * image.Height;
        if (total > int.MaxValue)
        {
            throw new SixelException("The image is too large for the BMP format.");
        }

        var bmp = new byte[total];
        int dataOffset = FileHeaderSize + headerSize;
        bmp[0] = (byte)'B';
        bmp[1] = (byte)'M';
        WriteInt32(bmp, 2, (int)total);
        WriteInt32(bmp, 10, dataOffset);
        WriteInt32(bmp, 14, headerSize);
        WriteInt32(bmp, 18, image.Width);
        WriteInt32(bmp, 22, image.Height);
        WriteInt16(bmp, 26, 1);
        WriteInt16(bmp, 28, bitsPerPixel);
        WriteInt32(bmp, 30, alpha ? CompressionBitfields : CompressionRgb);
        WriteInt32(bmp, 34, (int)(stride * image.Height));
        WriteInt32(bmp, 38, 2835);
        WriteInt32(bmp, 42, 2835);
        if (alpha)
        {
            WriteInt32(bmp, 54, 0x00FF0000);
            WriteInt32(bmp, 58, 0x0000FF00);
            WriteInt32(bmp, 62, 0x000000FF);
            WriteInt32(bmp, 66, unchecked((int)0xFF000000));
            WriteInt32(bmp, 70, SrgbColorSpace);
        }

        WritePixels(bmp, dataOffset, (int)stride, image, alpha);
        return bmp;
    }

    /// <summary>Encodes the image as BMP and writes it to the stream.</summary>
    public static void Encode(SixelImage image, Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        byte[] bytes = Encode(image);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WritePixels(byte[] bmp, int dataOffset, int stride, SixelImage image, bool alpha)
    {
        int bytesPerPixel = alpha ? 4 : 3;
        for (int y = 0; y < image.Height; y++)
        {
            int target = dataOffset + (image.Height - 1 - y) * stride;
            for (int x = 0; x < image.Width; x++)
            {
                int source = (y * image.Width + x) * SixelImage.BytesPerPixel;
                int o = target + x * bytesPerPixel;
                bmp[o] = image.Pixels[source + 2];
                bmp[o + 1] = image.Pixels[source + 1];
                bmp[o + 2] = image.Pixels[source];
                if (alpha)
                {
                    bmp[o + 3] = image.Pixels[source + 3];
                }
            }
        }
    }

    private static SixelColor[] ReadPalette(byte[] data, BmpHeader header)
    {
        int offset = FileHeaderSize + header.HeaderSize;
        int maxEntries = 1 << header.BitsPerPixel;
        int entries = header.ColorsUsed > 0 ? Math.Min(header.ColorsUsed, maxEntries) : maxEntries;
        if ((long)offset + entries * 4L > data.Length)
        {
            throw new SixelException("BMP palette is truncated.");
        }

        var palette = new SixelColor[entries];
        for (int i = 0; i < entries; i++)
        {
            int o = offset + i * 4;
            palette[i] = new SixelColor(data[o + 2], data[o + 1], data[o]);
        }

        return palette;
    }

    private static SixelColor ReadPixel(byte[] data, int rowOffset, int x, BmpHeader header, SixelColor[] palette)
    {
        switch (header.BitsPerPixel)
        {
            case 1:
                return Lookup(palette, (data[rowOffset + x / 8] >> (7 - x % 8)) & 1);
            case 4:
                byte pair = data[rowOffset + x / 2];
                return Lookup(palette, x % 2 == 0 ? pair >> 4 : pair & 0x0F);
            case 8:
                return Lookup(palette, data[rowOffset + x]);
            case 16:
                return header.Masks.Decode((uint)(data[rowOffset + x * 2] | data[rowOffset + x * 2 + 1] << 8));
            case 24:
                int o = rowOffset + x * 3;
                return new SixelColor(data[o + 2], data[o + 1], data[o]);
            default:
                return header.Masks.Decode((uint)ReadInt32(data, rowOffset + x * 4));
        }
    }

    private static SixelColor Lookup(SixelColor[] palette, int index) =>
        index < palette.Length ? palette[index] : SixelColor.Black;

    private static int ReadInt32(byte[] data, int offset) =>
        data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24;

    private static int ReadInt16(byte[] data, int offset) => data[offset] | data[offset + 1] << 8;

    private static void WriteInt32(byte[] data, int offset, int value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
        data[offset + 2] = (byte)(value >> 16);
        data[offset + 3] = (byte)(value >> 24);
    }

    private static void WriteInt16(byte[] data, int offset, int value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
    }

    private sealed class BmpHeader
    {
        public int HeaderSize { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public bool TopDown { get; private set; }

        public int BitsPerPixel { get; private set; }

        public int ColorsUsed { get; private set; }

        public int DataOffset { get; private set; }

        public int Stride { get; private set; }

        public ChannelMasks Masks { get; private set; }

        public static BmpHeader Parse(byte[] data)
        {
            if (!IsBmp(data) || data.Length < FileHeaderSize + InfoHeaderSize)
            {
                throw new SixelException("The data is not a valid BMP file.");
            }

            var header = new BmpHeader
            {
                DataOffset = ReadInt32(data, 10),
                HeaderSize = ReadInt32(data, 14),
            };
            if (header.HeaderSize < InfoHeaderSize || (long)FileHeaderSize + header.HeaderSize > data.Length)
            {
                throw new SixelException("Unsupported or invalid BMP header.");
            }

            int rawHeight = ReadInt32(data, 22);
            header.Width = ReadInt32(data, 18);
            header.TopDown = rawHeight < 0;
            header.Height = rawHeight == int.MinValue ? 0 : Math.Abs(rawHeight);
            header.BitsPerPixel = ReadInt16(data, 28);
            header.ColorsUsed = ReadInt32(data, 46);
            int compression = ReadInt32(data, 30);

            header.ValidateGeometry(data.Length, compression);
            header.Masks = ChannelMasks.Read(data, header.HeaderSize, header.BitsPerPixel, compression);
            return header;
        }

        private void ValidateGeometry(int dataLength, int compression)
        {
            if (compression == CompressionRle4 || compression == CompressionRle8)
            {
                throw new SixelException("RLE-compressed BMP files are not supported.");
            }

            bool knownCompression = compression == CompressionRgb || compression == CompressionBitfields
                                    || compression == CompressionAlphaBitfields;
            bool knownDepth = BitsPerPixel == 1 || BitsPerPixel == 4 || BitsPerPixel == 8
                              || BitsPerPixel == 16 || BitsPerPixel == 24 || BitsPerPixel == 32;
            if (!knownCompression || !knownDepth)
            {
                throw new SixelException("Unsupported BMP compression or bit depth.");
            }

            if (Width <= 0 || Height <= 0 || (long)Width * Height > MaxPixels)
            {
                throw new SixelException("Invalid BMP dimensions.");
            }

            long stride = ((long)Width * BitsPerPixel + 31) / 32 * 4;
            Stride = (int)stride;
            if (DataOffset < FileHeaderSize + HeaderSize || DataOffset + stride * Height > dataLength)
            {
                throw new SixelException("BMP pixel data is truncated.");
            }
        }
    }

    private readonly struct ChannelMasks
    {
        private readonly Channel _red;
        private readonly Channel _green;
        private readonly Channel _blue;
        private readonly Channel _alpha;

        private ChannelMasks(uint red, uint green, uint blue, uint alpha)
        {
            _red = new Channel(red);
            _green = new Channel(green);
            _blue = new Channel(blue);
            _alpha = new Channel(alpha);
        }

        public static ChannelMasks Read(byte[] data, int headerSize, int bitsPerPixel, int compression)
        {
            if (bitsPerPixel != 16 && bitsPerPixel != 32)
            {
                return default;
            }

            bool explicitMasks = compression == CompressionBitfields || compression == CompressionAlphaBitfields;
            if (!explicitMasks)
            {
                return bitsPerPixel == 16
                    ? new ChannelMasks(0x7C00, 0x03E0, 0x001F, 0)
                    : new ChannelMasks(0x00FF0000, 0x0000FF00, 0x000000FF, 0);
            }

            int offset = FileHeaderSize + InfoHeaderSize;
            bool alphaPresent = headerSize >= 56 || compression == CompressionAlphaBitfields;
            int needed = offset + (alphaPresent ? 16 : 12);
            if (data.Length < needed)
            {
                throw new SixelException("BMP channel masks are truncated.");
            }

            return new ChannelMasks(
                (uint)ReadInt32(data, offset),
                (uint)ReadInt32(data, offset + 4),
                (uint)ReadInt32(data, offset + 8),
                alphaPresent ? (uint)ReadInt32(data, offset + 12) : 0);
        }

        public SixelColor Decode(uint value) =>
            new SixelColor(_red.Extract(value), _green.Extract(value), _blue.Extract(value), _alpha.ExtractOrOpaque(value));
    }

    private readonly struct Channel
    {
        private readonly uint _mask;
        private readonly int _shift;
        private readonly uint _max;

        public Channel(uint mask)
        {
            _mask = mask;
            _shift = 0;
            if (mask != 0)
            {
                while (((mask >> _shift) & 1) == 0)
                {
                    _shift++;
                }
            }

            _max = mask >> _shift;
        }

        public byte Extract(uint value)
        {
            if (_max == 0)
            {
                return 0;
            }

            ulong scaled = ((ulong)((value & _mask) >> _shift) * 255 + _max / 2) / _max;
            return (byte)Math.Min(scaled, 255UL);
        }

        public byte ExtractOrOpaque(uint value) => _max == 0 ? (byte)255 : Extract(value);
    }
}
