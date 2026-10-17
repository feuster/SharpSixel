# SharpSixel

C# encoder/decoder for sixel graphics (DECSIXEL), a command line tool, and NuGet packages.
Author: Alexander Feuster. A detailed user manual (German) is in [`Docs/README.txt`](Docs/README.txt).

| Package | Purpose |
|---|---|
| `SharpSixel` | Dependency-free core: `SixelEncoder`, `SixelDecoder`, `SixelImage`, `BmpCodec`, `ImageScaler`, `SixelTerminal` |
| `SharpSixel.Skia` | SkiaSharp adapter for PNG/JPEG |
| `SharpSixel.Cli` | Command line tool (`SharpSixel.exe`) |

## CLI

```
SharpSixel encode <input> [-o <file>] [--colors <2-256>] [--dither [floyd|bayer|none]]
                  [--width <px>] [--height <px>] [--fit] [--upscale]
SharpSixel decode <input> -o <file>      # .bmp, .png, .jpg
SharpSixel check                         # exit 0 if the terminal reports sixel support (DA1)
```

- `<input>` may be `-` for stdin. `encode` without `-o` writes sixel to stdout.
- `--dither` reduces banding when colors are cut: `floyd` (error diffusion, default for a bare `--dither`),
  `bayer` (ordered 8x8, regular pattern, smaller output) or `none`.
- `--width`/`--height` shrink the image to fit (aspect ratio kept); `--fit` fits it into the terminal's drawable
  area (queried from the terminal); `--upscale` also enlarges small images.

## Build and test

```
dotnet test
```

## Standalone executable and NuGet packages (Release only)

```powershell
./publish.ps1                  # -> publish/SharpSixel.exe, publish/README.txt, publish/nuget/*.nupkg
./publish.ps1 -Version 0.2.0   # optional version for exe and packages
```

`SharpSixel.exe` is win-x64, self-contained, single file (no .NET install needed). The user manual is copied next to
it. The packages are built locally and not uploaded; use `publish/nuget` as a local NuGet source. Pack and publish
reject Debug builds.

Licensed under MIT.
