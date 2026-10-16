# Sixel.Skia

SkiaSharp adapter for [Sixel](https://www.nuget.org/packages/Sixel): converts `SixelImage` from/to
`SKBitmap` and reads/writes PNG, JPEG and other formats supported by SkiaSharp.

```csharp
SixelImage image = SkiaImageCodec.Decode(File.ReadAllBytes("in.png"));
string sixel = new SixelEncoder().Encode(image);
File.WriteAllBytes("out.png", SkiaImageCodec.Encode(new SixelDecoder().Decode(sixel)));
```

SkiaSharp ships native assets for Windows and macOS; on Linux add `SkiaSharp.NativeAssets.Linux`.
