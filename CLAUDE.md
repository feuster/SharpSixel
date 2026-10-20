# SharpSixel – Sixel Encoder/Decoder (C#)

Sixel-Grafiken (DECSIXEL) enkodieren/dekodieren. Autor: Alexander Feuster, MIT. Teil einer CLI; Kern als NuGet portierbar.
Repo-Ordner: `C:\ai\SharpSixel` (vorher `C:\ai\Sixel`). GitHub: https://github.com/feuster/SharpSixel (Branch `master`). CI: `.github/workflows/dotnet.yml` (windows-latest, .NET 10, restore/build/test bei Push und PR auf master; Status-Badge in den READMEs). Repo-URL steht auch in den NuGet-Metadaten (`Directory.Build.props`).
GitHub-READMEs: `README.md` (englisch) und `README.de.md` (deutsch), inhaltlich deckungsgleich pflegen; Screenshot `docs/images/check.png`. Ordnername `docs` klein (GitHub ist case-sensitiv, Git-Index muss zur Platte passen).

## Struktur
- `src/SharpSixel` – Kern, **0 Abhängigkeiten**, `netstandard2.0;net10.0`: `SixelImage`, `SixelEncoder`, `SixelDecoder`, `BmpCodec`, `ImageScaler`, `SixelTerminal` (DA1 + Pixelgröße, I/O-agnostisch). Namespace `SharpSixel`; Typnamen `Sixel*` bleiben (Format).
- `src/SharpSixel.Skia` – SkiaSharp-Adapter (PNG/JPEG), eigenes NuGet `SharpSixel.Skia`
- `src/SharpSixel.Cli` – `encode`/`decode`/`check` (`CliApp` testbar über `CliEnvironment`; `ConsoleTerminal` = Raw-Mode + Timeout)
- `tests/SharpSixel.Tests` – xUnit. `docs/`: `README.txt` (Benutzer-Anleitung deutsch, mit echten Umlauten/ß), `README.en.txt` (englische Version, gleicher Inhalt) – beide werden beim Publish nach `publish/` kopiert; `all-about-sixels.txt` (Spec)

## Befehle
- Test: `dotnet test`
- Alles in einem Lauf: `./publish.ps1 [-Version x.y.z]` → `publish/SharpSixel.exe` (win-x64, self-contained, Single-File, komprimiert), `publish/README.txt` + `publish/README.en.txt`, `publish/nuget/*.nupkg` (SharpSixel, SharpSixel.Skia). `publish/` ist gitignored.
- Einzeln: `dotnet publish src/SharpSixel.Cli -c Release -p:PublishProfile=win-x64`; `dotnet pack src/SharpSixel -o publish/nuget`
- **Nur Release**: `Directory.Build.targets` bricht Pack/Publish bei Debug ab; `PackRelease`/`PublishRelease` setzen Release als Default.
- NuGet nur lokal, nie hochladen ohne Auftrag.

## Regeln
- Keine neuen NuGets ohne Rückfrage (MIT bevorzugt). SkiaSharp (MIT) ist freigegeben, nur in Skia/CLI.
- Kern netstandard2.0-kompatibel: kein Span/System.Memory, kein `init`/`record`.
- Code/Kommentare englisch; Chat deutsch, knapp. Clean Code, kleine Klassen; Fehler vor neuen Features beheben; Public API mit XML-Doku (TreatWarningsAsErrors).
- Pixelformat: RGBA8888, straight alpha, zeilenweise.
- Publish-Profil (`src/SharpSixel.Cli/Properties/PublishProfiles/win-x64.pubxml`) gilt nur beim Publish (kein RID im Projekt). Skia-Native im Bundle, PDBs entfernt, Exe-Umbenennung + README-Kopie im Target `FinalizeSingleFileExe`. Kein Trimming (Skia/P-Invoke).
- Nutzerdoku `docs/README.txt` und `docs/README.en.txt` bei Feature-/CLI-Änderungen gemeinsam pflegen (inhaltlich deckungsgleich, deutsche Version mit ä/ö/ü/ß statt ae/oe/ue/ss-Umschreibung). Beide enthalten einen freiwilligen KI-Hinweis mit Modellversion (Abschnitt 16: Claude Sonnet 5.5, laut Autor die einzige eingesetzte Modellversion) – bei Modellwechsel aktualisieren.

## Spec-Entscheidungen
- P1-Tabelle nach VT330/340-Handbuch (0/1→2:1, 2→5:1, 3/4→3:1, 5/6→2:1, 7-9→1:1); `all-about-sixels.txt` weicht ab (veraltet).
- HLS: DEC-Hue (Blau 0°, Rot 120°, Grün 240°) → Standard-Hue = (h+240) mod 360. Farbwerte in Prozent (nur Vielfache von 20 % verlustfrei).
- Unbelegte Pixel: Hintergrund (opak) bei P2≠1, transparent bei P2=1. Größe: max(Raster Ph/Pv, Daten); deklarierte Höhe kürzt nur Band-Padding.
- Ohne `#`-Auswahl: Vordergrund weiß. Register ≥ N: modulo. VT340-Defaultpalette aus Implementierungen, nur Register 1 verifiziert.
- Decoder-Limits (16384 px je Achse, 64 Mi Pixel) gegen DoS.
- Encoder: exakte Palette wenn ≤ MaxColors, sonst Median Cut (5-5-5-Histogramm). Dithering None/FloydSteinberg/Ordered (CLI `--dither [floyd|bayer|none]`, bare = floyd), nur bei Farbreduktion.
- Skalierung: `ImageScaler`; CLI `--width/--height/--fit/--upscale`. Terminalgröße per `CSI 14t/16t` + DA1 als Sentinel, Fallback 10x20-Zellen mit Warnung.
- Terminal-Check: DA1, Sixel = Attribut 4 (erster Parameter = Klasse). Bei Erfolg zeigt `check` ein kleines Testbild (CLI-Klasse `TestImage`, 128x64: Farbbalken, 16-stufiges Grau, Schachbrett, Rot-Blau-Verlauf; wenige Farben = exakte Palette), bei Misserfolg nichts.
- `--show` (nur encode): Terminal-Ausgabe erzwungen (sonst Fehler, Exit 1), DA1-Warnung bei fehlender Sixel-Meldung (entfällt bei Eingabe `-`), Zeilenumbruch nach dem Bild. Ohne `-o` nur anzeigen, mit `-o` speichern + anzeigen. Sixel-Eingabe nur per DCS-Header erkannt (`SixelDecoder.LooksLikeSixel`, bewusst keine Rohtext-Heuristik): ohne Größen-/Farb-/Dither-Option unverändert durchgereicht (vorher per Decode validiert), sonst dekodiert + neu enkodiert.

## Status / Offen
- Fertig: Decoder, Encoder (+Dithering), BMP, Skia, CLI (encode/decode/check, Skalierung), lokale NuGets, Single-File-Publish, Benutzer-README. 247 Tests grün.
- Windows real verifiziert (check inkl. Testbild, --fit, --show inkl. Sixel-Datei-Eingabe/Speichern, Publish-Exe). Unix-Pfad (`stty`) ungetestet.
- Offen: Linux-Natives (`SkiaSharp.NativeAssets.Linux`) + Unix-Test (evtl. zusätzlicher Linux-CI-Job); Autor und Repo-URL sind gesetzt.
- Details/Messwerte/Historie → `CLAUDE-History.md`.
