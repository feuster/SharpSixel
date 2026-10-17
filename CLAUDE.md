# Sixel – Encoder/Decoder (C#)

Sixel-Grafiken (DECSIXEL) enkodieren/dekodieren. Teil einer CLI; Kern als NuGet portierbar.

## Struktur
- `src/Sixel` – Kern, **0 Abhängigkeiten**, `netstandard2.0;net10.0`: `SixelImage`, `SixelEncoder`, `SixelDecoder`, `BmpCodec`, `SixelTerminal` (DA1-Parser/Detect, I/O-agnostisch)
- `src/Sixel.Skia` – SkiaSharp-Adapter (PNG/JPEG), eigenes NuGet `Sixel.Skia`
- `src/Sixel.Cli` – `encode`/`decode`/`check` (`CliApp` testbar über `CliEnvironment`; `ConsoleTerminal` = Raw-Mode + Timeout für DA1)
- `tests/Sixel.Tests` – xUnit; Docs: `Docs/all-about-sixels.txt`

## Befehle
- Test: `dotnet test`
- Pack (lokal, kein Publish): `dotnet pack src/Sixel -c Release -o artifacts/nupkg` (ebenso `src/Sixel.Skia`)
- CLI: `dotnet run --project src/Sixel.Cli -- encode in.bmp -o out.six`

## Regeln
- Keine neuen NuGets ohne Rückfrage (MIT bevorzugt). SkiaSharp (MIT) ist freigegeben, nur in Skia/CLI.
- Kern netstandard2.0-kompatibel: kein Span/System.Memory, kein `init`/`record`.
- Bibliotheks-/CLI-Code und Kommentare englisch; Chat deutsch, knapp.
- Clean Code, kleine Klassen; Fehler vor neuen Features beheben; Public API mit XML-Doku (TreatWarningsAsErrors).
- Pixelformat: RGBA8888, straight alpha, zeilenweise.

## Spec-Entscheidungen
- P1-Tabelle nach VT330/340-Handbuch (0/1→2:1, 2→5:1, 3/4→3:1, 5/6→2:1, 7-9→1:1). `Docs/all-about-sixels.txt` nennt abweichend 0/1→5:1, 2→3:1 (veraltet).
- HLS: DEC-Hue (Blau 0°, Rot 120°, Grün 240°) → Standard-Hue = (h+240) mod 360.
- Farbwerte in Prozent; 8-Bit↔Prozent gerundet (nur Vielfache von 20 % verlustfrei).
- Unbelegte Pixel: Hintergrund (opak, `Background`) bei P2≠1, transparent bei P2=1.
- Größe: max(Raster Ph/Pv, Daten); deklarierte Höhe kürzt nur das Padding des letzten Bands.
- Ohne `#`-Auswahl: Vordergrund weiß. Register ≥ N: modulo. VT340-Defaultpalette aus Implementierungen, nur Register 1 verifiziert.
- Decoder-Limits (Breite/Höhe 16384, 64 Mi Pixel) gegen DoS durch `!`-Repeat/Raster.
- Encoder: Exakte Palette wenn ≤ MaxColors, sonst Median Cut (5-5-5-Histogramm); optional Floyd-Steinberg (`SixelDithering`, CLI `--dither`), nur bei Farbreduktion, Transparenz bleibt unberührt.

## Status / Offen
- Terminal-Erkennung: DA1 (`ESC [ c`), Sixel = Attribut 4 (erster Parameter = Klasse, zählt nicht). Windows (P/Invoke, VT-Input) real verifiziert; Unix-Pfad (`stty`) nur implementiert, ungetestet.
- Fertig: Decoder, Encoder (+Dithering), BMP, Skia, CLI, Terminal-Check, lokale NuGets, 142 Tests grün.
- Performance (Release, net10.0, 3840x2160 verrauscht): Encode ~95 ms, mit Dithering ~210 ms, Decode ~150 ms.
- Offen: Linux-Natives für CLI (`SkiaSharp.NativeAssets.Linux`), Authors/Repo-URL in NuGet-Metadaten, Unix-Pfad von `check` testen.
- Lange Historie → `CLAUDE-History.md` auslagern, wenn diese Datei wächst.
