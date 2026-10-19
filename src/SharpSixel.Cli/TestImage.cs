namespace SharpSixel.Cli;

/// <summary>Small built-in test pattern that "check" shows on terminals that support sixel.</summary>
internal static class TestImage
{
    public const int Width = 128;
    public const int Height = 64;

    private const int BarWidth = 16;
    private const int BarRows = 32;
    private const int RampRows = 16;
    private const int GrayLevels = 16;
    private const int SquareSize = 8;

    private static readonly SixelColor[] Bars =
    {
        new SixelColor(255, 255, 255),
        new SixelColor(255, 255, 0),
        new SixelColor(0, 255, 255),
        new SixelColor(0, 255, 0),
        new SixelColor(255, 0, 255),
        new SixelColor(255, 0, 0),
        new SixelColor(0, 0, 255),
        new SixelColor(0, 0, 0),
    };

    /// <summary>
    /// Top: eight color bars. Middle: a gray ramp with 16 steps. Bottom: a checkerboard on the left and a
    /// red-to-blue gradient on the right. Uses few colors, so it encodes losslessly.
    /// </summary>
    public static SixelImage Create()
    {
        var image = new SixelImage(Width, Height);
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                image.SetPixel(x, y, ColorAt(x, y));
            }
        }

        return image;
    }

    private static SixelColor ColorAt(int x, int y)
    {
        if (y < BarRows)
        {
            return Bars[x / BarWidth];
        }

        if (y < BarRows + RampRows)
        {
            byte gray = (byte)(x * GrayLevels / Width * 255 / (GrayLevels - 1));
            return new SixelColor(gray, gray, gray);
        }

        return x < Width / 2 ? Checker(x, y) : Gradient(x);
    }

    private static SixelColor Checker(int x, int y)
    {
        bool light = ((x / SquareSize) + (y / SquareSize)) % 2 == 0;
        return light ? new SixelColor(255, 255, 255) : new SixelColor(0, 0, 0);
    }

    private static SixelColor Gradient(int x)
    {
        int half = Width / 2;
        int step = (x - half) * GrayLevels / half;
        byte red = (byte)(255 - step * 255 / (GrayLevels - 1));
        byte blue = (byte)(step * 255 / (GrayLevels - 1));
        return new SixelColor(red, 0, blue);
    }
}
