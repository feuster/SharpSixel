# SharpSixel – Sixel Encoder/Decoder (C#)

Sixel-Grafiken (DECSIXEL) enkodieren/dekodieren. Autor: Alexander Feuster, MIT. Teil einer CLI; Kern als NuGet portierbar.
Repo-Ordner: `C:\ai\SharpSixel` (vorher `C:\ai\Sixel`). GitHub: https://github.com/feuster/SharpSixel (Branch `master`). CI: `.github/workflows/dotnet.yml` (windows-latest, .NET 10, restore/build/test bei Push und PR auf master; Status-Badge in den READMEs). Repo-URL steht auch in den NuGet-Metadaten (`Directory.Build.props`).
GitHub-READMEs: `README.md` (englisch) und `README.de.md` (deutsch), inhaltlich deckungsgleich pflegen; Screenshot `docs/images/check.png`. Ordnername `docs` klein (GitHub ist case-sensitiv, Git-Index muss zur Platte passen).

## Struktur
- `src/SharpSixel` – Kern, **0 Abhängigkeiten**, `netstandard2.0;net10.0`: `SixelImage`, `SixelEncoder`, `SixelDecoder`, `BmpCodec`, `ImageScaler`, `SixelTerminal` (DA1 + Pixelgröße, I/O-agnostisch). Namespace `SharpSixel`; Typnamen `Sixel*` bleiben (Format).
- `src/SharpSixel.Skia` – SkiaSharp-Adapter (PNG/JPEG), eigenes NuGet `SharpSixel.Skia`
- `src/SharpSixel.Osc` – Terminalsteuerung per OSC/DECSCUSR, statische Klasse `TerminalControl` (Namespace `SharpSixel.Osc`; Klasse nicht `Osc` wegen Namensgleichheit mit dem Namespace), `CursorStyle`; Titel (OSC 0), Vorder-/Hintergrund-/Cursorfarbe (OSC 10/11/12, Reset 110/111/112), Cursorstil (`CSI Ps SP q`), `Reset()`; jede Methode mit optionalem `TextWriter` (Default `Console.Out`); Farben als `SixelColor` oder `#RGB`/`#RRGGBB` (sonst `ArgumentException`, Alpha ignoriert); Titel ohne Steuerzeichen, Backup des Konsolentitels nur unter Windows (einmalig bis `ResetTitle`); nur SharpSixel als Abhängigkeit, eigenes NuGet `SharpSixel.Osc`; CLI `osc` in SharpSixelUI (nicht in `demo`); Tests `TerminalControlTests`, `UiCliAppTests`. Aus der ursprünglichen `OSC.cs` übernommen, aber korrigiert: Farbausgabe war `Color.ToString()` statt Hex, Regex erlaubte Alpha-Formen, `System.Drawing`-Abhängigkeit, Titel-Backup wurde bei erneutem Setzen überschrieben, Tippfehler `SetCursorStylUnderscoreStatic`; die sechs Stil-Wrapper entfallen (`SetCursorStyle(CursorStyle)` ist öffentlich)
- `src/SharpSixel.Ui` – Terminal-Widgets (`ProgressBar`, `Spinner`, `BarChart`, `Table`, `Button`, `Panel`, `Clock`, `DateDisplay`, `Tree`) und `StatusDisplay` (Spinner + änderbarer Statustext, kein Callback/Context: `Start`/`Text`/`WriteLine`/`Refresh`/`Stop`/`Dispose`, Timer-Animation zeitbasiert, Fallback ohne ANSI = eine Zeile je Textwechsel; CLI `status` und `demo`), Sixel oder ASCII-Fallback (`WidgetEnvironment`, `RenderMode`), `LiveRegion` (In-Place-Redraw per DECSC/DECRC + relative CSI-Moves, Zeilen vorab reserviert; mehrzeilige ASCII-Frames: `Widget.AsciiRows`, Zeilen per `\n` getrennt, LiveRegion setzt jede Zeile per `Down(i)`), `BarChart` (horizontal: Label, Balken, Wert; `BarChartItem`/`BarChartStyle`; ein Sixel-Bild für alle Balken; CLI `chart`, in `demo` enthalten), `Table` (Spaltenköpfe + Textzellen im Raster; `TableColumn` (`Header`, `Alignment` = `TableAlignment` Left/Right/Center, `Width` null = an breitesten Text angepasst, sonst fest + Kürzung mit „…“), `Rows` = `string[]` je Zeile, `AddColumn`/`AddRow`; `TableStyle`: `Padding`, `LineThickness`, `ShowBorder`/`ShowHeader`/`ShowColumnSeparators`/`ShowRowSeparators`, `BorderColor`, `HeaderTextColor`, `HeaderBackgroundColor`, `StripeColor`, ASCII-Zeichen; Sixel und ASCII haben dieselben Zeilen (Überschrift, obere Linie, Kopf, Kopflinie, Datenzeilen, untere Linie) – Sixel zeichnet nur das Raster als dünne Pixellinien in eigenen Zellen (Trenner-/Linienzellen bleiben ohne Text, da Terminals Grafik unter Text teils löschen), Kopf/Zellen als Terminaltext, Kopf-/Streifenhintergrund per ANSI; CLI `table`, in `demo` enthalten), `Button` (Beschriftung + `ButtonState` Normal/Focused/Pressed/Disabled, nur Darstellung, Aufrufer setzt `State` zwischen Frames; `ButtonStyle`: Farben je Zustand, `Padding`, `MinWidth`, `HeightRows`, `CornerRadius`; Breite = Label + 2*(Padding+1) Zellen; Sixel: abgerundete Fläche als Bild, Label als Terminaltext zentriert mit ANSI-Vorder-/Hintergrund (Flächenfarbe), da Terminals Grafik unter Text teils löschen; ASCII: `[ OK ]`, Marker `[>OK<]`/`[#OK#]`/`[-OK-]`; CLI `button` zeigt alle Zustände nacheinander, in `demo` enthalten; im echten Terminal verifiziert; `Button.Contains` für Klick-Hit-Test), `Panel` (Rahmen um Textzeilen, optionaler `Title` in der oberen Kante; `Lines`/`AddLine`, `Width` null = an breiteste Zeile/Titel angepasst, sonst fest + Kürzung mit „…“ (Breite = Textbereich ohne Padding/Rahmen); `PanelStyle`: `Padding`, `LineThickness`, `CornerRadius`, `BorderColor`, `TitleColor`, `BackgroundColor` (Innenfläche, Transparent = Terminalhintergrund), ASCII-Zeichen; Sixel und ASCII haben dieselben Zeilen (obere Kante, eine je Textzeile, untere Kante) – Sixel zeichnet nur den abgerundeten Rahmen als ein Bild (Titelzellen ohne Linienpixel, da Text Grafik darunter ersetzt), Titel/Zeilen als Terminaltext (ANSI-Hintergrund bei opakem `BackgroundColor`); ASCII `+- Titel --+ / | Text | / +---+`; CLI `panel` (`--caption` = Titel, `--width` = Textbreite, `--border`/`--active`/`--background`/`--radius`), in `demo` enthalten (nach `button`); Tests `PanelTests`, `UiCliAppTests`; Benutzerdoku des Widget-Demos: `docs/README.Ui.txt`/`README.Ui.en.txt`), `Clock` (Uhrzeit; `Time` setzt der Aufrufer vor jedem Frame, kein Eigentakt; Sixel: quadratische Analoguhr (Rand, 12 Marken, Stunden-/Minuten-/optional Sekundenzeiger) als ein Bild + digitale Zeit als Terminaltext, ASCII nur Text; `ClockStyle`: `HeightRows`, `LineThickness`, `ShowSecondHand`, `TimeFormat` (invariant, Default `HH:mm:ss`, Setter prüft Format), Farben; CLI `clock` (`--duration`, laufend per `Now` aus `UiCliEnvironment`), in `demo` enthalten) und `DateDisplay` (Datum; `Date`, Kalenderblatt mit Tagesnummer in Sieben-Segment-Ziffern + Datumstext, `DateDisplayStyle`: `CornerRadius`, `DateFormat` Default `ddd, dd MMM yyyy`, Farben; CLI `date`, einmal gezeichnet, in `demo` enthalten; Tests `ClockTests`, `DateDisplayTests`, `UiCliAppTests`), `Tree` (Baum aus `TreeNode` (`Text`, `Expanded`, `Nodes`), optionaler `Title` = Wurzelzeile; Sixel und ASCII haben dieselben Zeilen (Titel, eine je sichtbarem Knoten, Kinder eingeklappter Knoten ausgeblendet, Knoten erhält `TreeStyle.CollapsedSuffix` " [+]"); Sixel: Führungslinien als ein Bild in 4-Zellen-Spalten je Ebene, Knotentext dahinter als Terminaltext (Terminals löschen Grafik unter Text teils), ASCII `+-- `/`` `-- ``/`|`; `TreeStyle`: `LineThickness`, `CollapsedSuffix`, `GuideColor`, `TitleColor`, ASCII-Zeichen; CLI `tree` (`--caption` = Titel, `--border` = Linien, `--active` = Titel), einmal gezeichnet, in `demo` enthalten; Tests `TreeTests`, `UiCliAppTests`), `ImageView` (Bild in View fester Größe `Columns` x `Rows`; `ImageViewStyle`: optionaler Rahmen, `BackgroundColor` unabhängig vom Terminal; Fit (Seitenverhältnis, zentriert, `AllowUpscale`) automatisch beim Zeichnen via `ImageScaler.Fit`, Kopie gecacht, `Image` neu zuweisen nach Pixeländerung; Bild per `Image = SixelImage` oder `Load(Pfad|Stream|Bytes)`: BMP/Sixel eingebaut, sonst `Decoder` (z. B. `SkiaImageCodec.Decode`, Ui hängt nicht von Skia ab); Sixel: ein Bild Canvas + `EraseChars` je Zeile bei transparentem Hintergrund; ASCII: 1 Pixel = 1 Zelle (`AsciiRamp` nach Helligkeit, ANSI-Pixelfarbe), Rahmen ab 3x3; CLI `image` (`--file` BMP/Sixel, ohne Datei Demo-Bild; `--border` = Rahmenfarbe), nicht in `demo`), `Prompt<T>` (Basis; `Dialog` (Meldung + Button-Zeile, Ergebnis = Button-Index, Links/Rechts/Tab; Nachrichtenzeilen/Buttons als `Indent`-Zeilen, gewählter Button per `HighlightStart/Length` invertiert), `SelectionPrompt<T>`, `MultiSelectionPrompt<T>`, `TextPrompt`, `ValidationResult`, `PromptStyle`: interaktiv per `Show(writer, env, readKey)`, Tasten via `Func<ConsoleKeyInfo>` (Default `Console.ReadKey(true)`), Rendering über internen `PromptView` (Zeilen mit Marker: Sixel-Grafik Kreis/Quadrat/Punkt + Terminaltext, ASCII-Präfix) in `LiveRegion`, Fehlerzeile immer reserviert (konstante Zeilenzahl), Validierung bei Enter: eingebaute Regeln, dann `Validator`; ohne ANSI/Sixel Plain-Dialog per `LineReader`; CLI `prompts` interaktiv, nicht in `demo`, ohne Raw-Mode, nur VT-Ausgabe via `ConsoleTerminal.EnterVtOutput`, Ctrl+C = `OperationCanceledException`; Maus: `ShowWithMouse(writer, env, Func<InputEvent>)` (SGR-Mausmeldungen 1000/1006, Ursprung per DSR 6n in `LiveRegion.EnableMouse`, `TryGetLocal`, `VtInputParser`, `MouseEvent`/`InputEvent`; Klick wählt Dialog-Button/Auswahleintrag, schaltet Mehrfachauswahl um, Rad = Auf/Ab; CLI: `ConsoleInput` + `ConsoleTerminal.EnterMouseInput` (Raw, Strg+C als Zeichen 0x03), `--no-mouse`, `button --click`; im echten Terminal verifiziert); Sixel-Look der Prompts: `PromptView` zeichnet zuerst EIN Bild (abgerundeter Rahmen mit 2 Zeilen/Spalten Zusatz bei `PromptStyle.ShowFrame` (`PromptView.ExtraRows`; `Prompt.Handle` korrigiert Mauspositionen um 1), Balken `Bar` hinter der aktuellen Zeile (`HighlightColor`, Transparent = aus), Marker (Skala 0,8; Check mit ausgeschnittenem Häkchen), Button-Flächen `PromptChip` im Dialog), danach den Text mit ANSI-Hintergrund in Balken-/Panel-/Flächenfarbe (Text in einer Zelle ersetzt die Grafik darunter; Bild-vor-Text wie bei `Button`); `BackgroundColor` opak = Panel; ASCII unverändert; Tests `PromptViewTests` (Flat()-Style für den alten Marker-Look), `DialogTests`; Dateidialoge `OpenFileDialog`/`SaveFileDialog` (Basis `FileDialog : Prompt<string>` in `FileDialog.cs`, interner Ctor; Zeilen: Verzeichnis, immer `PageSize` Listenzeilen (aufgefüllt, konstante Zeilenzahl, `ChoiceNavigator`), Namensfeld (nur Save), Hinweis, Fehler; Einträge `..`/Verzeichnisse/Dateien sortiert, `Filter` = `;`-getrennte Platzhalter, `ShowHidden`, `..` über Windows-Laufwerksstamm = Laufwerksliste, `Width` kürzt mit „…“; Esc = `OperationCanceledException`; Save: Tippen = Feldfokus, Listentasten = Listenfokus, `ConfirmOverwrite` = zweites Enter; ohne ANSI/Sixel Pfad tippen (relativ zum Startverzeichnis, y/n beim Überschreiben, kein Filter); schreibt nie Dateien; CLI `open`/`save` mit `--dir`/`--filter`/`--caption`, interaktiv, nicht in `demo`; Tests `FileDialogTests`, `UiCliFileDialogTests`)), 0 Abhängigkeiten außer SharpSixel, eigenes NuGet `SharpSixel.Ui`. Caption/Prozent = Terminaltext neben Grafik (1 Zelle je Zeichen). Sixel-Frame adressiert relativ zum gespeicherten Ursprung (Cursor nach Sixel ist terminalabhängig). `Console.SetCursorPosition` bewusst nicht Standard (absolute Buffer-Koordinaten, wirft außerhalb Buffer, an Console gekoppelt, nicht testbar); optional später als `ICursorControl`-Fallback für ASCII ohne VT.
- `src/SharpSixel.Cli` – `encode`/`decode`/`check` (`CliApp` testbar über `CliEnvironment`; `ConsoleTerminal` = Raw-Mode + Timeout)
- `tests/SharpSixel.Tests` – xUnit. `docs/`: `README.txt` (Benutzer-Anleitung deutsch, mit echten Umlauten/ß), `README.en.txt` (englische Version, gleicher Inhalt) – beide werden beim Publish nach `publish/` kopiert; `all-about-sixels.txt` (Spec)

## Befehle
- Test: `dotnet test`
- Alles in einem Lauf: `./publish.ps1 [-Version x.y.z]` → `publish/SharpSixel.exe` (win-x64, self-contained, Single-File, komprimiert), `publish/README.txt` + `publish/README.en.txt`, `publish/nuget/*.nupkg` + `*.snupkg` (SharpSixel, SharpSixel.Skia, SharpSixel.Ui, SharpSixel.Osc; Icon `docs/images/icon.png`, SourceLink über das SDK). `publish/` ist gitignored.
- Einzeln: `dotnet publish src/SharpSixel.Cli -c Release -p:PublishProfile=win-x64`; `dotnet pack src/SharpSixel -o publish/nuget`
- **Nur Release**: `Directory.Build.targets` bricht Pack/Publish bei Debug ab; `PackRelease`/`PublishRelease` setzen Release als Default.
- NuGet nur lokal, nie hochladen ohne Auftrag.

## Regeln
- Keine neuen NuGets ohne Rückfrage (MIT bevorzugt). SkiaSharp (MIT) ist freigegeben, nur in Skia/CLI.
- Kern netstandard2.0-kompatibel: kein Span/System.Memory, kein `init`/`record`.
- Code/Kommentare englisch; Chat deutsch, knapp. Clean Code, kleine Klassen; Fehler vor neuen Features beheben; Public API mit XML-Doku (TreatWarningsAsErrors).
- Pixelformat: RGBA8888, straight alpha, zeilenweise.
- Shell-Zugriff (PowerShell-MCP) ist oft verzögert verfügbar: bei Fehlern/"nicht verfügbar" mehrfach mit Pausen dazwischen erneut versuchen (z. B. `Wait`-Tool), bevor aufgegeben wird; erst dann "nicht gebaut/getestet" melden.
- xUnit: `Assert.Contains`/`DoesNotContain` mit Strings, die ESC (`\u001b`) enthalten, immer `StringComparison.Ordinal` verwenden (kulturabhängiger Vergleich ignoriert Steuerzeichen, `DoesNotContain("\u001b", …)` schlägt sonst fälschlich an).
- Publish-Profil (`src/SharpSixel.Cli/Properties/PublishProfiles/win-x64.pubxml`) gilt nur beim Publish (kein RID im Projekt). Skia-Native im Bundle, PDBs entfernt, Exe-Umbenennung + README-Kopie im Target `FinalizeSingleFileExe`. Kein Trimming (Skia/P-Invoke).
- Nutzerdoku `docs/README.txt` und `docs/README.en.txt` bei Feature-/CLI-Änderungen gemeinsam pflegen (inhaltlich deckungsgleich, deutsche Version mit ä/ö/ü/ß statt ae/oe/ue/ss-Umschreibung). Beide enthalten einen freiwilligen KI-Hinweis mit Modellversion (Abschnitt 16: Claude Sonnet 5.5, laut Autor die einzige eingesetzte Modellversion) – bei Modellwechsel aktualisieren.

## Spec-Entscheidungen
- P1-Tabelle nach VT330/340-Handbuch (0/1→2:1, 2→5:1, 3/4→3:1, 5/6→2:1, 7-9→1:1); `all-about-sixels.txt` weicht ab (veraltet).
- HLS: DEC-Hue (Blau 0°, Rot 120°, Grün 240°) → Standard-Hue = (h+240) mod 360. Farbwerte in Prozent (nur Vielfache von 20 % verlustfrei).
- Unbelegte Pixel: Hintergrund (opak) bei P2≠1, transparent bei P2=1. Größe: max(Raster Ph/Pv, Daten); deklarierte Höhe kürzt nur Band-Padding.
- Ohne `#`-Auswahl: Vordergrund weiß. Register ≥ N: modulo. VT340-Defaultpalette aus Implementierungen, nur Register 1 verifiziert.
- Decoder-Limits (16384 px je Achse, 64 Mi Pixel) gegen DoS.
- Encoder: exakte Palette wenn ≤ MaxColors, sonst Median Cut (5-5-5-Histogramm). Dithering None/FloydSteinberg/Ordered (CLI `--dither [floyd|bayer|none]`, bare = floyd), nur bei Farbreduktion.
- Skalierung: `ImageScaler` (`Fit` = so groß wie möglich bei Seitenverhältnis, Default `allowUpscale = true`; `Stretch`/`StretchSize` = Limits voll nutzen ohne Seitenverhältnis; CLI `--fit` unverändert: vergrößert nur mit `--upscale`); CLI `--width/--height/--cols/--rows/--fit/--upscale` (cols/rows = Zellen × Zellgröße aus `TerminalSize.CellWidth/CellHeight`; pro Achse nur px ODER Zellen). Terminalgröße per `CSI 14t/16t` + DA1 als Sentinel, Fallback 10x20-Zellen mit Warnung.
- Terminal-Check: DA1, Sixel = Attribut 4 (erster Parameter = Klasse). Bei Erfolg zeigt `check` ein kleines Testbild (CLI-Klasse `TestImage`, 128x64: Farbbalken, 16-stufiges Grau, Schachbrett, Rot-Blau-Verlauf; wenige Farben = exakte Palette), bei Misserfolg nichts.
- `--show` (nur encode): Terminal-Ausgabe erzwungen (sonst Fehler, Exit 1), DA1-Warnung bei fehlender Sixel-Meldung (entfällt bei Eingabe `-`), Zeilenumbruch nach dem Bild. Ohne `-o` nur anzeigen, mit `-o` speichern + anzeigen. Sixel-Eingabe nur per DCS-Header erkannt (`SixelDecoder.LooksLikeSixel`, bewusst keine Rohtext-Heuristik): ohne Größen-/Farb-/Dither-Option unverändert durchgereicht (vorher per Decode validiert), sonst dekodiert + neu enkodiert.

## Status / Offen
- Osc-Feature 1.2.0 fertig (`SharpSixel.Osc`, CLI `osc`, Tests, Doku `docs/README.Ui*.txt`, `publish.ps1`, Version 1.2.0): `dotnet test`, Build und Demo (`SharpSixelUI osc`) vom Nutzer als OK bestätigt. GitHub-READMEs (README.md, README.de.md) um Widgets, OSC und ein Mermaid-Übersichtsdiagramm (Abschnitt Overview/Überblick) ergänzt. Version 1.3.0: `ImageScaler.Fit` vergrößert per Default, neu `Stretch`/`StretchSize`; Tests grün (664 + 93), READMEs/Docs aktualisiert.
- UI-Feature 1.1.0 fertig: `SharpSixel.Ui` (ProgressBar, Spinner, LiveRegion) + CLI `src/SharpSixel.Cli.Ui` (SharpSixelUI.exe), 348 Tests grün, in Windows Terminal real verifiziert, `publish.ps1` läuft (3 NuGets 1.1.0). Lessons: Sixel-Höhe = Vielfaches von 6, Guard-Zeile gegen Scroll; `wt` trennt an `;`; PowerShell: `#RRGGBB` quoten. Screenshots: `tools\Screenshot.exe --title <Titel> --onlyfile <Pfad>`. Details → History. ToDo: Commit (Nutzer).
- Fertig: Decoder, Encoder (+Dithering), BMP, Skia, CLI (encode/decode/check, Skalierung), lokale NuGets, Single-File-Publish, Benutzer-README. 247 Tests grün.
- Windows real verifiziert (check inkl. Testbild, --fit, --show inkl. Sixel-Datei-Eingabe/Speichern, Publish-Exe). Unix-Pfad (`stty`) ungetestet.
- Offen: Linux-Natives (`SkiaSharp.NativeAssets.Linux`) + Unix-Test (evtl. zusätzlicher Linux-CI-Job); Autor und Repo-URL sind gesetzt.
- Version 1.4.0 (2026-10-31, committed): `WidgetEnvironment.Columns` (Terminalbreite; Table/Panel/Tree/BarChart/ProgressBar begrenzt), `decode -o -` + `--format`, NuGet-Icon/snupkg/SourceLink, Ui-Beschreibung; Tests grün (689 + 93). Offen: Test im echten Terminal (Spaltenbegrenzung), `./publish.ps1`-Lauf für 1.4.0.
- ToDo Backlog aus Funktionsprüfung (nicht gewählt): Linux-CI-Job + `SkiaSharp.NativeAssets.Linux` + Unix-Test (`stty`); XTSMGRAPHICS-Abfrage (Farbregister/Geometrie), DECSDM/`?1070`; Prompts mit Suche/Filter, `TextPrompt<T>`, MultiSelect "Alle"; ProgressBar indeterminate/Multi-Task; LiveRegion-Resize; Zeichenbreite (CJK/Emoji); Osc: OSC 8/52/9;4/7, Titel-Restore außerhalb Windows (CSI 22/23 t); CLI: `AlphaThreshold`/`EmitRasterAttributes`/Decoder-Hintergrund als Optionen; PixelAspect wird nirgends angewandt.
- Details/Messwerte/Historie → `CLAUDE-History.md`.

# CLAUDE.md

Behavioral guidelines to reduce common LLM coding mistakes. Merge with project-specific instructions as needed.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.
