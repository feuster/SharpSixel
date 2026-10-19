# SharpSixel

C# encoder/decoder for sixel graphics (DECSIXEL), a command line tool, and NuGet packages.
Author: Alexander Feuster. Detailed user manuals: [`Docs/README.txt`](Docs/README.txt) (German) and
[`Docs/README.en.txt`](Docs/README.en.txt) (English).

| Package | Purpose |
|---|---|
| `SharpSixel` | Dependency-free core: `SixelEncoder`, `SixelDecoder`, `SixelImage`, `BmpCodec`, `ImageScaler`, `SixelTerminal` |
| `SharpSixel.Skia` | SkiaSharp adapter for PNG/JPEG |
| `SharpSixel.Cli` | Command line tool (`SharpSixel.exe`) |

## CLI

```
SharpSixel encode <input> [-o <file>] [--show] [--colors <2-256>] [--dither [floyd|bayer|none]]
                  [--width <px>] [--height <px>] [--fit] [--upscale]
SharpSixel decode <input> -o <file>      # .bmp, .png, .jpg
SharpSixel check                         # exit 0 if the terminal reports sixel support (DA1); shows a small test image
```

- `<input>` may be `-` for stdin. `encode` without `-o` writes sixel to stdout. A sixel file (header `ESC P ... q`) is
  also accepted as input; it is passed through unchanged unless size/color options are given.
- `--show` (encode only) displays the sixel in the terminal: without `-o` it is only displayed, with `-o` it is saved
  and displayed. Standard output must be a terminal.
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
./publish.ps1                  # -> publish/SharpSixel.exe, publish/README.txt, publish/README.en.txt, publish/nuget/*.nupkg
./publish.ps1 -Version 0.2.0   # optional version for exe and packages
```

`SharpSixel.exe` is win-x64, self-contained, single file (no .NET install needed). Both user manuals
(`README.txt`, `README.en.txt`) are copied next to it. The packages are built locally and not uploaded; use
`publish/nuget` as a local NuGet source. Pack and publish reject Debug builds.

## Note on the use of AI (voluntary)

Parts of this project (source code, tests and documentation) were created with the assistance of the AI language
model Claude by Anthropic (Claude Sonnet 5.5; only this one model version was used). The author is responsible
for the content and its publication.

Licensed under MIT.
