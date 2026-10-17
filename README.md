# Sixel

C# encoder/decoder for sixel graphics (DECSIXEL), a CLI, and NuGet packages.

| Package | Purpose |
|---|---|
| `Sixel` | Dependency-free core: `SixelEncoder`, `SixelDecoder`, `SixelImage`, `BmpCodec`, `ImageScaler`, `SixelTerminal` |
| `Sixel.Skia` | SkiaSharp adapter for PNG/JPEG |
| `Sixel.Cli` | Command line tool |

## CLI

```
sixel encode <input> [-o <file>] [--colors <2-256>] [--dither [floyd|bayer|none]]
             [--width <px>] [--height <px>] [--fit] [--upscale]
sixel decode <input> -o <file>      # .bmp, .png, .jpg
sixel check                         # exit 0 if the terminal reports sixel support (DA1)
```

`--dither` reduces banding when colors are cut: `floyd` (error diffusion, default for a bare `--dither`), `bayer` (ordered 8x8, regular pattern, smaller output) or `none`.
`--width`/`--height` shrink the image to fit (aspect ratio kept); `--fit` fits it into the terminal's drawable
area (queried from the terminal); `--upscale` also enlarges small images.

`<input>` may be `-` for stdin. `encode` without `-o` writes sixel to stdout.

## Build, test, pack

```
dotnet test
dotnet pack src/Sixel -c Release -o artifacts/nupkg
dotnet pack src/Sixel.Skia -c Release -o artifacts/nupkg
```

Licensed under MIT.
