namespace Sixel;

/// <summary>Dithering applied when reducing an image to fewer colors.</summary>
public enum SixelDithering
{
    /// <summary>Nearest palette color per pixel.</summary>
    None = 0,

    /// <summary>Floyd-Steinberg error diffusion (smooth, content dependent).</summary>
    FloydSteinberg = 1,

    /// <summary>Ordered dithering with an 8x8 Bayer matrix (regular pattern, position dependent, compresses well).</summary>
    Ordered = 2,
}
