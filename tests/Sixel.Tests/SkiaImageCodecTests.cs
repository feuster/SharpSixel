using Sixel.Skia;
using SkiaSharp;

namespace Sixel.Tests;

public class SkiaImageCodecTests
{
    private static SixelImage Sample()
    {
        var image = new SixelImage(5, 4);
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                image.SetPixel(x, y, new SixelColor((byte)(x * 50), (byte)(y * 70), (byte)(x * y * 10)));
            }
        }

        return image;
    }

    [Fact]
    public void Png_RoundTrip_PreservesPixels()
    {
        SixelImage image = Sample();

        SixelImage decoded = SkiaImageCodec.Decode(SkiaImageCodec.Encode(image));

        Assert.Equal(image.Width, decoded.Width);
        Assert.Equal(image.Height, decoded.Height);
        Assert.Equal(image.Pixels, decoded.Pixels);
    }

    [Fact]
    public void Png_FullyTransparentPixel_StaysTransparent()
    {
        SixelImage image = Sample();
        image.SetPixel(2, 2, SixelColor.Transparent);

        SixelImage decoded = SkiaImageCodec.Decode(SkiaImageCodec.Encode(image));

        Assert.Equal(0, decoded.GetPixel(2, 2).A);
        Assert.Equal(255, decoded.GetPixel(0, 0).A);
    }

    [Fact]
    public void Jpeg_Encode_ProducesDecodableImageOfSameSize()
    {
        byte[] jpeg = SkiaImageCodec.Encode(Sample(), SKEncodedImageFormat.Jpeg, 95);

        SixelImage decoded = SkiaImageCodec.Decode(new MemoryStream(jpeg));

        Assert.Equal(5, decoded.Width);
        Assert.Equal(4, decoded.Height);
        Assert.Equal(255, decoded.GetPixel(0, 0).A);
    }

    [Fact]
    public void ToSKBitmap_And_Back_PreservesPixels()
    {
        SixelImage image = Sample();

        using SKBitmap bitmap = SkiaImageCodec.ToSKBitmap(image);
        SixelImage back = SkiaImageCodec.ToSixelImage(bitmap);

        Assert.Equal(5, bitmap.Width);
        Assert.Equal(image.Pixels, back.Pixels);
    }

    [Fact]
    public void Decode_GarbageData_ThrowsSixelException()
    {
        Assert.Throws<SixelException>(() => SkiaImageCodec.Decode(new byte[] { 1, 2, 3, 4, 5 }));
    }

    [Fact]
    public void NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => SkiaImageCodec.Decode((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => SkiaImageCodec.Decode((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => SkiaImageCodec.ToSixelImage(null!));
        Assert.Throws<ArgumentNullException>(() => SkiaImageCodec.ToSKBitmap(null!));
        Assert.Throws<ArgumentNullException>(() => SkiaImageCodec.Encode(null!));
    }
}
