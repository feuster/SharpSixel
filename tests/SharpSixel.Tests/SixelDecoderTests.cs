namespace SharpSixel.Tests;

public class SixelDecoderTests
{
    private const string Esc = "\u001b";
    private static readonly SixelColor Red = new(255, 0, 0);
    private static readonly SixelColor Green = new(0, 255, 0);
    private static readonly SixelColor Blue = new(0, 0, 255);
    private static readonly SixelColor Yellow = new(255, 255, 0);
    private static readonly SixelColor Black = new(0, 0, 0);

    private static SixelImage Decode(string data) => new SixelDecoder().Decode(data);

    [Fact]
    public void Decode_SpecExample_RendersYellowRectangleWithGreenHi()
    {
        // Example from Docs/all-about-sixels.txt, section V. Two bands => 12 rows.
        string data = Esc + "Pq\n#0;2;0;0;0#1;2;100;100;0#2;2;0;100;0\n"
                      + "#1~~@@vv@@~~@@~~$\n#2??}}GG}}??}}??-\n#1!14@\n" + Esc + "\\";

        SixelImage image = Decode(data);

        Assert.Equal(14, image.Width);
        Assert.Equal(12, image.Height);
        Assert.Equal(Yellow, image.GetPixel(0, 0));
        Assert.Equal(Green, image.GetPixel(2, 1));
        Assert.Equal(Green, image.GetPixel(4, 3));
        Assert.Equal(Yellow, image.GetPixel(4, 2));
        Assert.Equal(Yellow, image.GetPixel(13, 6));
        Assert.Equal(Black, image.GetPixel(0, 7));
    }

    [Fact]
    public void Decode_RepeatIntroducer_RepeatsNextSixel()
    {
        SixelImage image = Decode(Esc + "Pq#1;2;100;0;0!5~" + Esc + "\\");

        Assert.Equal(5, image.Width);
        Assert.Equal(6, image.Height);
        Assert.Equal(Red, image.GetPixel(4, 5));
    }

    [Fact]
    public void Decode_RepeatZero_IsTreatedAsOne()
    {
        SixelImage image = Decode("#1;2;100;0;0!0~");

        Assert.Equal(1, image.Width);
    }

    [Fact]
    public void Decode_CarriageReturn_OverstrikesCurrentBand()
    {
        SixelImage image = Decode("#1;2;100;0;0@$#2;2;0;100;0A");

        Assert.Equal(1, image.Width);
        Assert.Equal(Red, image.GetPixel(0, 0));
        Assert.Equal(Green, image.GetPixel(0, 1));
    }

    [Fact]
    public void Decode_NewLine_StartsNextBand()
    {
        SixelImage image = Decode("#1;2;100;0;0~-~");

        Assert.Equal(1, image.Width);
        Assert.Equal(12, image.Height);
        Assert.Equal(Red, image.GetPixel(0, 11));
    }

    [Fact]
    public void Decode_RasterAttributes_SetSizeAndAspect()
    {
        SixelImage image = Decode(Esc + "Pq\"2;1;4;3#1;2;0;0;100~" + Esc + "\\");

        Assert.Equal(4, image.Width);
        Assert.Equal(3, image.Height);
        Assert.Equal(2, image.PixelAspectVertical);
        Assert.Equal(1, image.PixelAspectHorizontal);
        Assert.Equal(Blue, image.GetPixel(0, 0));
        Assert.Equal(Black, image.GetPixel(1, 0));
    }

    [Fact]
    public void Decode_DeclaredSizeSmallerThanData_DataWins()
    {
        SixelImage image = Decode("\"1;1;1;1#1;2;100;0;0!3~-~");

        Assert.Equal(3, image.Width);
        Assert.Equal(12, image.Height);
    }

    [Theory]
    [InlineData(120, 50, 100, 255, 0, 0)]
    [InlineData(240, 50, 100, 0, 255, 0)]
    [InlineData(0, 50, 100, 0, 0, 255)]
    [InlineData(0, 100, 0, 255, 255, 255)]
    public void Decode_HlsColor_UsesDecHueConvention(int h, int l, int s, int r, int g, int b)
    {
        SixelImage image = Decode($"#1;1;{h};{l};{s}~");

        Assert.Equal(new SixelColor((byte)r, (byte)g, (byte)b), image.GetPixel(0, 0));
    }

    [Fact]
    public void Decode_RgbPercent_RoundsToNearestByte()
    {
        SixelImage image = Decode("#1;2;50;50;50~");

        Assert.Equal(new SixelColor(128, 128, 128), image.GetPixel(0, 0));
    }

    [Fact]
    public void Decode_UndefinedRegister_UsesVt340Default()
    {
        SixelImage image = Decode("#1~");

        Assert.Equal(new SixelColor(51, 51, 204), image.GetPixel(0, 0));
    }

    [Fact]
    public void Decode_NoColorSelected_UsesWhiteForeground()
    {
        SixelImage image = Decode("~");

        Assert.Equal(new SixelColor(255, 255, 255), image.GetPixel(0, 0));
    }

    [Fact]
    public void Decode_RegisterBeyondLimit_WrapsAround()
    {
        var decoder = new SixelDecoder(new SixelDecoderOptions { MaxColorRegisters = 4 });

        SixelImage image = decoder.Decode("#5;2;100;0;0#1~");

        Assert.Equal(Red, image.GetPixel(0, 0));
    }

    [Fact]
    public void Decode_DefaultBackground_FillsUnsetPixelsOpaque()
    {
        SixelImage image = Decode(Esc + "Pq#1;2;100;0;0~?~" + Esc + "\\");

        Assert.Equal(new SixelColor(0, 0, 0, 255), image.GetPixel(1, 0));
    }

    [Fact]
    public void Decode_P2One_LeavesUnsetPixelsTransparent()
    {
        SixelImage image = Decode(Esc + "P0;1;0q#1;2;100;0;0~?~" + Esc + "\\");

        Assert.Equal(Red, image.GetPixel(0, 0));
        Assert.Equal(SixelColor.Transparent, image.GetPixel(1, 0));
        Assert.True(image.HasTransparency);
    }

    [Fact]
    public void Decode_CustomBackground_IsApplied()
    {
        var decoder = new SixelDecoder(new SixelDecoderOptions { Background = new SixelColor(1, 2, 3) });

        SixelImage image = decoder.Decode("#1;2;100;0;0~?~");

        Assert.Equal(new SixelColor(1, 2, 3), image.GetPixel(1, 0));
    }

    [Theory]
    [InlineData("Pq", 2)]
    [InlineData("P0;0;0q", 2)]
    [InlineData("P2;0;0q", 5)]
    [InlineData("P3q", 3)]
    [InlineData("P7q", 1)]
    public void Decode_MacroParameter_SelectsPixelAspect(string header, int expectedVertical)
    {
        SixelImage image = Decode(Esc + header + "#1~" + Esc + "\\");

        Assert.Equal(expectedVertical, image.PixelAspectVertical);
        Assert.Equal(1, image.PixelAspectHorizontal);
    }

    [Fact]
    public void Decode_EightBitControls_AreSupported()
    {
        SixelImage image = Decode("junk\u0090q#1;2;100;0;0~\u009c~~~");

        Assert.Equal(1, image.Width);
        Assert.Equal(Red, image.GetPixel(0, 0));
    }

    [Fact]
    public void Decode_InputWithoutHeader_IsTreatedAsRawData()
    {
        SixelImage image = Decode("#1;2;100;0;0~~");

        Assert.Equal(2, image.Width);
    }

    [Fact]
    public void Decode_UnknownCharactersAndWhitespace_AreIgnored()
    {
        SixelImage image = Decode(Esc + "Pq#1;2;100;0;0\r\n~\a~\n" + Esc + "\\");

        Assert.Equal(2, image.Width);
    }

    [Fact]
    public void Decode_LineBreakInsideParameters_IsIgnored()
    {
        SixelImage image = Decode("#1;2;10\n0;0;0~");

        Assert.Equal(Red, image.GetPixel(0, 0));
    }

    [Fact]
    public void Decode_ByteArrayAndStream_MatchStringOverload()
    {
        const string data = "#1;2;100;0;0!3~";
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(data);
        var decoder = new SixelDecoder();

        SixelImage fromBytes = decoder.Decode(bytes);
        SixelImage fromStream = decoder.Decode(new MemoryStream(bytes));

        Assert.Equal(Decode(data).Pixels, fromBytes.Pixels);
        Assert.Equal(fromBytes.Pixels, fromStream.Pixels);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Pq")]
    [InlineData("#1;2;100;0;0")]
    public void Decode_NoImageData_Throws(string body)
    {
        Assert.Throws<SixelException>(() => Decode(Esc + body + Esc + "\\"));
    }

    [Fact]
    public void Decode_RepeatBeyondWidthLimit_Throws()
    {
        Assert.Throws<SixelException>(() => Decode("!99999999~"));
    }

    [Fact]
    public void Decode_HugeRepeatNumber_DoesNotOverflow()
    {
        Assert.Throws<SixelException>(() => Decode("!99999999999999999999~"));
    }

    [Fact]
    public void Decode_DeclaredSizeBeyondLimit_Throws()
    {
        Assert.Throws<SixelException>(() => Decode("\"1;1;100000;100000~"));
    }

    [Fact]
    public void Decode_TooManyBands_Throws()
    {
        var decoder = new SixelDecoder(new SixelDecoderOptions { MaxHeight = 12 });

        Assert.Throws<SixelException>(() => decoder.Decode("~-~-~"));
    }

    [Fact]
    public void Decode_PixelLimit_Throws()
    {
        var decoder = new SixelDecoder(new SixelDecoderOptions { MaxPixels = 10 });

        Assert.Throws<SixelException>(() => decoder.Decode("!2~"));
    }

    [Fact]
    public void Constructor_InvalidOptions_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SixelDecoder(new SixelDecoderOptions { MaxWidth = 0 }));
        Assert.Throws<ArgumentNullException>(() => new SixelDecoder(null!));
    }

    [Fact]
    public void Decode_NullInput_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SixelDecoder().Decode((string)null!));
        Assert.Throws<ArgumentNullException>(() => new SixelDecoder().Decode((byte[])null!));
    }
}
