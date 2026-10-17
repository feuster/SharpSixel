namespace SharpSixel.Tests;

public class BmpCodecTests
{
    private static byte[] BuildBmp(int width, int height, int bpp, int compression, byte[] extra, byte[] pixels)
    {
        const int headerSize = 40;
        int dataOffset = 14 + headerSize + extra.Length;
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((byte)'B');
        w.Write((byte)'M');
        w.Write(dataOffset + pixels.Length);
        w.Write(0);
        w.Write(dataOffset);
        w.Write(headerSize);
        w.Write(width);
        w.Write(height);
        w.Write((short)1);
        w.Write((short)bpp);
        w.Write(compression);
        w.Write(pixels.Length);
        w.Write(2835);
        w.Write(2835);
        w.Write(bpp <= 8 ? extra.Length / 4 : 0);
        w.Write(0);
        w.Write(extra);
        w.Write(pixels);
        return ms.ToArray();
    }

    private static byte[] Palette(params (byte R, byte G, byte B)[] colors) =>
        colors.SelectMany(c => new[] { c.B, c.G, c.R, (byte)0 }).ToArray();

    private static SixelImage PatternImage(int width, int height, bool alpha)
    {
        var image = new SixelImage(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image.SetPixel(x, y, new SixelColor((byte)(x * 40), (byte)(y * 60), (byte)(x + y),
                    alpha ? (byte)(x * 50 + 5) : (byte)255));
            }
        }

        return image;
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 2)]
    [InlineData(5, 7)]
    public void Encode_Opaque_RoundTripsAs24Bit(int width, int height)
    {
        SixelImage image = PatternImage(width, height, alpha: false);

        byte[] bmp = BmpCodec.Encode(image);
        SixelImage decoded = BmpCodec.Decode(bmp);

        Assert.Equal(24, bmp[28]);
        Assert.Equal(image.Pixels, decoded.Pixels);
        Assert.Equal(width, decoded.Width);
        Assert.Equal(height, decoded.Height);
    }

    [Fact]
    public void Encode_WithTransparency_RoundTripsAs32BitWithAlpha()
    {
        SixelImage image = PatternImage(4, 3, alpha: true);

        byte[] bmp = BmpCodec.Encode(image);
        SixelImage decoded = BmpCodec.Decode(bmp);

        Assert.Equal(32, bmp[28]);
        Assert.Equal(image.Pixels, decoded.Pixels);
    }

    [Fact]
    public void Encode_ToStream_MatchesByteArray()
    {
        SixelImage image = PatternImage(3, 3, alpha: false);
        using var stream = new MemoryStream();

        BmpCodec.Encode(image, stream);

        Assert.Equal(BmpCodec.Encode(image), stream.ToArray());
        stream.Position = 0;
        Assert.Equal(image.Pixels, BmpCodec.Decode(stream).Pixels);
    }

    [Fact]
    public void Decode_EightBitPalette_BottomUp()
    {
        byte[] palette = Palette((0, 0, 0), (255, 0, 0), (0, 255, 0), (0, 0, 255));
        // bottom row first: [1, 2]; top row: [3, 0]; each row padded to 4 bytes.
        byte[] pixels = { 1, 2, 0, 0, 3, 0, 0, 0 };

        SixelImage image = BmpCodec.Decode(BuildBmp(2, 2, 8, 0, palette, pixels));

        Assert.Equal(new SixelColor(0, 0, 255), image.GetPixel(0, 0));
        Assert.Equal(new SixelColor(0, 0, 0), image.GetPixel(1, 0));
        Assert.Equal(new SixelColor(255, 0, 0), image.GetPixel(0, 1));
        Assert.Equal(new SixelColor(0, 255, 0), image.GetPixel(1, 1));
    }

    [Fact]
    public void Decode_NegativeHeight_IsTopDown()
    {
        byte[] palette = Palette((0, 0, 0), (255, 0, 0));
        byte[] pixels = { 1, 0, 0, 0, 0, 1, 0, 0 };

        SixelImage image = BmpCodec.Decode(BuildBmp(2, -2, 8, 0, palette, pixels));

        Assert.Equal(new SixelColor(255, 0, 0), image.GetPixel(0, 0));
        Assert.Equal(new SixelColor(255, 0, 0), image.GetPixel(1, 1));
        Assert.Equal(new SixelColor(0, 0, 0), image.GetPixel(1, 0));
    }

    [Fact]
    public void Decode_OneBitPalette_ReadsMostSignificantBitFirst()
    {
        byte[] palette = Palette((0, 0, 0), (255, 255, 255));
        byte[] pixels = { 0b10101010, 0, 0, 0 };

        SixelImage image = BmpCodec.Decode(BuildBmp(8, 1, 1, 0, palette, pixels));

        Assert.Equal(new SixelColor(255, 255, 255), image.GetPixel(0, 0));
        Assert.Equal(new SixelColor(0, 0, 0), image.GetPixel(1, 0));
        Assert.Equal(new SixelColor(255, 255, 255), image.GetPixel(6, 0));
    }

    [Fact]
    public void Decode_FourBitPalette_ReadsHighNibbleFirst()
    {
        byte[] palette = Palette((0, 0, 0), (255, 0, 0), (0, 255, 0));
        byte[] pixels = { 0x12, 0, 0, 0 };

        SixelImage image = BmpCodec.Decode(BuildBmp(2, 1, 4, 0, palette, pixels));

        Assert.Equal(new SixelColor(255, 0, 0), image.GetPixel(0, 0));
        Assert.Equal(new SixelColor(0, 255, 0), image.GetPixel(1, 0));
    }

    [Fact]
    public void Decode_SixteenBitBitfields565_ScalesChannels()
    {
        byte[] masks = new byte[12];
        BitConverter.GetBytes(0xF800).CopyTo(masks, 0);
        BitConverter.GetBytes(0x07E0).CopyTo(masks, 4);
        BitConverter.GetBytes(0x001F).CopyTo(masks, 8);
        byte[] pixels = { 0x00, 0xF8, 0xE0, 0x07 };

        SixelImage image = BmpCodec.Decode(BuildBmp(2, 1, 16, 3, masks, pixels));

        Assert.Equal(new SixelColor(255, 0, 0), image.GetPixel(0, 0));
        Assert.Equal(new SixelColor(0, 255, 0), image.GetPixel(1, 0));
    }

    [Fact]
    public void Decode_ThirtyTwoBitRgb_IgnoresUnusedAlphaByte()
    {
        byte[] pixels = { 10, 20, 30, 0 };

        SixelImage image = BmpCodec.Decode(BuildBmp(1, 1, 32, 0, new byte[0], pixels));

        Assert.Equal(new SixelColor(30, 20, 10, 255), image.GetPixel(0, 0));
    }

    [Fact]
    public void IsBmp_DetectsSignature()
    {
        Assert.True(BmpCodec.IsBmp(BmpCodec.Encode(new SixelImage(1, 1))));
        Assert.False(BmpCodec.IsBmp(new byte[] { 0x89, 0x50 }));
        Assert.False(BmpCodec.IsBmp(null!));
    }

    [Fact]
    public void Decode_InvalidData_ThrowsSixelException()
    {
        Assert.Throws<SixelException>(() => BmpCodec.Decode(new byte[10]));
        Assert.Throws<SixelException>(() => BmpCodec.Decode(new byte[100]));
    }

    [Fact]
    public void Decode_TruncatedPixelData_Throws()
    {
        byte[] bmp = BmpCodec.Encode(PatternImage(4, 4, alpha: false));

        Assert.Throws<SixelException>(() => BmpCodec.Decode(bmp.Take(bmp.Length - 5).ToArray()));
    }

    [Fact]
    public void Decode_RleCompression_IsRejected()
    {
        byte[] bmp = BuildBmp(2, 2, 8, 1, Palette((0, 0, 0), (255, 0, 0)), new byte[] { 1, 0, 0, 0 });

        Assert.Throws<SixelException>(() => BmpCodec.Decode(bmp));
    }

    [Fact]
    public void Decode_ZeroWidth_Throws()
    {
        Assert.Throws<SixelException>(() => BmpCodec.Decode(BuildBmp(0, 1, 24, 0, new byte[0], new byte[4])));
    }

    [Fact]
    public void Decode_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => BmpCodec.Decode((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => BmpCodec.Encode(null!));
    }

    [Fact]
    public void BmpToSixelToImage_Pipeline_PreservesExactColors()
    {
        SixelImage original = PatternImage(6, 6, alpha: false);
        // Quantize to a few percent-exact colors so the sixel step is lossless.
        for (int i = 0; i < original.Pixels.Length; i += 4)
        {
            original.Pixels[i] = (byte)(original.Pixels[i] / 51 * 51);
            original.Pixels[i + 1] = (byte)(original.Pixels[i + 1] / 51 * 51);
            original.Pixels[i + 2] = (byte)(original.Pixels[i + 2] / 51 * 51);
        }

        SixelImage viaBmp = BmpCodec.Decode(BmpCodec.Encode(original));
        SixelImage viaSixel = new SixelDecoder().Decode(new SixelEncoder().Encode(viaBmp));

        Assert.Equal(original.Pixels, viaSixel.Pixels);
    }
}
