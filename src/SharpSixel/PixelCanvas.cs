using System;

namespace SharpSixel;

/// <summary>Growable RGBA pixel buffer that enforces size limits.</summary>
internal sealed class PixelCanvas
{
    private readonly int _maxWidth;
    private readonly int _maxHeight;
    private readonly long _maxPixels;

    public PixelCanvas(int maxWidth, int maxHeight, long maxPixels)
    {
        _maxWidth = maxWidth;
        _maxHeight = maxHeight;
        _maxPixels = maxPixels;
        Pixels = new byte[0];
    }

    /// <summary>Allocated width (row stride in pixels).</summary>
    public int Width { get; private set; }

    /// <summary>Allocated height.</summary>
    public int Height { get; private set; }

    public byte[] Pixels { get; private set; }

    public void Ensure(int width, int height)
    {
        if (width <= Width && height <= Height)
        {
            return;
        }

        int newWidth = width <= Width ? Width : Grow(Width, width, _maxWidth);
        int newHeight = height <= Height ? Height : Grow(Height, height, _maxHeight);
        if (width > _maxWidth || height > _maxHeight || (long)newWidth * newHeight > _maxPixels)
        {
            throw new SixelException("The sixel image exceeds the configured size limits.");
        }

        var grown = new byte[newWidth * newHeight * SixelImage.BytesPerPixel];
        for (int row = 0; row < Height; row++)
        {
            Buffer.BlockCopy(
                Pixels,
                row * Width * SixelImage.BytesPerPixel,
                grown,
                row * newWidth * SixelImage.BytesPerPixel,
                Width * SixelImage.BytesPerPixel);
        }

        Pixels = grown;
        Width = newWidth;
        Height = newHeight;
    }

    /// <summary>Paints <paramref name="count"/> pixels of one row; the area must have been ensured.</summary>
    public void Fill(int x, int y, int count, SixelColor color)
    {
        int index = (y * Width + x) * SixelImage.BytesPerPixel;
        for (int i = 0; i < count; i++)
        {
            Pixels[index] = color.R;
            Pixels[index + 1] = color.G;
            Pixels[index + 2] = color.B;
            Pixels[index + 3] = color.A;
            index += SixelImage.BytesPerPixel;
        }
    }

    /// <summary>Returns a tightly packed copy of the given size; areas outside the canvas stay zero.</summary>
    public byte[] CropTo(int width, int height)
    {
        var result = new byte[width * height * SixelImage.BytesPerPixel];
        int columns = Math.Min(width, Width);
        int rows = Math.Min(height, Height);
        for (int row = 0; row < rows; row++)
        {
            Buffer.BlockCopy(
                Pixels,
                row * Width * SixelImage.BytesPerPixel,
                result,
                row * width * SixelImage.BytesPerPixel,
                columns * SixelImage.BytesPerPixel);
        }

        return result;
    }

    private static int Grow(int current, int needed, int max) =>
        (int)Math.Min(Math.Max((long)needed, (long)current * 2), max);
}
