================================================================================
 SharpSixelUI / SharpSixel.Ui  -  Terminal-Widgets als Sixel-Grafik
================================================================================

 Version : 1.4.0
 Autor   : Alexander Feuster
 Lizenz  : MIT
 English version: README.Ui.en.txt


1. WAS IST DAS?
---------------
SharpSixel.Ui ist eine .NET-Bibliothek (NuGet, hängt nur von SharpSixel ab),
die Widgets im Terminal zeichnet. Unterstützt das Terminal Sixel, wird das
Widget als Grafik gezeichnet, sonst aus ASCII-Zeichen aufgebaut. Style und
Farben sind anpassbar. Verfügbare Widgets:

  * ProgressBar  mit optionaler Caption und Prozent- oder Wert/Maximum-Anzeige
  * Spinner      mit optionaler Caption
  * StatusDisplay  Spinner mit änderbarem Statustext für Arbeit unbekannter Dauer
  * BarChart     horizontales Balkendiagramm: Label, Balken, Wert; optionale Überschrift
  * Table        Tabelle mit Spaltenköpfen und ausgerichteten Textzellen in einem Raster; optionale Überschrift
  * Button       Beschriftung mit Zustand (normal, fokussiert, gedrückt, deaktiviert)
  * Panel        Rahmen um Textzeilen mit optionalem Titel in der oberen Kante
  * Clock        Uhrzeit: Sixel als Analoguhr (Ziffernblatt, Stunden-, Minuten- und Sekundenzeiger) mit digitaler Zeit daneben, ASCII nur digital
  * DateDisplay  Datum: Sixel als Kalenderblatt mit Tagesnummer und Datumstext daneben, ASCII nur Text
  * Tree         Hierarchische Liste mit Führungslinien, optionalem Titel als Wurzel und einklappbaren Knoten
  * ImageView    Bild, das in eine View fester Größe eingepasst wird; optionaler
                 Rahmen, eigener Hintergrund; Bild als Grafik oder aus einer Datei
  * Prompts      interaktive Eingabe mit Validierung: Dialog (Meldung mit Buttons),
                 SelectionPrompt (ein Eintrag),
                 MultiSelectionPrompt (mehrere Einträge), TextPrompt (Texteingabe),
                 OpenFileDialog / SaveFileDialog (Dateipfad wählen)

SharpSixelUI.exe ist ein Demo-Programm für diese Widgets. Das Hauptprogramm
SharpSixel.exe ist in README.txt beschrieben.


2. DAS DEMO-PROGRAMM
--------------------
    SharpSixelUI <progress|spinner|status|chart|table|button|panel|clock|date|tree|image|prompts|open|save|osc|demo> [Optionen]

  progress    animierte ProgressBar von 0 bis 100 %
  spinner     animierter Spinner
  status      Statusanzeige: Spinner mit wechselndem Text, Meldungen laufen darüber
  chart       animiertes Balkendiagramm (Demo-Daten wachsen auf ihre Werte)
  table       Tabelle mit Demo-Daten; --caption setzt die Überschrift, --border die Rasterfarbe, --active die Textfarbe
              des Tabellenkopfs, --inactive dessen Hintergrund, --track den Hintergrund jeder zweiten Zeile
  button      ein Button, der seine Zustände durchläuft (normal, fokussiert, gedrückt, deaktiviert); --caption setzt die Beschriftung
  panel       Rahmen mit Demo-Zeilen; --caption setzt den Titel (Standard Status), --width die Textbreite in Zellen
              (Standard: breiteste Zeile), --border die Rahmenfarbe, --active die Titelfarbe, --background die
              Innenfläche, --radius den Eckenradius
  clock       zeigt die aktuelle Uhrzeit, laufend aktualisiert (--duration Sekunden, Standard 5); --caption setzt eine
              Beschriftung davor, --rows die Größe der Grafik (1-20, Standard 3), --border den Rand, --fill das Ziffernblatt,
              --active die Zeiger, --background den Hintergrund der Grafik
  date        zeigt das aktuelle Datum in Sprache und Reihenfolge des Systems (z. B. Mi, 28.10.2026);
              --caption, --rows, --background wie bei clock, außerdem
              --radius (Eckenradius), --border (Rahmen), --fill (Blatt) und --active (Tagesnummer)
  tree        zeigt einen Baum mit Demo-Knoten (--caption setzt den Titel der Wurzel, Standard "Files"; der eingeklappte
              Knoten "tests" ist mit [+] markiert); --border setzt die Farbe der Führungslinien, --active die Titelfarbe
  image       zeigt ein Bild eingepasst in eine View: --file (BMP oder Sixel) oder ein
              erzeugtes Demo-Bild; --border zeichnet einen Rahmen, --background setzt
              den Hintergrund der View
  prompts     interaktiv: Dialog, Auswahl-, Mehrfachauswahl-, Text- und Passwort-
              mit Validierung (Auf/Ab, Leertaste, Enter, Mausklick; Strg+C bricht ab);
              braucht ein Terminal und ist nicht Teil von demo
  open        interaktiver Datei-öffnen-Dialog: --dir setzt das Startverzeichnis, --filter die angezeigten
              Dateien (z. B. *.bmp;*.sixel), --caption den Titel; gibt den gewählten Pfad aus; braucht ein
              Terminal, nicht Teil von demo
  save        interaktiver Datei-speichern-Dialog (wie open, mit Namensfeld); gibt nur den Pfad aus, schreibt nichts
  osc         ändert das Terminal mit SharpSixel.Osc: --caption setzt den Titel (Standard "SharpSixel OSC demo"), --fill die
              Textfarbe, --background die Hintergrundfarbe, --active die Cursorfarbe (Standard: Gold, Dunkelblau, Rot); der
              Cursorstil durchläuft Block, Unterstrich und Balken (blinkend und statisch) während --duration Sekunden;
              Strg+C bricht ab; danach wird alles zurückgesetzt; braucht ein Terminal, nicht Teil von demo
  demo        ProgressBar, Spinner, Statusanzeige, Balkendiagramm, Tabelle, Button, Panel, danach Uhrzeit, Datum und Baum (Standard-Captions "Download"/"Loading"/"Fruit sales"/"Fruit stock"/"Status"/"Files")

Optionen:

  --mode auto|sixel|ascii        auto (Standard): Sixel, wenn das Terminal es meldet
  --caption <Text>               Caption des Widgets (status: fester Statustext statt Demo-Stufen)
  --text none|percent|fraction   Text hinter dem Balken (progress, Standard percent)
  --duration <Sekunden>          Laufzeit je Widget (Standard 5)
  --width <Zellen>               Breite in Zellen (progress: Balken, mindestens 3; chart: Balkenfläche, mindestens 1; button: Mindestbreite; panel: Textbreite; image: View, Standard 40)
  --rows <n>                     Zeilen der Sixel-Grafik (progress 1-20; chart: Zeilen je Balken 1-10; button 1-10; image: Höhe der View, Standard 10; clock, date: Größe 1-20, Standard 3)
  --file <Pfad>                  Bilddatei des Befehls image (BMP oder Sixel)
  --dir <Pfad>                   Startverzeichnis von open/save (Standard: aktuelles Verzeichnis)
  --filter <Muster>              open/save: nur passende Dateien anzeigen, durch Semikolon getrennt (z. B. *.bmp;*.sixel)
  --radius <px>                  Eckenradius in Pixeln (progress, chart, button, panel, date)
  --fill | --track | --border <Farbe>   Farben des Balkens (progress; chart: --track; table: --border = Raster, --track = Streifen; image: --border = Rahmenfarbe; panel: --border = Rahmen; clock: --fill = Ziffernblatt, --border = Rand; date: --fill = Blatt, --border = Rahmen; tree: --border = Führungslinien)
  --active | --inactive <Farbe>         Farben des Spinners (prompts: aktueller/übrige Einträge; button: fokussierte/normale Fläche; table: Kopftext/-hintergrund; panel: --active = Titeltext; clock: --active = Zeiger; date: --active = Tagesnummer; tree: --active = Titeltext)
  --background <Farbe>           Hintergrund der Grafik (prompts, open, save: der Marker; image: der View; panel: Innenfläche des Rahmens; clock, date: der Grafik)
  --no-ansi                      keine ANSI-Farben im ASCII-Modus
  --no-mouse                     keine Mauseingabe (prompts, open, save, button --click)
  --click                        button: wartet, bis der Button angeklickt wird (oder Enter/Leertaste); braucht ein Terminal
  -h, --help                     Hilfe

Farben: #RRGGBB, #RRGGBBAA (Alpha) oder "transparent".

Beispiele:

    SharpSixelUI demo
    SharpSixelUI progress --caption Upload --text fraction --fill #E67E22 --rows 2
    SharpSixelUI spinner --mode ascii --caption "Bitte warten"
    SharpSixelUI status
    SharpSixelUI chart --caption Umsatz --rows 2 --track #333333
    SharpSixelUI table --caption Lager --border #3498DB --track #202830
    SharpSixelUI button --caption Speichern --rows 2 --active #E67E22
    SharpSixelUI panel --caption Status --border #3498DB --background #101820
    SharpSixelUI clock --caption Zeit --rows 4 --active #E67E22
    SharpSixelUI date --caption Heute --border #3498DB
    SharpSixelUI tree --caption Projekt --border #3498DB
    SharpSixelUI image --file logo.bmp --width 30 --rows 8 --border #3498DB --background #101820
    SharpSixelUI prompts --mode ascii
    SharpSixelUI open --dir C:\Images --filter "*.bmp;*.sixel"
    SharpSixelUI save --caption Export --mode ascii
    SharpSixelUI progress --duration 3 > out.txt      (umgeleitet: eine ASCII-Zeile)

Strg+C beendet die Animation sauber. Ist die Ausgabe umgeleitet (Datei, Pipe),
gibt es keine Animation: das Widget wird einmal als ASCII-Text ausgegeben
(chart, table, panel: eine Zeile je Balken/Zeile/Textzeile).
Rückgabewerte: 0 Erfolg, 1 Fehler (ungültiges Argument, unbekannter Befehl).


3. VERWENDUNG ALS BIBLIOTHEK
----------------------------
    dotnet add package SharpSixel.Ui      (aus dem lokalen Ordner "nuget")

Zielplattformen: .NET Standard 2.0 und .NET 10, keine Abhängigkeiten außer SharpSixel.

    using SharpSixel;
    using SharpSixel.Ui;

    SixelTerminalInfo? info = SixelTerminal.Query(Console.Out, readChar);
    var env = WidgetEnvironment.Create(RenderMode.Auto, info);

    var bar = new ProgressBar { Caption = "Download" };
    bar.Style.FillColor = new SixelColor(230, 126, 34);
    bar.Style.Width = 40;

    using (var region = new LiveRegion(Console.Out, env))
    {
        region.Begin(bar.GetRows(env));
        for (int i = 0; i <= 100; i++)
        {
            bar.Value = i;
            region.Update(bar);
            Thread.Sleep(30);
        }
    }

readChar ist die Eingabefunktion der Terminal-Abfrage (siehe README.txt, Abschnitt 12).
Das Terminal muss VT-Sequenzen verarbeiten (unter Windows aktivieren).

Statusanzeige für Arbeit unbekannter Dauer (Spinner mit Text, Timer-Animation):

    using (var status = new StatusDisplay(Console.Out, env))
    {
        status.Start("Lade Daten ...");
        status.Text = "Verbinde ...";
        status.WriteLine("Konfiguration geladen");   // erscheint oberhalb des Spinners
    }   // Dispose entfernt die Anzeige und zeigt den Cursor wieder

Balkendiagramm (Elemente per Add, Werte jederzeit änderbar):

    var chart = new BarChart { Caption = "Umsatz" };
    chart.Style.Width = 30;
    chart.Add("Äpfel", 42);
    chart.Add("Birnen", 28, new SixelColor(231, 76, 60));   // eigene Farbe

    using (var region = new LiveRegion(Console.Out, env))
    {
        region.Begin(chart.GetRows(env));
        chart.Items[0].Value = 50;
        region.Update(chart);
    }

Tabelle (Spalten mit Ausrichtung, Zeilen als Texte; Zellen jederzeit änderbar):

    var table = new Table { Caption = "Lager" };
    table.AddColumn("Obst");
    table.AddColumn("Menge", TableAlignment.Right, width: 8);   // feste Breite hält die Größe stabil
    string[] apples = table.AddRow("Äpfel", "42");
    table.AddRow("Birnen", "28");
    table.Style.ShowRowSeparators = true;
    table.Style.StripeColor = new SixelColor(32, 40, 48);       // jede zweite Zeile (Sixel)

    using (var region = new LiveRegion(Console.Out, env))
    {
        region.Begin(table.GetRows(env));
        apples[1] = "50";
        region.Update(table);
    }

Button (zeichnet nur; State wird zwischen den Frames geändert, z. B. bei Tastendruck):

    var button = new Button("Speichern");
    button.Style.MinWidth = 12;
    button.State = ButtonState.Focused;      // Normal, Focused, Pressed, Disabled

    using (var region = new LiveRegion(Console.Out, env))
    {
        region.Begin(button.GetRows(env));
        region.Update(button);
        button.State = ButtonState.Pressed;
        region.Update(button);
    }

Panel (Rahmen um Textzeilen, optionaler Titel; Zeilen jederzeit änderbar):

    var panel = new Panel { Title = "Status", Width = 20 };   // feste Breite hält die Größe stabil
    panel.AddLine("Verbunden");
    panel.AddLine("Dateien: 0");
    panel.Style.BackgroundColor = new SixelColor(16, 24, 32); // Innenfläche (Sixel)

    using (var region = new LiveRegion(Console.Out, env))
    {
        region.Begin(panel.GetRows(env));
        panel.Lines[1] = "Dateien: 42";   // Zeilen änderbar, ihre Anzahl darf nicht wachsen
        region.Update(panel);
    }

Uhrzeit und Datum (die Widgets ticken nicht selbst: Time/Date vor jedem Frame setzen):

    var clock = new Clock { Caption = "Zeit" };
    clock.Style.HeightRows = 2;            // Größe der Analoguhr (Sixel)
    clock.Style.TimeFormat = "HH:mm:ss";   // Format der digitalen Zeit (Standard)

    using (var region = new LiveRegion(Console.Out, env))
    {
        region.Begin(clock.GetRows(env));
        clock.Time = DateTime.Now;
        region.Update(clock);
    }

    var date = new DateDisplay { Date = DateTime.Today };   // Text in der Kultur des Systems, z. B. "Mi, 28.10.2026"
    // wie oben: region.Begin(date.GetRows(env)); region.Update(date);

Baum (hierarchische Liste mit Führungslinien; der Titel ist die Wurzel, eingeklappte Knoten verbergen ihre Kinder):

    var tree = new Tree { Title = "Files" };
    TreeNode src = tree.AddNode("src");
    src.AddNode("Program.cs");
    tree.AddNode("docs").AddNode("README.md");
    TreeNode tests = tree.AddNode("tests");
    tests.AddNode("TreeTests.cs");
    tests.Expanded = false;                 // wird als "tests [+]" gezeigt

    using (var region = new LiveRegion(Console.Out, env))
    {
        region.Begin(tree.GetRows(env));    // Zeilen = Titel + sichtbare Knoten, darf später nicht wachsen
        region.Update(tree);
    }

Bildanzeige (das Bild wird automatisch in die View eingepasst):

    var view = new ImageView(40, 10, new ImageViewStyle { ShowBorder = true });
    view.Style.BackgroundColor = new SixelColor(16, 24, 32);   // unabhängig vom Terminal
    view.Load("logo.bmp");                                     // oder view.Image = someSixelImage;
    view.Decoder = SkiaImageCodec.Decode;                      // optional: PNG/JPEG über SharpSixel.Skia

    using (var region = new LiveRegion(Console.Out, env))
    {
        region.Begin(view.GetRows(env));
        region.Update(view);
    }

Prompts (Show liefert die validierte Antwort; Tasten von Console.ReadKey(true)):

    var dialog = new Dialog { Title = "Datei löschen?", Message = "report.txt wird entfernt." };
    dialog.SetButtons("Löschen", "Abbrechen");
    dialog.DefaultButton = 1;
    bool delete = dialog.Show(Console.Out, env) == 0;

    var fruit = new SelectionPrompt<string> { Title = "Lieblingsobst?" };
    fruit.AddChoices(new[] { "Äpfel", "Birnen", "Pflaumen" });
    string one = fruit.Show(Console.Out, env);

    var toppings = new MultiSelectionPrompt<string> { Title = "Zutaten?", MinSelected = 1, MaxSelected = 3 };
    toppings.AddChoice("Sahne", selected: true);
    toppings.AddChoice("Zucker");
    IReadOnlyList<string> many = toppings.Show(Console.Out, env);

    var name = new TextPrompt { Title = "Name?", MinLength = 2 };
    name.Validator = v => v == "root" ? ValidationResult.Error("Reservierter Name.") : ValidationResult.Success();
    string text = name.Show(Console.Out, env);

    var open = new OpenFileDialog { Title = "Bild öffnen", InitialDirectory = @"C:\Images", Filter = "*.bmp;*.sixel" };
    string path = open.Show(Console.Out, env);       // vollständiger Pfad; Esc wirft OperationCanceledException

    var save = new SaveFileDialog { FileName = "bild.sixel", ConfirmOverwrite = true };
    string target = save.Show(Console.Out, env);     // wählt einen Pfad, schreibt nichts

Maus (Prompts, Dateidialoge und Button): ShowWithMouse nimmt eine Quelle für Tasten und
Mausmeldungen. Das Terminal sendet sie als VT-Sequenzen auf der Standardeingabe;
VtInputParser macht aus den Zeichen InputEvents (Taste, Maus, Cursorposition):

    var parser = new VtInputParser(readChar, readCharShort);   // -1 = Ende / Timeout
    bool delete = dialog.ShowWithMouse(Console.Out, env, parser.Read) == 0;

Ein Linksklick wählt einen Dialog-Button oder einen Auswahleintrag und schaltet
einen Mehrfachauswahl-Eintrag um und öffnet in einem Dateidialog ein Verzeichnis bzw.
wählt eine Datei; das Mausrad bewegt den Cursor. Für eigene Widgets:

    using (var region = new LiveRegion(Console.Out, env))
    {
        region.Begin(button.GetRows(env));
        region.EnableMouse(parser.Read);              // Mausmeldungen an, Ursprung abgefragt
        region.Update(button);
        InputEvent e = parser.Read();
        if (e.Mouse != null && region.TryGetLocal(e.Mouse, out int col, out int row)
            && button.Contains(env, col, row)) { /* angeklickt */ }
    }

Terminal-Steuerung (eigenes Paket SharpSixel.Osc, hängt nur von SharpSixel ab; jede Methode nimmt optional einen
TextWriter, Standard Console.Out):

    using SharpSixel.Osc;

    TerminalControl.SetTitle("Mein Tool");                          // Steuerzeichen werden entfernt
    TerminalControl.SetForegroundColor("#FFD700");                  // #RGB oder #RRGGBB, sonst ArgumentException
    TerminalControl.SetBackgroundColor(new SixelColor(16, 32, 64)); // SixelColor, Alpha wird ignoriert
    TerminalControl.SetCursorColor("#FF4040");
    TerminalControl.SetCursorStyle(CursorStyle.BarBlinking);        // Default, Box*, Underscore*, Bar*
    TerminalControl.Reset();   // Farben und Cursorstil auf Standard; Titel nur unter Windows wiederhergestellt

Typen:

  Prompt<T>           Basis der Prompts: Title, Style, Validator, Show(writer, env, readKey),
                      ShowWithMouse(writer, env, readInput)
  InputEvent          Kind (Key, Mouse, CursorPosition), Key, Mouse, Row, Column
  MouseEvent          Column, Row (0-basiert), Button (Left, Middle, Right, WheelUp, WheelDown), IsPress
  VtInputParser       Read(): VT-Zeichen zu InputEvents (Tasten, SGR-Maus, Cursorposition)
  Dialog              Message, Buttons, SetButtons, DefaultButton; Show liefert den Button-Index
  SelectionPrompt<T>  AddChoice, AddChoices, Choices, Converter, PageSize, Instructions
  MultiSelectionPrompt<T>  wie SelectionPrompt plus AddChoice(item, selected),
                      MinSelected, MaxSelected
  TextPrompt          DefaultValue, AllowEmpty, MinLength, MaxLength, Pattern,
                      PatternMessage, IsSecret, Mask
  FileDialog          Basis von OpenFileDialog und SaveFileDialog: InitialDirectory, Filter, ShowHidden,
                      PageSize, Width, Instructions; Show liefert den vollständigen Pfad
  OpenFileDialog      wählt eine vorhandene Datei
  SaveFileDialog      FileName, ConfirmOverwrite; Namensfeld, wählt einen Pfad (schreibt nichts)
  ValidationResult    Success(), Error(message), IsValid, Message
  PromptStyle         ActiveColor, InactiveColor, ErrorColor, BackgroundColor,
                      AsciiPointer, AsciiRadioOn/Off, AsciiCheckOn/Off
  WidgetEnvironment   Sixel oder ASCII, Zellgröße in Pixeln, ANSI-Nutzung, Columns
                      (Terminalbreite in Zellen, 0 = unbekannt); Create(RenderMode,
                      SixelTerminalInfo?, useAnsi, columns) entscheidet nach Modus
  RenderMode          Auto, Sixel, Ascii
  Widget              Basisklasse: Render(env) liefert einen Frame, GetRows(env)
  BarChart            Items, Add(label, value, color), Caption, Maximum, ShowValues,
                      ValueFormat, Style
  BarChartItem        Label, Value, Color (null = Farbe aus Style.Palette)
  BarChartStyle       Width, BarHeightRows, BarThickness, CornerRadius, Palette,
                      TrackColor, BackgroundColor, AsciiFill, AsciiEmpty
  Button              Label, State, Columns, Style
  ButtonState         Normal, Focused, Pressed, Disabled
  ButtonStyle         Padding, MinWidth, HeightRows, CornerRadius, NormalColor,
                      FocusedColor, PressedColor, DisabledColor, TextColor,
                      DisabledTextColor, BackgroundColor, AsciiBorderLeft/Right,
                      AsciiFocusLeft/Right, AsciiPressedMarker, AsciiDisabledMarker
  Panel               Lines, AddLine(text), Title, Width (null = an die breiteste Zeile/den Titel angepasst), Style
  PanelStyle          Padding, LineThickness, CornerRadius, BorderColor, TitleColor,
                      BackgroundColor, AsciiHorizontal, AsciiVertical, AsciiCorner
  Clock               Time, Caption, Style
  ClockStyle          HeightRows, LineThickness, ShowSecondHand, TimeFormat, BorderColor, FaceColor,
                      TickColor, HandColor, SecondHandColor, TextColor, BackgroundColor
  DateDisplay         Date, Caption, Style
  DateDisplayStyle    HeightRows, LineThickness, CornerRadius, DateFormat, Culture, BorderColor, AccentColor,
                      DigitColor, PaperColor, TextColor, BackgroundColor
  Tree                Title, Nodes, AddNode(text), Style
  TreeNode            Text, Expanded, Nodes, AddNode(text)
  TreeStyle           LineThickness, CollapsedSuffix, GuideColor, TitleColor,
                      AsciiVertical, AsciiBranch, AsciiLastBranch, AsciiHorizontal
  ImageView           Image, Load(Pfad|Stream|Bytes), Decoder, AllowUpscale, Columns, Rows, Style
  ImageViewStyle      ShowBorder, BorderColor, BorderThickness, BackgroundColor,
                      AsciiCorner, AsciiHorizontal, AsciiVertical, AsciiRamp
  ProgressBar         Value, Maximum, Caption, ProgressText, Style
  ProgressBarStyle    Width, HeightRows, BarThickness, CornerRadius,
                      BorderThickness, FillColor, TrackColor, BorderColor,
                      BackgroundColor, AsciiBorderLeft/Right, AsciiFill, AsciiEmpty
  Spinner             Caption, Advance(), Frame, Style
  SpinnerStyle        HeightRows, SegmentCount, TrailLength, DotScale,
                      ActiveColor, InactiveColor, BackgroundColor, AsciiFrames
  Table               Columns, Rows, AddColumn(header, alignment, width), AddRow(cells...),
                      Caption, Style
  TableColumn         Header, Alignment, Width (null = an den breitesten Text angepasst)
  TableAlignment      Left, Right, Center
  TableStyle          Padding, LineThickness, ShowBorder, ShowHeader, ShowColumnSeparators,
                      ShowRowSeparators, BorderColor, HeaderTextColor, HeaderBackgroundColor,
                      StripeColor, AsciiHorizontal, AsciiVertical, AsciiCorner
  StatusDisplay       Start(text), Text, WriteLine(text), Refresh(), Stop(), IsRunning,
                      AutoRefresh, RefreshInterval, SpinnerStyle
  LiveRegion          Begin(rows), Update(widget), End(): zeichnet an Ort und Stelle neu;
                      EnableMouse(readInput), TryGetLocal(mouse, out col, out row)


4. HINWEISE UND GRENZEN
-----------------------
  * WidgetEnvironment.Columns (Terminalbreite in Zellen) begrenzt Widgets mit flexibler
    Breite: Table (breiteste Spalten werden schmaler, Text mit Auslassungszeichen
    gekürzt), Panel, Tree, BarChart und ProgressBar (Balken wird schmaler). Create
    übernimmt den Wert aus dem Argument columns oder leitet ihn aus gemeldeter
    Textfläche und Zellgröße ab; 0 bedeutet unbekannt, dann wird nichts begrenzt. Die
    Demo-CLI übergibt Console.WindowWidth. Prompts, Dialoge und die übrigen Widgets
    werden nicht begrenzt.
  * Caption und Fortschrittstext sind normaler Terminaltext neben der Grafik
    (Sixel hat keine Schrift). Angenommen wird eine Zelle pro Zeichen.
  * LiveRegion reserviert zuerst die Zeilen, merkt sich die Cursorposition
    (ESC 7 / ESC 8) und bewegt sich relativ dazu. Es schreibt in jeden
    TextWriter. Console.SetCursorPosition wird nicht verwendet: Es arbeitet mit
    absoluten Buffer-Koordinaten, wirft außerhalb des Buffers und ist ohne
    Konsole nicht testbar.
  * Wo der Cursor nach einem Sixel-Bild steht, ist je nach Terminal verschieden;
    deshalb wird jeder Sixel-Frame relativ zum gemerkten Ursprung positioniert.
  * Bei transparentem Hintergrund (Standard) werden die belegten Zellen vor jedem
    Frame gelöscht (ECH, ESC [ n X). Lässt das Terminal alte Pixel stehen,
    eine deckende BackgroundColor setzen.
  * StatusDisplay animiert per Timer (RefreshInterval, Standard 100 ms; ohne
    AutoRefresh nur bei Refresh()). Während es läuft nicht direkt in die
    Ausgabe schreiben, sondern WriteLine verwenden. Ohne ANSI/Sixel wird jeder
    Textwechsel als eigene Zeile ausgegeben.
  * BarChart: Die Elementanzahl darf nach LiveRegion.Begin nicht über die
    reservierten Zeilen wachsen und sollte während der Anzeige gleich bleiben.
    Ohne Maximum entspricht der größte Wert der vollen Breite; negative Werte und
    NaN ergeben leere Balken. Im ASCII-Modus liefert Render eine Zeile je Balken
    (getrennt durch \n, bei Überschrift zuerst); LiveRegion setzt jede Zeile auf
    ihre Position. Ohne LiveRegion die Zeilen direkt ausgeben.
  * Table: Sixel und ASCII verwenden dieselben Zeilen (Überschrift, obere Linie, Kopf,
    Kopflinie, eine Zeile je Datenzeile, untere Linie; Zeilenlinien nur mit
    ShowRowSeparators), GetRows ist daher in beiden Modi gleich. Die Zeilenanzahl
    darf nach LiveRegion.Begin nicht über die reservierten Zeilen wachsen; feste
    TableColumn.Width-Werte halten die Breite stabil, während sich Zellen ändern
    (sonst passen sich die Spalten an den breitesten Text an). Längerer Text wird
    mit Auslassungszeichen gekürzt, fehlende Zellen sind leer, Steuerzeichen werden
    zu Leerzeichen. Sixel: Das Raster wird als dünne Pixellinien in eigenen Zellen
    gezeichnet (Terminals löschen Grafik unter Text teils), Kopf und Zelltexte sind
    Terminaltext; Kopf- und Streifenhintergrund (ANSI) und LineThickness gelten nur
    für Sixel. ASCII: Raster aus + - | mit optionalen ANSI-Farben für Raster und Kopf.
  * Button: zeichnet einen Button, behandelt aber weder Tasten noch Klicks; State
    zwischen den Frames setzen (interaktive Button-Reihe: siehe Dialog; Contains
    sagt, ob eine Mausposition den Button trifft). Sixel:
    abgerundete Fläche in der Farbe des Zustands, die Beschriftung wird als
    Terminaltext zentriert auf der Fläche ausgegeben (mit der Flächenfarbe als
    Textuntergrund, damit es auch klappt, wenn das Terminal Grafik unter Text
    löscht); bei einer Zeile ist die Grafik etwas niedriger als die Zelle
    (Vielfaches von 6 Pixeln). ASCII: [ OK ] mit den Markern [>OK<] fokussiert,
    [#OK#] gedrückt, [-OK-] deaktiviert (mit ANSI gefärbt). Breite = Label +
    2 * (Padding + 1) Zellen, mindestens MinWidth.
  * Panel: Rahmen um Textzeilen mit optionalem Titel in der oberen Kante. Sixel und
    ASCII verwenden dieselben Zeilen (obere Kante, eine Zeile je Textzeile, untere
    Kante), GetRows ist daher in beiden Modi gleich; nach LiveRegion.Begin darf die
    Zeilenanzahl nicht über die reservierten Zeilen wachsen. Width ist die Breite des
    Textbereichs ohne Padding und Rahmen: null passt sie an die breiteste Zeile bzw.
    den Titel an, ein fester Wert hält die Größe stabil (längerer Text und ein zu
    langer Titel werden mit Auslassungszeichen gekürzt, bei sehr schmalem Panel
    entfällt der Titel). Steuerzeichen werden zu Leerzeichen. Sixel: Der
    abgerundete Rahmen ist ein Bild (LineThickness, CornerRadius und BackgroundColor
    als Innenfläche gelten nur für Sixel); die Titelzellen bleiben ohne Linienpixel,
    da Terminals Grafik unter Text teils löschen; Titel und Zeilen sind Terminaltext
    (bei deckendem BackgroundColor mit ANSI-Hintergrund). ASCII: Rahmen aus + - |
    mit optionalen ANSI-Farben für Rahmen und Titel.
  * Clock: zeigt Time (nur die Uhrzeit); das Widget tickt nicht selbst, Time wird vor
    jedem Frame gesetzt (z. B. DateTime.Now). Sixel: quadratische Analoguhr (Rand, 12
    Stundenmarken, Stunden-, Minuten- und optional Sekundenzeiger) als ein Bild, dahinter
    die digitale Zeit als Terminaltext (TimeFormat, invariante Kultur, Standard
    "HH:mm:ss"; TextColor, sonst Terminalfarbe). HeightRows, LineThickness,
    ShowSecondHand und die übrigen Farben gelten nur für Sixel. ASCII: nur die digitale
    Zeit in einer Zeile. Caption steht davor.
  * DateDisplay: zeigt Date (nur das Datum), gesetzt wie bei Clock. Sixel: quadratisches
    Kalenderblatt (Rahmen, Trennlinie, zwei Bindungsringe, Tagesnummer in
    Sieben-Segment-Ziffern) als ein Bild, dahinter das formatierte Datum als Terminaltext
    (Culture, null = Kultur des Systems; DateFormat, null = Wochentagskürzel und kurzes
    Datum der Kultur, z. B. Mi, 28.10.2026 oder Wed, 10/28/2026). ASCII: nur der
    Datumstext. Ungültige Formate lehnt der Style-Setter mit FormatException ab.
  * Tree: hierarchische Liste aus TreeNode-Objekten (Text, Expanded, Nodes) mit optionalem
    Title als Wurzelzeile. Sixel und ASCII verwenden dieselben Zeilen (Titel, dann eine
    Zeile je sichtbarem Knoten; Kinder eingeklappter Knoten sind ausgeblendet, der Knoten
    erhält CollapsedSuffix, Standard " [+]"), daher darf die Zeilenzahl nach
    LiveRegion.Begin nicht über die reservierten Zeilen hinaus wachsen. Ein Knoten darf
    nicht sich selbst oder einem seiner Nachfahren hinzugefügt werden. Ohne Titel beginnen
    die Knoten der obersten Ebene am linken Rand. Sixel: Die Führungslinien
    (LineThickness, GuideColor) sind ein Bild in eigenen Spalten à 4 Zellen je Ebene; die
    Knotentexte stehen als Terminaltext hinter den Führungslinien, weil Terminals Grafik
    unter Text teils löschen, und sollten in eine Terminalzeile passen. ASCII: Führung aus
    | + ` - (Ascii*-Eigenschaften des Styles) mit optionalen ANSI-Farben für Linien und Titel.
  * ImageView: Die View hat eine feste Größe (Columns x Rows Zellen; Sixel-Bild
    Columns * Zellbreite mal GetImageHeight(Rows) Pixel). Das Bild wird beim
    Zeichnen in die Fläche innerhalb des Rahmens eingepasst: Seitenverhältnis
    bleibt, zentriert, vergrößert außer bei AllowUpscale = false; die eingepasste
    Kopie wird zwischengespeichert, nach Pixeländerungen Image neu zuweisen. Load
    liest BMP und Sixel; andere Formate brauchen einen Decoder (SharpSixel.Ui
    selbst hat keine Bildcodec-Abhängigkeit). BackgroundColor wird unabhängig vom
    Terminal-Hintergrund innerhalb des Rahmens gezeichnet; halbtransparente
    Pixel werden auf einen deckenden Hintergrund gemischt. Der ASCII-Fallback
    bildet einen Pixel auf eine Zelle ab (Zeichen aus AsciiRamp nach Helligkeit,
    ANSI-Farbe des Pixels), zeichnet ab 3x3 einen einzeichenbreiten Rahmen und
    ignoriert die Hintergrundfarbe. Demo: SharpSixelUI image liest nur BMP- und
    Sixel-Dateien.
  * Prompts: Show zeichnet den Prompt an Ort und Stelle (Sixel-Marker: Kreis bei
    Auswahl, Quadrat bei Mehrfachauswahl, Punkt bei Text; ASCII: Zeiger und
    (*) / [x]), liest Tasten bis die Antwort gültig ist, entfernt den Prompt und
    gibt eine Zusammenfassungszeile aus. Dialog: Links/Rechts/Tab (mit Umbruch),
    Pos1/Ende, Enter; der gewählte Button wird als [ Label ] gezeichnet (mit ANSI
    invertiert), mehrzeilige Meldung möglich. Tasten: Auf/Ab (mit Umbruch),
    Pos1/Ende, Bild auf/ab, Leertaste (Mehrfachauswahl), Enter; Text: Zeichen,
    Rücktaste, Enter (keine Cursorbewegung im Text). Die Validierung läuft bei
    Enter: zuerst die eingebauten Regeln (MinSelected/MaxSelected; AllowEmpty,
    MinLength, MaxLength, Pattern), danach der eigene Validator. Ungültige
    Eingaben zeigen ihre Meldung unter dem Prompt. Die Tasten kommen von
    Console.ReadKey(true), sofern keine readKey-Funktion übergeben wird. Ohne
    ANSI/Sixel werden die Einträge nummeriert und eine Zeile getippt (mehrere
    Nummern durch Komma getrennt; leer behält die Vorauswahl). Labels und Text
    sollten in eine Terminalzeile passen. Das Demo-Programm liest Tasten, solange
    das Terminal nicht im Raw-Modus ist (außer mit Maus, siehe unten).
  * Aussehen der Prompts im Sixel-Modus (PromptStyle): ein abgerundeter Rahmen um
    den Prompt (ShowFrame, FrameColor, CornerRadius; zwei Zeilen und Spalten mehr,
    Mauspositionen werden korrigiert), ein Balken hinter der aktuellen Zeile der
    Auswahl-Prompts und Dateidialoge (HighlightColor; Transparent schaltet ihn
    ab), größere Radio-/Check-/Punkt-Marker, Checkboxen mit aus dem Kasten
    ausgeschnittenem Häkchen und Dialog-Buttons als abgerundete Flächen
    (ButtonColor, ButtonTextColor; der gewählte Button nutzt ActiveColor). Ein
    deckendes BackgroundColor füllt den Prompt als Panel. Die Grafik wird zuerst
    gezeichnet, der Text darüber mit passendem ANSI-Hintergrund (Text in einer
    Zelle ersetzt die Grafik darunter). Der ASCII-Modus bleibt unverändert.
  * Dateidialoge: OpenFileDialog und SaveFileDialog sind Prompts, die das
    Dateisystem durchsuchen. Zeile 1 zeigt das aktuelle Verzeichnis, darunter
    PageSize Zeilen (konstant, aufgefüllt; längere Listen scrollen) mit ".." (unter
    einem Windows-Laufwerksstamm: die Laufwerksliste), den Verzeichnissen und
    danach den Dateien, nach Namen sortiert; versteckte und Systemeinträge nur mit
    ShowHidden; Filter (durch Semikolon getrennte Platzhalter * und ?) begrenzt die
    Dateien. Tasten: Auf/Ab (mit Umbruch), Pos1/Ende, Bild auf/ab; Enter öffnet ein
    Verzeichnis oder wählt eine Datei; Esc wirft OperationCanceledException (wie
    Strg+C im Demo). Namen länger als Width werden mit einer Ellipse gekürzt.
    SaveFileDialog hat zusätzlich ein Namensfeld: Zeichen und Rücktaste wählen das
    Feld, die Listentasten die Liste; Enter im Feld bestätigt den Namen im
    angezeigten Verzeichnis, Enter auf einer Datei der Liste übernimmt deren Namen
    und bestätigt. Mit ConfirmOverwrite (Standard) wird eine vorhandene Datei erst
    beim zweiten Enter (oder Klick) akzeptiert; der Validator sieht den vollständigen
    Pfad. Es wird nichts angelegt oder geschrieben. Ohne ANSI/Sixel tippt der
    Benutzer einen Dateinamen oder Pfad (relativ zum Startverzeichnis; leer =
    FileName) und beantwortet die Überschreiben-Frage mit y/n; Filter gilt dann
    nicht. Ein fehlendes InitialDirectory wirft InvalidOperationException. Ein
    Klick auf einen Listeneintrag öffnet das Verzeichnis bzw. wählt die Datei, ein
    Klick auf das Namensfeld fokussiert es.
  * Maus: ShowWithMouse und LiveRegion.EnableMouse schalten die Mausmeldungen ein
    (ESC [?1000h, ?1006h) und fragen die Cursorposition ab (ESC [6n), um zu wissen,
    wo die Region liegt; die Eingabe muss im VT-Modus gelesen werden (Windows:
    ENABLE_VIRTUAL_TERMINAL_INPUT, QuickEdit aus; Unix: stty -icanon -echo -isig).
    Mauspositionen gelten, solange der Bildschirm nicht scrollt oder die Größe
    ändert. Antwortet das Terminal nicht auf die Positionsabfrage, blockiert der
    Aufruf. Ein Klick auf einen Auswahleintrag bestätigt ihn (der Validator kann
    ihn ablehnen), ein Klick auf einen Mehrfachauswahl-Eintrag schaltet ihn um
    (Bestätigen mit Enter), Text-Prompts ignorieren die Maus. SharpSixelUI: prompts,
    open, save und button --click nutzen die Maus, außer mit --no-mouse; Strg+C wird als
    Zeichen gelesen und bricht ab. Nicht in jedem Terminal getestet.
  * Ist die Ausgabe kein Terminal, LiveRegion nicht verwenden, sondern
    widget.Render(env) mit einer ASCII-Umgebung aufrufen und die Zeile(n) ausgeben.


5. LIZENZ
---------
MIT (siehe LICENSE). Copyright (c) 2026 Alexander Feuster.
Teile dieses Projekts wurden mit Unterstützung des KI-Sprachmodells Claude von
Anthropic erstellt (siehe README.txt, Abschnitt 16).
