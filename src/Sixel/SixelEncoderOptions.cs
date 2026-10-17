using System;

namespace Sixel;

/// <summary>Settings of <see cref="SixelEncoder"/>. Do not modify after passing to an encoder.</summary>
public sealed class SixelEncoderOptions
{
    /// <summary>Maximum number of palette colors (2-256). Default 256.</summary>
    public int MaxColors { get; set; } = 256;

    /// <summary>Pixels with an alpha value below this threshold become transparent. Default 128; 0 disables transparency.</summary>
    public byte AlphaThreshold { get; set; } = 128;

    /// <summary>Emit the raster attributes command with aspect ratio and image size. Default true.</summary>
    public bool EmitRasterAttributes { get; set; } = true;

    /// <summary>Dithering used when the image has more colors than <see cref="MaxColors"/>. Default none.</summary>
    public SixelDithering Dithering { get; set; } = SixelDithering.None;

    internal void Validate()
    {
        if (!Enum.IsDefined(typeof(SixelDithering), Dithering))
        {
            throw new ArgumentOutOfRangeException(nameof(Dithering));
        }

        if (MaxColors < 2 || MaxColors > 256)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxColors), "MaxColors must be between 2 and 256.");
        }
    }
}
