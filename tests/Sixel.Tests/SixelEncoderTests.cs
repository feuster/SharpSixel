namespace Sixel.Tests;

public class SixelEncoderTests
{
    private const string Esc = "\u001b";
    private static readonly SixelColor Red = new(255, 0, 0);
    private static readonly SixelColor Blue = new(0, 0, 255);

    private static SixelImage Filled(int width, int height, SixelColor color)
    {
        var image = new SixelImage(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image.SetPixel(x, y, color);
            }
        }

        return image;
    }

    [Fact]
    public void Encode_SingleColor_ProducesExactSequence()
    {
        string sixel = new SixelEncoder().Encode(Filled(2, 1, Red));

        Assert.Equal(Esc + "P0;0;0q\"1;1;2;1#0;2;100;0;0#0@@" + Esc + "\\", sixel);
    }

    [Theory]
    [InlineData(3, "@@@")]
    [InlineData(4, "!4@")]
    [InlineData(300, "!300@")]
    public void Encode_RunLength_UsesRepeatOnlyWhenShorter(int width, string expectedRow)
    {
        string sixel = new SixelEncoder().Encode(Filled(width, 1, Red));

        Assert.Contains("#0" + expectedRow + Esc + "\\", sixel);
    }

    [Fact]
    public void Encode_TwoColorsInOneBand_SeparatedByCarriageReturn()
    {
        var image = new SixelImage(2, 1);
        image.SetPixel(0, 0, Red);
        image.SetPixel(1, 0, Blue);

        string sixel = new SixelEncoder().Encode(image);

        Assert.Contains("#0@$#1?@", sixel);
    }

    [Fact]
    public void Encode_MultipleBands_SeparatedByNewLine()
    {
        string sixel = new SixelEncoder().Encode(Filled(1, 7, Red));

        Assert.Contains("#0~-#0@", sixel);
    }

    [Fact]
    public void Encode_ExactPalette_RoundTripsLosslessly()
    {
        SixelColor[] colors =
        {
            new(0, 0, 0), new(255, 255, 255), new(51, 102, 153), new(204, 51, 0), new(255, 0, 255),
        };
        var image = new SixelImage(13, 14);
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                image.SetPixel(x, y, colors[(x * 3 + y * 5 + x * y) % colors.Length]);
            }
        }

        SixelImage decoded = new SixelDecoder().Decode(new SixelEncoder().Encode(image));

        Assert.Equal(image.Width, decoded.Width);
        Assert.Equal(image.Height, decoded.Height);
        Assert.Equal(image.Pixels, decoded.Pixels);
    }

    [Fact]
    public void Encode_Grayscale256_RoundTripsWithinPercentPrecision()
    {
        var image = new SixelImage(256, 1);
        for (int x = 0; x < 256; x++)
        {
            image.SetPixel(x, 0, new SixelColor((byte)x, (byte)x, (byte)x));
        }

        SixelImage decoded = new SixelDecoder().Decode(new SixelEncoder().Encode(image));

        for (int x = 0; x < 256; x++)
        {
            Assert.InRange(Math.Abs(decoded.GetPixel(x, 0).R - x), 0, 2);
        }
    }

    [Fact]
    public void Encode_TransparentPixels_SetP2AndSurviveRoundTrip()
    {
        var image = Filled(3, 2, Red);
        image.SetPixel(1, 0, SixelColor.Transparent);

        string sixel = new SixelEncoder().Encode(image);
        SixelImage decoded = new SixelDecoder().Decode(sixel);

        Assert.StartsWith(Esc + "P0;1;0q", sixel);
        Assert.Equal(SixelColor.Transparent, decoded.GetPixel(1, 0));
        Assert.Equal(Red, decoded.GetPixel(0, 0));
        Assert.Equal(Red, decoded.GetPixel(2, 1));
    }

    [Fact]
    public void Encode_FullyTransparentImage_KeepsSizeViaRasterAttributes()
    {
        var image = new SixelImage(5, 4);

        SixelImage decoded = new SixelDecoder().Decode(new SixelEncoder().Encode(image));

        Assert.Equal(5, decoded.Width);
        Assert.Equal(4, decoded.Height);
        Assert.DoesNotContain(decoded.Pixels, b => b != 0);
    }

    [Fact]
    public void Encode_AlphaThresholdZero_DisablesTransparency()
    {
        var encoder = new SixelEncoder(new SixelEncoderOptions { AlphaThreshold = 0 });

        string sixel = encoder.Encode(new SixelImage(2, 2));

        Assert.StartsWith(Esc + "P0;0;0q", sixel);
    }

    [Fact]
    public void Encode_MoreColorsThanAllowed_ReducesPaletteAndKeepsHues()
    {
        var image = new SixelImage(32, 32);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                image.SetPixel(x, y, x < 16
                    ? new SixelColor((byte)(200 + (x * 7 + y) % 56), 0, 0)
                    : new SixelColor(0, 0, (byte)(200 + (x * 5 + y) % 56)));
            }
        }

        string sixel = new SixelEncoder(new SixelEncoderOptions { MaxColors = 2 }).Encode(image);
        SixelImage decoded = new SixelDecoder().Decode(sixel);

        Assert.Equal(2, CountColorDefinitions(sixel));
        Assert.True(decoded.GetPixel(3, 3).R > 150 && decoded.GetPixel(3, 3).B < 100);
        Assert.True(decoded.GetPixel(30, 30).B > 150 && decoded.GetPixel(30, 30).R < 100);
    }

    [Fact]
    public void Encode_ManyColors_NeverExceedsMaxColors()
    {
        var image = new SixelImage(64, 64);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                image.SetPixel(x, y, new SixelColor((byte)(x * 4), (byte)(y * 4), (byte)((x + y) * 2)));
            }
        }

        string sixel = new SixelEncoder(new SixelEncoderOptions { MaxColors = 16 }).Encode(image);

        Assert.InRange(CountColorDefinitions(sixel), 2, 16);
        Assert.Equal(64, new SixelDecoder().Decode(sixel).Width);
    }

    [Fact]
    public void Encode_PixelAspect_IsWrittenAndRestored()
    {
        SixelImage image = Filled(2, 6, Red);
        image.PixelAspectVertical = 2;

        SixelImage decoded = new SixelDecoder().Decode(new SixelEncoder().Encode(image));

        Assert.Equal(2, decoded.PixelAspectVertical);
        Assert.Equal(1, decoded.PixelAspectHorizontal);
    }

    [Fact]
    public void Encode_WithoutRasterAttributes_UsesSquarePixelMacro()
    {
        var encoder = new SixelEncoder(new SixelEncoderOptions { EmitRasterAttributes = false });

        string sixel = encoder.Encode(Filled(2, 6, Red));

        Assert.StartsWith(Esc + "P7;0;0q#0;", sixel);
        Assert.DoesNotContain("\"", sixel);
        Assert.Equal(2, new SixelDecoder().Decode(sixel).Width);
    }

    [Fact]
    public void Encode_Overloads_ProduceSameOutput()
    {
        SixelImage image = Filled(5, 5, Blue);
        var encoder = new SixelEncoder();
        var writer = new StringWriter();

        encoder.Encode(image, writer);

        Assert.Equal(encoder.Encode(image), writer.ToString());
        Assert.Equal(System.Text.Encoding.ASCII.GetBytes(encoder.Encode(image)), encoder.EncodeToBytes(image));
    }

    [Fact]
    public void Encode_OutputIsPureSevenBitAscii()
    {
        string sixel = new SixelEncoder().Encode(Filled(9, 9, Red));

        Assert.All(sixel, c => Assert.True(c < 128));
    }

    private static SixelImage GrayGradient(int width, int height)
    {
        var image = new SixelImage(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte gray = (byte)(x * 4);
                image.SetPixel(x, y, new SixelColor(gray, gray, gray));
            }
        }

        return image;
    }

    private static SixelImage RoundTrip(SixelImage image, SixelDithering dithering, int maxColors)
    {
        var encoder = new SixelEncoder(new SixelEncoderOptions { MaxColors = maxColors, Dithering = dithering });
        return new SixelDecoder().Decode(encoder.Encode(image));
    }

    private static int Transitions(SixelImage image, int y)
    {
        int count = 0;
        for (int x = 1; x < image.Width; x++)
        {
            if (image.GetPixel(x, y) != image.GetPixel(x - 1, y))
            {
                count++;
            }
        }

        return count;
    }

    [Fact]
    public void Dithering_None_ProducesHardEdgeWithTwoColors()
    {
        SixelImage decoded = RoundTrip(GrayGradient(64, 8), SixelDithering.None, 2);

        Assert.Equal(1, Transitions(decoded, 0));
    }

    [Theory]
    [InlineData(SixelDithering.FloydSteinberg)]
    [InlineData(SixelDithering.Ordered)]
    public void Dithering_MixesColorsAndPreservesAverageBrightness(SixelDithering dithering)
    {
        SixelImage original = GrayGradient(64, 8);

        SixelImage decoded = RoundTrip(original, dithering, 2);

        Assert.True(Transitions(decoded, 3) > 3);
        double originalMean = Enumerable.Range(0, 64).Average(x => (double)original.GetPixel(x, 0).R);
        double decodedMean = Enumerable.Range(0, 64).SelectMany(x => Enumerable.Range(0, 8).Select(y => (double)decoded.GetPixel(x, y).R)).Average();
        Assert.InRange(Math.Abs(decodedMean - originalMean), 0, 15);
    }

    [Theory]
    [InlineData(SixelDithering.FloydSteinberg)]
    [InlineData(SixelDithering.Ordered)]
    public void Dithering_ExactPalette_StaysLossless(SixelDithering dithering)
    {
        var image = new SixelImage(4, 2);
        SixelColor[] colors = { Red, Blue, new(0, 255, 0), SixelColor.Black };
        for (int i = 0; i < 8; i++)
        {
            image.SetPixel(i % 4, i / 4, colors[(i * 3) % 4]);
        }

        SixelImage decoded = RoundTrip(image, dithering, 8);

        Assert.Equal(image.Pixels, decoded.Pixels);
    }

    [Theory]
    [InlineData(SixelDithering.FloydSteinberg)]
    [InlineData(SixelDithering.Ordered)]
    public void Dithering_KeepsTransparentPixelsTransparent(SixelDithering dithering)
    {
        SixelImage image = GrayGradient(16, 16);
        image.SetPixel(3, 3, SixelColor.Transparent);

        SixelImage decoded = RoundTrip(image, dithering, 4);

        Assert.Equal(0, decoded.GetPixel(3, 3).A);
        Assert.Equal(255, decoded.GetPixel(4, 3).A);
    }

    [Fact]
    public void Dithering_Ordered_IsPeriodicWithBayerSize()
    {
        SixelImage decoded = RoundTrip(GrayGradient(64, 16), SixelDithering.Ordered, 2);

        for (int x = 0; x < 64; x++)
        {
            Assert.Equal(decoded.GetPixel(x, 1), decoded.GetPixel(x, 9));
        }

        Assert.NotEqual(decoded.GetPixel(30, 0), decoded.GetPixel(30, 1));
    }

    [Fact]
    public void Dithering_Ordered_IsDeterministic()
    {
        SixelImage image = GrayGradient(64, 16);
        var ordered = new SixelEncoder(new SixelEncoderOptions { MaxColors = 2, Dithering = SixelDithering.Ordered });

        Assert.Equal(ordered.Encode(image), ordered.Encode(image));
    }

    [Fact]
    public void Dithering_Ordered_UsesOnlyPaletteColors()
    {
        SixelImage decoded = RoundTrip(GrayGradient(64, 8), SixelDithering.Ordered, 4);

        var distinct = new HashSet<SixelColor>();
        for (int x = 0; x < 64; x++)
        {
            distinct.Add(decoded.GetPixel(x, 2));
        }

        Assert.InRange(distinct.Count, 2, 4);
    }

    [Fact]
    public void Constructor_InvalidDithering_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SixelEncoder(new SixelEncoderOptions { Dithering = (SixelDithering)5 }));
    }

    [Fact]
    public void Constructor_InvalidOptions_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SixelEncoder(new SixelEncoderOptions { MaxColors = 1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SixelEncoder(new SixelEncoderOptions { MaxColors = 257 }));
        Assert.Throws<ArgumentNullException>(() => new SixelEncoder(null!));
    }

    [Fact]
    public void Encode_NullArguments_Throw()
    {
        var encoder = new SixelEncoder();

        Assert.Throws<ArgumentNullException>(() => encoder.Encode(null!));
        Assert.Throws<ArgumentNullException>(() => encoder.Encode(Filled(1, 1, Red), (TextWriter)null!));
    }

    private static int CountColorDefinitions(string sixel) =>
        System.Text.RegularExpressions.Regex.Matches(sixel, @"#\d+;2;").Count;
}
