namespace SharpSixel.Tests;

public class ImageScalerTests
{
    private static SixelImage Solid(int width, int height, SixelColor color)
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

    [Theory]
    [InlineData(1000, 500, 100, 100, false, 100, 50)]
    [InlineData(1000, 500, 800, null, false, 800, 400)]
    [InlineData(1000, 500, null, 100, false, 200, 100)]
    [InlineData(10, 10, 100, 100, false, 10, 10)]
    [InlineData(10, 10, 100, 100, true, 100, 100)]
    [InlineData(10, 5, 100, 1000, true, 100, 50)]
    [InlineData(1000, 1, 10, null, false, 10, 1)]
    [InlineData(100, 100, null, null, true, 100, 100)]
    public void FitSize_KeepsAspectRatioWithinLimits(
        int sourceW, int sourceH, int? maxW, int? maxH, bool upscale, int expectedW, int expectedH)
    {
        ImageScaler.FitSize(sourceW, sourceH, maxW, maxH, upscale, out int width, out int height);

        Assert.Equal(expectedW, width);
        Assert.Equal(expectedH, height);
    }

    [Fact]
    public void FitSize_NeverReturnsZero()
    {
        ImageScaler.FitSize(1000, 10, 5, null, false, out int width, out int height);

        Assert.Equal(5, width);
        Assert.Equal(1, height);
    }

    [Fact]
    public void FitSize_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageScaler.FitSize(0, 1, null, null, false, out _, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageScaler.FitSize(1, 1, 0, null, false, out _, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageScaler.FitSize(1, 1, null, -3, false, out _, out _));
    }

    [Fact]
    public void Fit_ImageAlreadySmallEnough_ReturnsSameInstance()
    {
        SixelImage image = Solid(10, 10, SixelColor.Black);

        Assert.Same(image, ImageScaler.Fit(image, 100, 100));
        Assert.Same(image, ImageScaler.Fit(image, null, null));
    }

    [Fact]
    public void Fit_LargeImage_IsShrunkProportionally()
    {
        SixelImage scaled = ImageScaler.Fit(Solid(40, 20, new SixelColor(9, 8, 7)), 10, 10);

        Assert.Equal(10, scaled.Width);
        Assert.Equal(5, scaled.Height);
        Assert.Equal(new SixelColor(9, 8, 7), scaled.GetPixel(4, 2));
    }

    [Fact]
    public void Resize_Downscale_AveragesCoveredPixels()
    {
        var image = new SixelImage(2, 2);
        image.SetPixel(0, 0, new SixelColor(255, 0, 0));
        image.SetPixel(1, 0, new SixelColor(0, 0, 0));
        image.SetPixel(0, 1, new SixelColor(0, 0, 0));
        image.SetPixel(1, 1, new SixelColor(255, 0, 0));

        SixelImage scaled = ImageScaler.Resize(image, 1, 1);

        Assert.Equal(new SixelColor(128, 0, 0), scaled.GetPixel(0, 0));
    }

    [Fact]
    public void Resize_NonIntegerDownscale_PreservesSolidColor()
    {
        SixelImage scaled = ImageScaler.Resize(Solid(7, 5, new SixelColor(10, 200, 30)), 3, 2);

        for (int y = 0; y < 2; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                Assert.Equal(new SixelColor(10, 200, 30), scaled.GetPixel(x, y));
            }
        }
    }

    [Fact]
    public void Resize_Upscale_InterpolatesBetweenPixels()
    {
        var image = new SixelImage(2, 1);
        image.SetPixel(0, 0, new SixelColor(0, 0, 0));
        image.SetPixel(1, 0, new SixelColor(255, 255, 255));

        SixelImage scaled = ImageScaler.Resize(image, 8, 1);

        Assert.Equal(0, scaled.GetPixel(0, 0).R);
        Assert.Equal(255, scaled.GetPixel(7, 0).R);
        for (int x = 1; x < 8; x++)
        {
            Assert.True(scaled.GetPixel(x, 0).R >= scaled.GetPixel(x - 1, 0).R);
        }

        Assert.InRange(scaled.GetPixel(3, 0).R, 60, 140);
    }

    [Fact]
    public void Resize_Transparency_DoesNotBleedColor()
    {
        var image = new SixelImage(2, 1);
        image.SetPixel(0, 0, new SixelColor(255, 0, 0));
        image.SetPixel(1, 0, SixelColor.Transparent);

        SixelColor result = ImageScaler.Resize(image, 1, 1).GetPixel(0, 0);

        Assert.Equal(255, result.R);
        Assert.Equal(0, result.G);
        Assert.Equal(0, result.B);
        Assert.InRange(result.A, 127, 128);
    }

    [Fact]
    public void Resize_FullyTransparent_StaysTransparent()
    {
        SixelImage scaled = ImageScaler.Resize(new SixelImage(4, 4), 2, 2);

        Assert.DoesNotContain(scaled.Pixels, b => b != 0);
    }

    [Fact]
    public void Resize_SameSize_CopiesPixels()
    {
        SixelImage image = Solid(3, 3, new SixelColor(1, 2, 3));
        image.SetPixel(1, 1, new SixelColor(200, 100, 50));

        SixelImage copy = ImageScaler.Resize(image, 3, 3);

        Assert.NotSame(image, copy);
        Assert.Equal(image.Pixels, copy.Pixels);
    }

    [Fact]
    public void Resize_PreservesPixelAspect()
    {
        SixelImage image = Solid(4, 4, SixelColor.Black);
        image.PixelAspectVertical = 2;

        SixelImage scaled = ImageScaler.Resize(image, 2, 2);

        Assert.Equal(2, scaled.PixelAspectVertical);
        Assert.Equal(1, scaled.PixelAspectHorizontal);
    }

    [Fact]
    public void Resize_InvalidArguments_Throw()
    {
        SixelImage image = Solid(2, 2, SixelColor.Black);

        Assert.Throws<ArgumentNullException>(() => ImageScaler.Resize(null!, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageScaler.Resize(image, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageScaler.Resize(image, 1, 0));
        Assert.Throws<ArgumentNullException>(() => ImageScaler.Fit(null!, 1, 1));
    }
}
