using System;
using System.IO;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace Sixel.Skia;

/// <summary>Converts between <see cref="SixelImage"/> and SkiaSharp bitmaps / encoded image formats.</summary>
public static class SkiaImageCodec
{
    private const string UnsupportedMessage = "The data is not a supported image format.";

    /// <summary>Decodes an encoded image (PNG, JPEG, ...) into a <see cref="SixelImage"/>.</summary>
    /// <exception cref="SixelException">The data is not a supported image.</exception>
    public static SixelImage Decode(byte[] data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        using (var stream = new MemoryStream(data))
        using (SKCodec? codec = SKCodec.Create(stream))
        {
            if (codec == null)
            {
                throw new SixelException(UnsupportedMessage);
            }

            using (SKBitmap? bitmap = SKBitmap.Decode(codec))
            {
                if (bitmap == null)
                {
                    throw new SixelException(UnsupportedMessage);
                }

                return ToSixelImage(bitmap);
            }
        }
    }

    /// <summary>Decodes an encoded image stream into a <see cref="SixelImage"/>.</summary>
    public static SixelImage Decode(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        using (var buffer = new MemoryStream())
        {
            stream.CopyTo(buffer);
            return Decode(buffer.ToArray());
        }
    }

    /// <summary>Copies a bitmap into a <see cref="SixelImage"/> (RGBA, straight alpha).</summary>
    public static SixelImage ToSixelImage(SKBitmap bitmap)
    {
        if (bitmap == null)
        {
            throw new ArgumentNullException(nameof(bitmap));
        }

        var info = new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var pixels = new byte[info.BytesSize];
        GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            using (SKImage image = SKImage.FromBitmap(bitmap))
            {
                if (!image.ReadPixels(info, handle.AddrOfPinnedObject(), info.RowBytes, 0, 0))
                {
                    throw new SixelException("Pixel data could not be read from the bitmap.");
                }
            }
        }
        finally
        {
            handle.Free();
        }

        return new SixelImage(info.Width, info.Height, pixels);
    }

    /// <summary>Creates an <see cref="SKBitmap"/> (RGBA8888, unpremultiplied) from the image. The caller disposes it.</summary>
    public static SKBitmap ToSKBitmap(SixelImage image)
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        var bitmap = new SKBitmap(new SKImageInfo(image.Width, image.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        Marshal.Copy(image.Pixels, 0, bitmap.GetPixels(), image.Pixels.Length);
        return bitmap;
    }

    /// <summary>Encodes the image, by default as PNG.</summary>
    /// <param name="image">Image to encode.</param>
    /// <param name="format">Target format.</param>
    /// <param name="quality">Quality 0-100 (ignored by lossless formats).</param>
    public static byte[] Encode(SixelImage image, SKEncodedImageFormat format = SKEncodedImageFormat.Png, int quality = 90)
    {
        using (SKBitmap bitmap = ToSKBitmap(image))
        using (SKImage skImage = SKImage.FromBitmap(bitmap))
        using (SKData? data = skImage.Encode(format, quality))
        {
            if (data == null)
            {
                throw new SixelException("The image could not be encoded in the requested format.");
            }

            return data.ToArray();
        }
    }
}
