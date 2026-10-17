# SharpSixel

Dependency-free sixel (DECSIXEL) encoder and decoder for .NET (`netstandard2.0`, `net10.0`).

```csharp
using SharpSixel;

SixelImage image = BmpCodec.Decode(File.ReadAllBytes("in.bmp"));
string sixel = new SixelEncoder().Encode(image);          // ESC P ... ESC \
SixelImage back = new SixelDecoder().Decode(sixel);
```

- `SixelImage`: RGBA8888 pixel buffer (straight alpha) plus pixel aspect ratio.
- `SixelEncoder`: median-cut quantization (up to 256 colors), run-length encoding, transparency.
- `SixelDecoder`: color registers (RGB/HLS), repeat, raster attributes, aspect ratio, size limits.
- `BmpCodec`: BMP read (1/4/8/16/24/32 bit) and write (24/32 bit).

PNG/JPEG support lives in the separate `SharpSixel.Skia` package (SkiaSharp).
