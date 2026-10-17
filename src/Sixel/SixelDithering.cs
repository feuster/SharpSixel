namespace Sixel;

/// <summary>Error diffusion applied when reducing an image to fewer colors.</summary>
public enum SixelDithering
{
    /// <summary>Nearest palette color per pixel.</summary>
    None = 0,

    /// <summary>Floyd-Steinberg error diffusion.</summary>
    FloydSteinberg = 1,
}
