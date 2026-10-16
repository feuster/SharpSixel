using System;

namespace Sixel;

/// <summary>Limits and behavior of <see cref="SixelDecoder"/>. Do not modify after passing to a decoder.</summary>
public sealed class SixelDecoderOptions
{
    /// <summary>Maximum image width in pixels. Default 16384.</summary>
    public int MaxWidth { get; set; } = 16384;

    /// <summary>Maximum image height in pixels. Default 16384.</summary>
    public int MaxHeight { get; set; } = 16384;

    /// <summary>Maximum total pixel count. Default 64 Mi pixels (256 MiB RGBA).</summary>
    public int MaxPixels { get; set; } = 64 * 1024 * 1024;

    /// <summary>Number of color registers; higher register numbers wrap around (modulo). Default 256.</summary>
    public int MaxColorRegisters { get; set; } = 256;

    /// <summary>Color for unset pixels when the stream does not request a transparent background (P2 = 1).</summary>
    public SixelColor Background { get; set; } = SixelColor.Black;

    internal void Validate()
    {
        RequirePositive(MaxWidth, nameof(MaxWidth));
        RequirePositive(MaxHeight, nameof(MaxHeight));
        RequirePositive(MaxColorRegisters, nameof(MaxColorRegisters));
        if (MaxPixels <= 0 || MaxPixels > int.MaxValue / SixelImage.BytesPerPixel)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxPixels));
        }
    }

    private static void RequirePositive(int value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, "Value must be positive.");
        }
    }
}
