# Sixel – Encoder/Decoder (C#)

Sixel-Grafiken (DECSIXEL) enkodieren/dekodieren. Teil einer CLI; Kern als NuGet portierbar.

## Struktur
- `src/Sixel` – Kern, **0 Abhängigkeiten**, `netstandard2.0;net10.0`: `SixelImage`, `SixelEncoder`, `SixelDecoder`, `BmpCodec`, `ImageScaler`, `SixelTerminal` (DA1 + Pixelgröße, I/O-agnostisch)
- `src/Sixel.Skia` – SkiaSharp-Adapter (PNG/JPEG), eigenes NuGet `Sixel.Skia`
- `src/Sixel.Cli` – `encode`/`decode`/`check` (`CliApp` testbar über `CliEnvironment`; `ConsoleTerminal` = Raw-Mode + Timeout für DA1)
- `tests/Sixel.Tests` – xUnit; Docs: `Docs/all-about-sixels.txt`

## Befehle
- Test: `dotnet test`
- Pack einzeln (lokal, kein Upload): `dotnet pack src/Sixel -c Release -o publish/nuget` (ebenso `src/Sixel.Skia`)
- Alles in einem Lauf: `./publish.ps1 [-Version x.y.z]` → `publish/sixel.exe` (win-x64, self-contained Single-File, komprimiert) + `publish/nuget/*.nupkg` (Sixel, Sixel.Skia); `publish/` ist gitignored. Nur die Exe: `dotnet publish src/Sixel.Cli -p:PublishProfile=win-x64`. NuGet nur lokal, nie hochladen ohne Auftrag. Konsum getestet über lokalen Feed (nuget.config mit `packageSourceMapping`).\n- CLI: `dotnet run --project src/Sixel.Cli -- encode in.bmp -o out.six`

## Regeln
- Keine neuen NuGets ohne Rückfrage (MIT bevorzugt). SkiaSharp (MIT) ist freigegeben, nur in Skia/CLI.
- Kern netstandard2.0-kompatibel: kein Span/System.Memory, kein `init`/`record`.
- Bibliotheks-/CLI-Code und Kommentare englisch; Chat deutsch, knapp.
- Clean Code, kleine Klassen; Fehler vor neuen Features beheben; Public API mit XML-Doku (TreatWarningsAsErrors).
- Publish: Profil `src/Sixel.Cli/Properties/PublishProfiles/win-x64.pubxml` (nur beim Publish aktiv, kein RID im Projekt, sonst Tests/Referenzen betroffen); SkiaSharp-Native im Bundle (`IncludeNativeLibrariesForSelfExtract`, Entpacken beim 1. Start nach %TEMP%\.net), PDBs entfernt, Exe heißt `sixel.exe`. Kein Trimming (Skia/P-Invoke-Risiko). ~41 MB komprimiert (83 MB unkomprimiert, +~30 ms Start).\n- Pixelformat: RGBA8888, straight alpha, zeilenweise.

## Spec-Entscheidungen
- P1-Tabelle nach VT330/340-Handbuch (0/1→2:1, 2→5:1, 3/4→3:1, 5/6→2:1, 7-9→1:1). `Docs/all-about-sixels.txt` nennt abweichend 0/1→5:1, 2→3:1 (veraltet).
- HLS: DEC-Hue (Blau 0°, Rot 120°, Grün 240°) → Standard-Hue = (h+240) mod 360.
- Farbwerte in Prozent; 8-Bit↔Prozent gerundet (nur Vielfache von 20 % verlustfrei).
- Unbelegte Pixel: Hintergrund (opak, `Background`) bei P2≠1, transparent bei P2=1.
- Größe: max(Raster Ph/Pv, Daten); deklarierte Höhe kürzt nur das Padding des letzten Bands.
- Ohne `#`-Auswahl: Vordergrund weiß. Register ≥ N: modulo. VT340-Defaultpalette aus Implementierungen, nur Register 1 verifiziert.
- Decoder-Limits (Breite/Höhe 16384, 64 Mi Pixel) gegen DoS durch `!`-Repeat/Raster.
- Encoder: Exakte Palette wenn ≤ MaxColors, sonst Median Cut (5-5-5-Histogramm); Dithering (`SixelDithering`: None/FloydSteinberg/Ordered, CLI `--dither [floyd|bayer|none]`, bare `--dither` = floyd), nur bei Farbreduktion, Transparenz bleibt unberührt. Ordered = 8x8-Bayer, Streuung = mittlerer Nearest-Neighbor-Palettenabstand/√3 (max 255); Nearest-Lookup über 5-5-5-Bin-Cache.

## Status / Offen
- Größenanpassung: `ImageScaler` (Flächenmittel beim Verkleinern, bilinear beim Vergrößern, alpha-gewichtet). CLI `--width/--height` (fit, nur verkleinern), `--upscale`, `--fit` = Terminalbereich; explizite Achse hat Vorrang. Terminalbereich: `CSI 14 t` (Textbereich px) bzw. `CSI 16 t` (Zelle px) × Konsolenzellen, sonst geschätzt 10x20 (Warnung auf stderr); Höhe minus eine Zeile. Abfrage `CSI 14t CSI 16t CSI c` in einem Rutsch, DA1 als Sentinel (kein Timeout bei Terminals ohne Support).
- Terminal-Erkennung: DA1 (`ESC [ c`), Sixel = Attribut 4 (erster Parameter = Klasse, zählt nicht). Windows (P/Invoke, VT-Input) real verifiziert; Unix-Pfad (`stty`) nur implementiert, ungetestet.
- Fertig: Decoder, Encoder (+Dithering), BMP, Skia, CLI, Terminal-Check, Skalierung/`--fit`, lokale NuGets, 205 Tests grün. `--fit` unter Windows Terminal real verifiziert (120x30 Zellen → 3000x2000 wird 870x580).
- Performance (Release, net10.0, 3840x2160 verrauscht): Encode ~120 ms, Floyd-Steinberg ~220 ms, Bayer ~130 ms (Bayer-Ausgabe ~25 % kleiner als Floyd), Decode ~150 ms.
- Offen: Linux-Natives für CLI (`SkiaSharp.NativeAssets.Linux`), Unix-Pfad von `check`/`--fit` testen. NuGet-Metadaten (Autor/Repo-URL) bewusst offen (Nutzerentscheidung), Pakete nur lokal packen.
- Lange Historie → `CLAUDE-History.md` auslagern, wenn diese Datei wächst.
