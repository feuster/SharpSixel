# CLAUDE History

Ausgelagerte, nicht mehr aktuelle Informationen aus `CLAUDE.md`.

## 2026-10-17 – Initialer Aufbau
- Entscheidungen: .NET 10 (Library zusätzlich netstandard2.0), xUnit, SkiaSharp 4.153.1 (MIT) für PNG/JPEG, BMP nativ im Kern, NuGets nur lokal.
- Pakete: (1) Decoder + Tests, (2) Encoder/Quantizer/BMP + Tests, (3) Skia + CLI + Tests, (4) Pack/Doku.
- Fund: `SKBitmap.Decode(byte[])` wirft in SkiaSharp 4 `ArgumentNullException` bei ungültigen Daten; stattdessen `SKCodec.Create` prüfen.

## 2026-10-17 – Erweiterungen (Details aus CLAUDE.md ausgelagert)
- Terminal-Erkennung: DA1 (ESC [ c); Größe: CSI 14 t (Textbereich px), CSI 16 t (Zelle px) x Konsolenzellen, sonst geschätzt 10x20; Höhe minus eine Zeile. Abfrage CSI 14t CSI 16t CSI c in einem Rutsch. Windows Terminal real verifiziert (120x30 Zellen, 3000x2000 -> 870x580 mit --fit).
- Dithering Ordered (Bayer 8x8): Streuung = mittlerer Nearest-Neighbor-Palettenabstand/sqrt(3) (max 255); Nearest-Lookup über 5-5-5-Bin-Cache (NearestColorCache), gemeinsam mit Floyd-Steinberg.
- Performance (Release, net10.0, 3840x2160 verrauscht): ohne Dithering 77/119 ms (16/256 Farben), Floyd-Steinberg 162/221 ms, Bayer 103/131 ms; Ausgabe Bayer ~15-25 % kleiner als Floyd; Decode ~150 ms.
- Publish: Single-File ~41 MB komprimiert (83 MB unkomprimiert, Warmstart ~74 statt ~43 ms), erster Start ~150 ms (Skia-Native-Entpacken nach %TEMP%\.net). PDBs (libSkiaSharp.pdb 91 MB) werden entfernt. SDK lädt Runtime-Pack Microsoft.NETCore.App.Runtime.win-x64 (SDK-Bestandteil, keine Projektabhängigkeit).
- Konsum der lokalen NuGets getestet (nuget.config mit packageSourceMapping, lokale Quelle publish/nuget).

## 2026-10-17 – Umbenennung SharpSixel
- Projekt/Solution/Assemblies/Namespaces/Pakete: Sixel -> SharpSixel (SharpSixel, SharpSixel.Skia, SharpSixel.Cli, SharpSixel.Tests); Exe SharpSixel.exe; Typnamen Sixel* unverändert. Repo-Ordner C:\ai\Sixel unverändert.
- Autor/Copyright (Props, LICENSE): Alexander Feuster. Publish/Pack nur Release (Guard in Directory.Build.targets). docs/README.txt wird beim Publish nach publish/ kopiert.
- Root-Ordner `C:\ai\Sixel` -> `C:\ai\SharpSixel` umbenannt (Rename-Item, Git-Historie unverändert); bin/obj neu gebaut, da sie absolute Pfade enthalten.

## UI-Feature 1.1.0 (Realtest + Lessons, 2026-10-22)
- UI-Feature (v1.1.0 geplant): ProgressBar, Spinner, LiveRegion, CLI `src/SharpSixel.Cli.Ui` (Assembly/Exe SharpSixelUI, Namespace SharpSixelUI; + `tests/SharpSixelUI.Tests`) geschrieben; 338 Tests grün (SharpSixel.Tests + SharpSixelUI.Tests), Solution eingebunden. Pack/Publish-Paket geschrieben, NOCH NICHT ausgeführt: Version 1.1.0 (Directory.Build.props), `publish.ps1` (+SharpSixelUI.exe, +SharpSixel.Ui-NuGet), pubxml `src/SharpSixel.Cli.Ui`, `docs/README.Ui*.txt`, Haupt-READMEs angepasst. Realtest WT (mit `tools\Screenshot.exe --title`): Bar + Spinner laufen an fester Position, Transparenz funktioniert (schwarzer Block war Folge des Scrollens), Spinner verschwindet am Ende. Ursache des Ur-Bugs + Fix: Bildhöhe = Vielfaches von 6 ≤ Zeilen*Zellhöhe (`GetImageHeight`), Sixel-`LiveRegion` reserviert 1 Guard-Zeile; Spinner blendet sich per `LiveRegion.End(clear: true)` aus. CLI: `--opt=value` möglich (PowerShell: unquoted `#RRGGBB` ist Kommentar). Testen: `wt` trennt Argumente an `;` (kein `cd x; cmd`, stattdessen `-d <dir>`), Fenster per `--title` + `--suppressApplicationTitle`. 348 Tests grün. `./publish.ps1` läuft sauber (SharpSixel.exe, SharpSixelUI.exe, 3 NuGets 1.1.0, README*.txt). ToDo: Commit (Nutzer), optional README-Hinweis auf Realtest-Terminals

## Table-Widget (2026-10-26)
- `Table` (+ `TableColumn`, `TableAlignment`, `TableStyle`) in SharpSixel.Ui, CLI `table` (auch in `demo`), Tests `TableTests` + CLI-Tests; gleiche Zeilenstruktur in Sixel und ASCII, Sixel zeichnet nur das Raster als Pixellinien in eigenen Zellen (Text löscht Grafik in seinen Zellen). READMEs (src/SharpSixel.Ui, docs/README.Ui.txt, README.Ui.en.txt) und CLAUDE.md aktualisiert. Ui/README.md und docs/README.Ui.txt waren beschädigt (Zeichen ersetzt) und wurden aus publish/NuGet-Paket wiederhergestellt.
