using SharpSixel.Cli;

namespace SharpSixel.Tests;

public class TestImageTests
{
    private static readonly SixelImage Image = TestImage.Create();

    [Fact]
    public void Create_HasExpectedSizeAndIsOpaque()
    {
        Assert.Equal(128, Image.Width);
        Assert.Equal(64, Image.Height);
        Assert.False(Image.HasTransparency);
    }

    [Theory]
    [InlineData(8, 255, 255, 255)]
    [InlineData(24, 255, 255, 0)]
    [InlineData(40, 0, 255, 255)]
    [InlineData(56, 0, 255, 0)]
    [InlineData(72, 255, 0, 255)]
    [InlineData(88, 255, 0, 0)]
    [InlineData(104, 0, 0, 255)]
    [InlineData(120, 0, 0, 0)]
    public void TopHalf_ShowsEightColorBars(int x, int r, int g, int b)
    {
        Assert.Equal(new SixelColor((byte)r, (byte)g, (byte)b), Image.GetPixel(x, 10));
        Assert.Equal(new SixelColor((byte)r, (byte)g, (byte)b), Image.GetPixel(x, 31));
    }

    [Fact]
    public void GrayRamp_GoesFromBlackToWhiteWithoutDecreasing()
    {
        Assert.Equal(new SixelColor(0, 0, 0), Image.GetPixel(0, 40));
        Assert.Equal(new SixelColor(255, 255, 255), Image.GetPixel(127, 40));
        for (int x = 1; x < Image.Width; x++)
        {
            Assert.True(Image.GetPixel(x, 40).R >= Image.GetPixel(x - 1, 40).R);
            Assert.Equal(Image.GetPixel(x, 40).R, Image.GetPixel(x, 40).G);
        }
    }

    [Fact]
    public void GrayRamp_HasSixteenSteps()
    {
        var steps = new HashSet<byte>();
        for (int x = 0; x < Image.Width; x++)
        {
            steps.Add(Image.GetPixel(x, 40).R);
        }

        Assert.Equal(16, steps.Count);
    }

    [Fact]
    public void BottomLeft_IsCheckerboardOfEightPixelSquares()
    {
        Assert.Equal(new SixelColor(255, 255, 255), Image.GetPixel(2, 50));
        Assert.Equal(new SixelColor(0, 0, 0), Image.GetPixel(10, 50));
        Assert.Equal(new SixelColor(0, 0, 0), Image.GetPixel(2, 58));
        Assert.Equal(new SixelColor(255, 255, 255), Image.GetPixel(10, 58));
    }

    [Fact]
    public void BottomRight_IsRedToBlueGradient()
    {
        Assert.Equal(new SixelColor(255, 0, 0), Image.GetPixel(64, 60));
        Assert.Equal(new SixelColor(0, 0, 255), Image.GetPixel(127, 60));
        Assert.True(Image.GetPixel(100, 60).R < Image.GetPixel(70, 60).R);
        Assert.True(Image.GetPixel(100, 60).B > Image.GetPixel(70, 60).B);
    }

    [Fact]
    public void Create_UsesFewColors_SoEncodingKeepsEveryPixelWithinPercentPrecision()
    {
        var colors = new HashSet<SixelColor>();
        for (int y = 0; y < Image.Height; y++)
        {
            for (int x = 0; x < Image.Width; x++)
            {
                colors.Add(Image.GetPixel(x, y));
            }
        }

        SixelImage decoded = new SixelDecoder().Decode(new SixelEncoder().Encode(Image));

        Assert.InRange(colors.Count, 8, 64);
        Assert.Equal(Image.Width, decoded.Width);
        Assert.Equal(Image.Height, decoded.Height);
        for (int i = 0; i < Image.Pixels.Length; i++)
        {
            Assert.InRange(Math.Abs(Image.Pixels[i] - decoded.Pixels[i]), 0, 2);
        }
    }
}
