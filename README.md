# SharpSixel

**Sixel graphics for .NET: a dependency-free encoder/decoder library, PNG/JPEG support via SkiaSharp, and a command line tool that shows images right in your terminal.**

![License: MIT](https://img.shields.io/badge/license-MIT-blue)
![.NET](https://img.shields.io/badge/.NET-10.0%20%7C%20netstandard2.0-512BD4)
![Platform](https://img.shields.io/badge/CLI-win--x64-lightgrey)

[Sixel](https://en.wikipedia.org/wiki/Sixel) is a bitmap graphics format from the DEC VT240/VT340 era. An image is
described by printable control characters, so a terminal can draw it directly, with no external viewer. Many modern
terminals support it (see [Terminal support](#terminal-support)).

```console
$ SharpSixel check
Terminal supports sixel graphics.
[a small test pattern appears: color bars, gray ramp, checkerboard, red-to-blue gradient]

$ SharpSixel encode photo.jpg --show --fit
[the photo appears in the terminal, scaled to the window]
```

## Contents

- [Features](#features)
- [Quick start](#quick-start)
- [Command line](#command-line)
- [Library](#library)
- [Terminal support](#terminal-support)
- [Build, test, publish](#build-test-publish)
- [Project layout](#project-layout)
- [Documentation](#documentation)
- [Limits and status](#limits-and-status)
- [Specifications](#specifications)
- [Note on the use of AI](#note-on-the-use-of-ai-voluntary)
- [License](#license)

## Features

**Encoder**
- Exact palette if the image has at most `MaxColors` colors (up to 256), otherwise median-cut quantization
- Dithering: Floyd-Steinberg (error diffusion) or ordered 8x8 Bayer matrix (faster, smaller output)
- Run-length encoding, raster attributes (size and aspect ratio), transparency (`P2 = 1`)

**Decoder**
- Full DECSIXEL: color registers (RGB and HLS with the DEC hue convention), repeat, raster attributes, pixel aspect
  ratio, background select, 7-bit and 8-bit control sequences
- Tolerant of unknown characters and line breaks; size limits protect against malformed or malicious data

**Library extras**
- `BmpCodec`: BMP read (1/4/8/16/24/32 bit) and write, built in, no dependencies
- `ImageScaler`: area averaging when shrinking, bilinear when enlarging, alpha-aware
- `SixelTerminal`: sixel support detection (DA1) and pixel size query, independent of how you do console I/O
- `SharpSixel.Skia`: PNG, JPEG and other formats through [SkiaSharp](https://github.com/mono/SkiaSharp) (MIT)

**Command line**
- `encode`, `decode`, `check`; show images in the terminal (`--show`), fit them to the window (`--fit`)
- Single-file, self-contained Windows executable, no .NET installation needed

## Quick start

Build the standalone executable, the NuGet packages and copy the manuals into `publish/` (requires the .NET 10 SDK):

```powershell
./publish.ps1
```

Then:

```powershell
cd publish
.\SharpSixel.exe check                              # does the terminal support sixel? shows a test image
.\SharpSixel.exe encode photo.jpg --show --fit      # display a photo, fitted to the terminal
.\SharpSixel.exe encode photo.png --width 800 --colors 64 --dither bayer -o photo.six
.\SharpSixel.exe encode photo.six --show            # display a saved sixel file
.\SharpSixel.exe decode photo.six -o photo.png      # sixel back to PNG
```

## Command line

```
SharpSixel encode <input> [-o <file>] [--show] [--colors <2-256>] [--dither [floyd|bayer|none]]
                  [--width <px>] [--height <px>] [--fit] [--upscale]
SharpSixel decode <input> -o <file>      # .bmp, .png, .jpg
SharpSixel check                         # exit 0 if the terminal reports sixel support; shows a test image
```

| Option | Meaning |
|---|---|
| `<input>` | File path or `-` for stdin. `encode` reads BMP, PNG, JPEG (and other formats SkiaSharp can read) or a sixel file |
| `-o, --output <file>` | Output file. Without `-o`, `encode` writes the sixel to stdout |
| `--show` | `encode` only: display in the terminal. Without `-o` it is only displayed, with `-o` it is saved and displayed. stdout must be a terminal |
| `--colors <n>` | Maximum palette colors, 2-256 (default 256) |
| `--dither [mode]` | `floyd` (also the default for a bare `--dither`), `bayer`, or `none` |
| `--width <px>`, `--height <px>` | Shrink to fit, aspect ratio kept |
| `--fit` | Fit into the terminal's drawable area (queried from the terminal) |
| `--upscale` | Also enlarge small images up to the given limits |
| `-h, --help`, `--version` | Help and version |

A sixel file (header `ESC P ... q`) is accepted as input for `encode`: it is passed through byte for byte unless size,
color or dithering options are given, in which case it is decoded, adjusted and encoded again.

Exit codes: `0` success, `1` runtime error (or `check`: not supported), `2` usage error.

The complete user manual, including troubleshooting, is in [`Docs/README.en.txt`](Docs/README.en.txt)
(German: [`Docs/README.txt`](Docs/README.txt)).

## Library

Targets `netstandard2.0` and `net10.0`. `SharpSixel` has no dependencies; `SharpSixel.Skia` needs SkiaSharp.

```csharp
using SharpSixel;
using SharpSixel.Skia;

// Load any image, shrink it, encode it as sixel
SixelImage image = SkiaImageCodec.Decode(File.ReadAllBytes("photo.png"));
image = ImageScaler.Fit(image, maxWidth: 800, maxHeight: 600);

var options = new SixelEncoderOptions { MaxColors = 64, Dithering = SixelDithering.Ordered };
string sixel = new SixelEncoder(options).Encode(image);
Console.Write(sixel);                                   // shows the image in a sixel terminal

// And back again
SixelImage decoded = new SixelDecoder().Decode(sixel);
File.WriteAllBytes("out.png", SkiaImageCodec.Encode(decoded));
```

Without Skia, `BmpCodec.Decode(bytes)` / `BmpCodec.Encode(image)` cover BMP files.

| Type | Purpose |
|---|---|
| `SixelImage`, `SixelColor` | RGBA8888 pixel buffer (straight alpha) and color value |
| `SixelEncoder`, `SixelEncoderOptions` | Image to sixel: `MaxColors`, `AlphaThreshold`, `EmitRasterAttributes`, `Dithering` |
| `SixelDecoder`, `SixelDecoderOptions` | Sixel to image: size limits, register count, background color; `LooksLikeSixel` |
| `BmpCodec`, `ImageScaler` | BMP read/write, `Resize` / `Fit` / `FitSize` |
| `SixelTerminal`, `SixelTerminalInfo` | DA1 and pixel size query |
| `SkiaImageCodec` (`SharpSixel.Skia`) | PNG/JPEG/... conversion and `SKBitmap` interop |
| `SixelException` | Invalid data or exceeded limits |

The packages are built locally by `./publish.ps1` into `publish/nuget/` and are not published on nuget.org yet. To use
them, add that folder as a local NuGet source.

## Terminal support

Terminals known to support sixel include Windows Terminal (1.22 or later), xterm (with sixel enabled), WezTerm,
foot, mlterm, Contour and iTerm2. Support depends on version and configuration, so run `SharpSixel check` to be sure.
The classic Windows console host (conhost) does not display sixel.

## Build, test, publish

```powershell
dotnet test                      # 247 tests
./publish.ps1                    # SharpSixel.exe + manuals + NuGet packages in publish/
./publish.ps1 -Version 0.2.0     # optional version for exe and packages
```

Pack and publish are Release-only; a `-c Debug` call is rejected. `publish/` contains:

```
SharpSixel.exe              single-file, self-contained, win-x64 (about 41 MB, compressed)
README.txt, README.en.txt   user manuals (German, English)
nuget/                      SharpSixel and SharpSixel.Skia packages
```

## Project layout

```
src/SharpSixel/         core library (no dependencies)
src/SharpSixel.Skia/    SkiaSharp adapter (PNG/JPEG)
src/SharpSixel.Cli/     command line tool
tests/SharpSixel.Tests/ xUnit tests
Docs/                   user manuals and sixel notes
publish.ps1             publish + pack in one run
```

## Documentation

- [`Docs/README.en.txt`](Docs/README.en.txt): user manual (English)
- [`Docs/README.txt`](Docs/README.txt): Benutzeranleitung (Deutsch)
- [`Docs/all-about-sixels.txt`](Docs/all-about-sixels.txt): sixel format notes used as a starting point

## Limits and status

- The decoder accepts images up to 16384 x 16384 pixels and 64 million pixels in total (configurable in the library).
- The encoder writes at most 256 colors; there are no animations and no interlacing.
- Measured on one machine (Release, .NET 10, noisy 3840 x 2160 test image): encoding about 0.1 s without and about
  0.2 s with Floyd-Steinberg dithering, decoding about 0.15 s.
- Verified on Windows (Windows Terminal). The Unix code path for `check`, `--fit` and `--show` (raw mode via `stty`) is
  implemented but not yet tested on Linux or macOS. On Linux the CLI additionally needs `SkiaSharp.NativeAssets.Linux`.
- Only the Windows executable is published; other platforms build from source.

## Specifications

- [Sixel (Wikipedia)](https://en.wikipedia.org/wiki/Sixel)
- [XTerm Control Sequences: Sixel Graphics](https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Sixel-Graphics)
- [VT330/VT340 Programmer Reference Manual, chapter 14](https://www.vt100.net/docs/vt3xx-gp/chapter14.html)

Where sources disagree (for example the pixel aspect ratio table for the `P1` parameter), the VT330/VT340 manual is
followed.

## Note on the use of AI (voluntary)

Parts of this project (source code, tests and documentation) were created with the assistance of the AI language model
Claude by Anthropic (Claude Sonnet 5.5; only this one model version was used). The author is responsible for the content
and its publication.

## License

[MIT](LICENSE), Copyright (c) 2026 Alexander Feuster

SkiaSharp, used for PNG/JPEG support, is licensed under the MIT license.
