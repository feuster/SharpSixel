using System;

namespace SharpSixel;

/// <summary>A raster image stored as RGBA8888 (straight alpha), row-major, top row first.</summary>
public sealed class SixelImage
{
    /// <summary>Number of bytes per pixel (R, G, B, A).</summary>
    public const int BytesPerPixel = 4;

    private int _aspectVertical = 1;
    private int _aspectHorizontal = 1;

    /// <summary>Creates a fully transparent image.</summary>
    public SixelImage(int width, int height)
        : this(width, height, new byte[ByteCount(width, height)])
    {
    }

    /// <summary>Creates an image that wraps the given RGBA buffer (no copy).</summary>
    public SixelImage(int width, int height, byte[] pixels)
    {
        if (pixels == null)
        {
            throw new ArgumentNullException(nameof(pixels));
        }

        if (pixels.Length != ByteCount(width, height))
        {
            throw new ArgumentException("Buffer length must be width * height * 4.", nameof(pixels));
        }

        Width = width;
        Height = height;
        Pixels = pixels;
    }

    /// <summary>Width in pixels.</summary>
    public int Width { get; }

    /// <summary>Height in pixels.</summary>
    public int Height { get; }

    /// <summary>The RGBA pixel buffer.</summary>
    public byte[] Pixels { get; }

    /// <summary>Vertical part of the pixel aspect ratio (vertical:horizontal). Default 1.</summary>
    public int PixelAspectVertical
    {
        get => _aspectVertical;
        set => _aspectVertical = RequirePositive(value, nameof(value));
    }

    /// <summary>Horizontal part of the pixel aspect ratio (vertical:horizontal). Default 1.</summary>
    public int PixelAspectHorizontal
    {
        get => _aspectHorizontal;
        set => _aspectHorizontal = RequirePositive(value, nameof(value));
    }

    /// <summary>True if at least one pixel is not fully opaque.</summary>
    public bool HasTransparency
    {
        get
        {
            for (int i = 3; i < Pixels.Length; i += BytesPerPixel)
            {
                if (Pixels[i] != 255)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Gets the pixel at the given position.</summary>
    public SixelColor GetPixel(int x, int y)
    {
        int i = IndexOf(x, y);
        return new SixelColor(Pixels[i], Pixels[i + 1], Pixels[i + 2], Pixels[i + 3]);
    }

    /// <summary>Sets the pixel at the given position.</summary>
    public void SetPixel(int x, int y, SixelColor color)
    {
        int i = IndexOf(x, y);
        Pixels[i] = color.R;
        Pixels[i + 1] = color.G;
        Pixels[i + 2] = color.B;
        Pixels[i + 3] = color.A;
    }

    private int IndexOf(int x, int y)
    {
        if (x < 0 || x >= Width)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        if (y < 0 || y >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y));
        }

        return (y * Width + x) * BytesPerPixel;
    }

    private static long ByteCount(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");
        }

        long count = (long)width * height * BytesPerPixel;
        if (count > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Image is too large.");
        }

        return count;
    }

    private static int RequirePositive(int value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, "Value must be positive.");
        }

        return value;
    }
}
