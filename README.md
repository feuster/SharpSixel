# Sixel

C# encoder/decoder for sixel graphics (DECSIXEL), a CLI, and NuGet packages.

| Package | Purpose |
|---|---|
| `Sixel` | Dependency-free core: `SixelEncoder`, `SixelDecoder`, `SixelImage`, `BmpCodec` |
| `Sixel.Skia` | SkiaSharp adapter for PNG/JPEG |
| `Sixel.Cli` | Command line tool |

## CLI

```
sixel encode <input> [-o <file>] [--colors <2-256>]
sixel decode <input> -o <file>      # .bmp, .png, .jpg
```

`<input>` may be `-` for stdin. `encode` without `-o` writes sixel to stdout.

## Build, test, pack

```
dotnet test
dotnet pack src/Sixel -c Release -o artifacts/nupkg
dotnet pack src/Sixel.Skia -c Release -o artifacts/nupkg
```

Licensed under MIT.
