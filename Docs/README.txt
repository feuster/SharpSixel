================================================================================
 SharpSixel  -  Sixel-Grafiken enkodieren und dekodieren
================================================================================

 Version : 0.1.0  (aktuelle Version des Programms: SharpSixel --version)
 Autor   : Alexander Feuster
 Lizenz  : MIT
 English version: README.en.txt


INHALT
------
  1. Was ist SharpSixel?
  2. Lieferumfang
  3. Voraussetzungen
  4. Installation
  5. Schnellstart
  6. Befehle
       6.1  encode  - Bild in Sixel umwandeln
       6.2  decode  - Sixel in Bilddatei umwandeln
       6.3  check   - Sixel-Unterstützung des Terminals prüfen
  7. Optionen im Detail
       7.1  Farbanzahl (--colors)
       7.2  Dithering (--dither)
       7.3  Bildgröße anpassen (--width, --height, --fit, --upscale)
       7.4  Transparenz
       7.5  Anzeige im Terminal (--show)
  8. Eingabe und Ausgabe über Dateien und Pipes
  9. Rückgabewerte (Exit-Codes)
 10. Terminals mit Sixel-Unterstützung
 11. Fehlerbehebung
 12. Verwendung als .NET-Bibliothek (NuGet)
 13. Technische Details und Grenzen
 14. Aus den Quellen bauen
 15. Lizenz
 16. Hinweis zur Nutzung von KI (freiwillig)


1. WAS IST SHARPSIXEL?
----------------------
Sixel ist ein Grafikformat aus der Zeit der DEC-Terminals (VT240/VT340). Es
beschreibt ein Bild als Folge von Steuerzeichen, die ein Terminal direkt als
Pixelgrafik darstellen kann. Moderne Terminals (siehe Abschnitt 10) können
Sixel-Bilder anzeigen, ohne ein externes Programm zu starten.

SharpSixel besteht aus:

  * SharpSixel.exe   Kommandozeilenprogramm: Bilder (BMP, PNG, JPEG) in Sixel
                     umwandeln und Sixel-Daten wieder in Bilddateien.
  * SharpSixel       .NET-Bibliothek (NuGet) ohne Abhängigkeiten: Encoder,
                     Decoder, BMP-Unterstützung, Skalierung, Terminal-Abfrage.
  * SharpSixel.Skia  Ergänzungspaket (NuGet): PNG/JPEG über SkiaSharp.


2. LIEFERUMFANG
---------------
Nach dem Publish (./publish.ps1) liegen im Ordner "publish":

  SharpSixel.exe          das Programm (eine einzelne Datei)
  README.txt              diese Anleitung (deutsch)
  README.en.txt           englische Version dieser Anleitung
  nuget\SharpSixel.<Version>.nupkg
  nuget\SharpSixel.Skia.<Version>.nupkg


3. VORAUSSETZUNGEN
------------------
  * Windows 10/11, 64 Bit (x64)
  * Keine .NET-Installation nötig (das Programm bringt die Laufzeit mit)
  * Zum Anzeigen von Bildern: ein Terminal mit Sixel-Unterstützung
    (z. B. Windows Terminal ab Version 1.22, siehe Abschnitt 10)
  * Zum Erzeugen und Umwandeln von Dateien ist kein Sixel-Terminal nötig


4. INSTALLATION
---------------
Es gibt keinen Installer. SharpSixel.exe in einen beliebigen Ordner kopieren.
Optional den Ordner zur Umgebungsvariable PATH hinzufügen, dann ist der Befehl
"SharpSixel" in jeder Konsole verfügbar.

Erster Start: Die Exe entpackt eine native Bibliothek (SkiaSharp) in einen
temporären Ordner (%TEMP%\.net). Das dauert beim ersten Mal einen Augenblick
(etwa 150 ms), danach startet das Programm sofort. Soll ein anderer Ordner
verwendet werden, kann die Umgebungsvariable DOTNET_BUNDLE_EXTRACT_BASE_DIR
gesetzt werden.

Deinstallation: Exe löschen, optional den Ordner %TEMP%\.net\SharpSixel
entfernen.


5. SCHNELLSTART
---------------
Prüfen, ob das Terminal Sixel kann (bei Erfolg erscheint ein kleines Testbild):

    SharpSixel check

Ein Bild im Terminal anzeigen, passend zur Fenstergröße:

    SharpSixel encode foto.jpg --show --fit

Bild anzeigen und zugleich als Datei speichern:

    SharpSixel encode foto.jpg --show -o foto.six

Ein Bild verkleinern, auf 64 Farben reduzieren und als Datei speichern:

    SharpSixel encode foto.png --width 800 --colors 64 --dither bayer -o foto.six

Eine gespeicherte Sixel-Datei anzeigen:

    SharpSixel encode foto.six --show
    (oder in cmd.exe: type foto.six)

Eine Sixel-Datei in ein PNG umwandeln:

    SharpSixel decode foto.six -o foto.png

Hilfe und Version:

    SharpSixel --help
    SharpSixel --version


6. BEFEHLE
----------

6.1  encode  -  Bild in Sixel umwandeln
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Aufruf:

    SharpSixel encode <Eingabe> [-o <Datei>] [--show] [--colors <2-256>]
                      [--dither [floyd|bayer|none]]
                      [--width <px>] [--height <px>] [--fit] [--upscale]

  <Eingabe>   Pfad zu einer Bilddatei oder "-" für die Standardeingabe.
              Unterstützt: BMP (1/4/8/16/24/32 Bit, unkomprimiert), PNG, JPEG
              sowie weitere von SkiaSharp lesbare Formate. Auch eine
              Sixel-Datei (mit Kopf ESC P ... q) ist als Eingabe erlaubt: sie
              wird unverändert ausgegeben, solange keine Größen-, Farb- oder
              Dithering-Option angegeben ist; sonst wird sie dekodiert,
              angepasst und neu enkodiert.
  -o <Datei>  Ausgabedatei. Ohne -o werden die Sixel-Daten auf die
              Standardausgabe geschrieben. Bei einem Sixel-Terminal erscheint
              das Bild dann direkt im Fenster.
  --show      Zeigt das Sixel im Terminal an (Details in Abschnitt 7.5).

6.2  decode  -  Sixel in Bilddatei umwandeln
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Aufruf:

    SharpSixel decode <Eingabe> -o <Datei>

  <Eingabe>   Pfad zu einer Sixel-Datei oder "-" für die Standardeingabe.
              Die Daten dürfen mit der Steuersequenz ESC P ... q beginnen
              (7-Bit oder 8-Bit) und mit ESC \ enden; reine Sixel-Daten ohne
              Kopf werden ebenfalls akzeptiert.
  -o <Datei>  Pflicht. Das Ausgabeformat ergibt sich aus der Endung:
                .bmp           BMP (24 Bit, bei Transparenz 32 Bit mit Alpha)
                .png           PNG (verlustfrei, mit Transparenz)
                .jpg / .jpeg   JPEG (Qualität 90, keine Transparenz)

6.3  check  -  Sixel-Unterstützung des Terminals prüfen
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Aufruf:

    SharpSixel check

Das Programm fragt das Terminal per "Primary Device Attributes" (Steuersequenz
ESC [ c). Meldet das Terminal das Merkmal 4, kann es Sixel darstellen.
Ausgabe: "Terminal supports sixel graphics." (Exit-Code 0) oder
"Terminal does not support sixel graphics (or did not answer)." (Exit-Code 1).
Antwortet das Terminal nicht innerhalb von etwa einer Sekunde oder sind
Ein-/Ausgabe umgeleitet (Pipe, Datei), gilt das Ergebnis als "nicht
unterstützt".

Ist der Test erfolgreich, zeigt SharpSixel zusätzlich ein kleines eingebautes
Testbild (128 × 64 Pixel) im Terminal an: oben acht Farbbalken (Weiß, Gelb,
Cyan, Grün, Magenta, Rot, Blau, Schwarz), darunter ein Graustufenverlauf mit 16
Stufen, unten links ein Schachbrettmuster und unten rechts ein Verlauf von Rot
nach Blau. Erscheint dieses Bild, funktioniert die Sixel-Darstellung wirklich.
Bei Misserfolg wird kein Bild ausgegeben.


7. OPTIONEN IM DETAIL
---------------------

7.1  Farbanzahl (--colors)
~~~~~~~~~~~~~~~~~~~~~~~~~~
    --colors <n>      n = 2 bis 256, Standard: 256

Sixel arbeitet mit einer Farbpalette. Hat das Bild höchstens n verschiedene
Farben, wird die Palette exakt übernommen. Hat es mehr, wird die Palette per
Median-Cut-Verfahren auf n Farben reduziert.

Weniger Farben bedeuten kleinere Dateien und schnellere Darstellung, aber
sichtbare Farbstufen (Banding). Dagegen hilft Dithering (7.2).

Hinweis zur Genauigkeit: Sixel speichert Farbwerte in Prozent (0-100). Beim
Umrechnen von 8 Bit auf Prozent entsteht ein Rundungsfehler von höchstens etwa
1,3 Stufen (von 255). Farben mit Prozentwerten 0, 20, 40, 60, 80 und 100 sind
verlustfrei.

7.2  Dithering (--dither)
~~~~~~~~~~~~~~~~~~~~~~~~~
    --dither              Floyd-Steinberg (Kurzform)
    --dither floyd        Floyd-Steinberg (auch: floyd-steinberg)
    --dither bayer        geordnetes Dithering mit 8x8-Bayer-Matrix (auch: ordered)
    --dither none         kein Dithering (Standard)

Dithering mischt benachbarte Pixel aus Paletten-Farben, damit der Eindruck
zusätzlicher Farben entsteht. Es wirkt nur, wenn die Farben tatsächlich
reduziert werden müssen; Bilder, die in die Palette passen, bleiben
unverändert.

  floyd   Fehlerdiffusion. Sehr natürlicher Eindruck bei Fotos, etwas
          langsamer, größere Ausgabedatei.
  bayer   Regelmäßiges Punktraster. Schneller und deterministisch, erzeugt
          meist deutlich kleinere Ausgaben (bei 4K-Testbildern etwa 15–25 %
          kleiner als floyd), das Raster ist bei flächigen Verläufen sichtbar.

Beispiel:  SharpSixel encode foto.jpg --colors 16 --dither bayer

7.3  Bildgröße anpassen (--width, --height, --fit, --upscale)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Große Bilder erzeugen sehr viele Daten und überfüllen das Terminal. Mit den
folgenden Optionen wird das Bild vor dem Enkodieren verkleinert. Das
Seitenverhältnis bleibt immer erhalten.

    --width <px>      maximale Breite in Pixeln
    --height <px>     maximale Höhe in Pixeln
    --fit             in den darstellbaren Bereich des Terminals einpassen
    --upscale         kleine Bilder auch vergrößern (bis zum Limit)

Regeln:

  * Ohne --upscale wird nur verkleinert, nie vergrößert.
  * --width und --height dürfen einzeln oder zusammen angegeben werden; das
    Bild wird so skaliert, dass beide Grenzen eingehalten werden.
  * --fit fragt das Terminal nach seiner Größe in Pixeln. Ist eine Achse per
    --width oder --height vorgegeben, hat diese Vorrang; die andere Achse
    kommt vom Terminal. Von der Terminalhöhe wird eine Textzeile abgezogen,
    damit der Prompt nicht aus dem Fenster scrollt.
  * --fit braucht ein Terminal an Standardeingabe und Standardausgabe. Bei
    umgeleiteter Ausgabe (Datei, Pipe) bricht das Programm mit einem Hinweis
    ab; dann --width/--height verwenden.
  * Meldet das Terminal seine Pixelgröße nicht, schätzt das Programm
    10 × 20 Pixel pro Zeichenzelle und gibt auf der Fehlerausgabe die Warnung
    "terminal did not report its pixel size" aus.
  * Beim Verkleinern wird der Flächenmittelwert gebildet, beim Vergrößern
    bilinear interpoliert. Transparente Ränder färben nicht ein.

Beispiele:

    SharpSixel encode foto.jpg --fit
    SharpSixel encode foto.jpg --width 640
    SharpSixel encode icon.png --width 128 --upscale
    SharpSixel encode foto.jpg --fit --width 400 -o klein.six

7.4  Transparenz
~~~~~~~~~~~~~~~~
Beim Enkodieren gelten Pixel mit einem Alphawert unter 128 als transparent.
Enthält das Bild solche Pixel, kennzeichnet SharpSixel dies im Sixel-Kopf
(Parameter P2 = 1), und das Terminal lässt diese Stellen unverändert
(Hintergrund scheint durch).

Beim Dekodieren gilt: Ist im Kopf P2 = 1 gesetzt, bleiben nicht gezeichnete
Pixel transparent (PNG/BMP mit Alpha). Sonst werden sie mit dem Hintergrund
(schwarz) gefüllt. JPEG unterstützt keine Transparenz.

7.5  Anzeige im Terminal (--show)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    --show            zeigt das erzeugte Sixel im Terminal an

Nur bei "encode". Die Standardausgabe muss ein Terminal sein (nicht in eine
Datei oder Pipe umgeleitet); sonst bricht das Programm mit einer Fehlermeldung
ab. Nach dem Bild wird ein Zeilenumbruch ausgegeben, damit der Prompt in einer
neuen Zeile steht.

Verhalten je nach Angabe:

  Eingabe, kein -o, --show     Das Sixel wird nur im Terminal angezeigt, es wird
                               nichts gespeichert.
  Eingabe, -o <Datei>, --show  Das Sixel wird in die Datei gespeichert und
                               zusätzlich im Terminal angezeigt.
  Eingabe, -o <Datei>          Nur speichern, keine Anzeige.
  Sixel-Datei als Eingabe      Die Datei wird geladen und als Terminal-Sixel
  (z. B. foto.six), --show     ausgegeben. Ohne Größen-, Farb- oder Dithering-
                               Option geschieht das unverändert (Byte für
                               Byte); mit -o wird eine identische Kopie
                               gespeichert.

Eine Sixel-Datei wird erkannt, wenn sie mit der Steuersequenz ESC P ... q
beginnt (so wie SharpSixel sie erzeugt). Dateien ohne diesen Kopf, zum Beispiel
einfache Textdateien, gelten nicht als Sixel und werden mit einer Fehlermeldung
abgelehnt, wenn sie auch kein lesbares Bild sind.

Vor der Anzeige fragt das Programm das Terminal, ob es Sixel unterstützt (siehe
"check"). Meldet es das nicht, erscheint auf der Fehlerausgabe eine Warnung; das
Bild wird trotzdem ausgegeben. Wird die Eingabe von der Standardeingabe gelesen
("-"), entfällt diese Abfrage.

--show lässt sich mit allen Optionen von "encode" kombinieren:

    SharpSixel encode foto.jpg --show --fit
    SharpSixel encode foto.jpg --show --width 600 --colors 32 --dither bayer
    SharpSixel encode foto.jpg --show -o foto.six
    SharpSixel encode foto.six --show
    SharpSixel encode foto.six --show --width 400    (dekodiert, skaliert, zeigt an)


8. EINGABE UND AUSGABE ÜBER DATEIEN UND PIPES
---------------------------------------------
"-" steht als Eingabe für die Standardeingabe. Bei "encode" schreibt das
Programm ohne -o (oder mit -o -) auf die Standardausgabe; "decode" braucht
immer eine Datei mit -o. Da Bilder Binärdaten sind, sollte die Umleitung in
cmd.exe mit "<" und ">" erfolgen.
Die PowerShell-Pipe ("|") wandelt Binärdaten in Text und beschädigt sie.

    cmd /c "SharpSixel encode - < foto.bmp"
    cmd /c "SharpSixel encode - --width 400 < foto.bmp > foto.six"
    cmd /c "SharpSixel decode - -o foto.png < foto.six"

Fehlermeldungen und Warnungen gehen auf die Fehlerausgabe (stderr), Bilddaten
auf die Standardausgabe (stdout) bzw. in die mit -o angegebene Datei.


9. RÜCKGABEWERTE (EXIT-CODES)
-----------------------------
    0   Erfolg (bei "check": Terminal unterstützt Sixel)
    1   Laufzeitfehler (Datei nicht gefunden, ungültige oder nicht
        unterstützte Bilddaten, keine Sixel-Daten, Größenlimit
        überschritten, --fit/--show ohne Terminal) bzw. bei "check": nicht
        unterstützt oder keine Antwort
    2   Falsche Bedienung (unbekannter Befehl oder unbekannte Option,
        fehlender Wert, ungültige Zahl, fehlendes -o bei "decode", --show bei "decode")

Fehler werden als Zeile "error: <Meldung>" auf der Fehlerausgabe ausgegeben.


10. TERMINALS MIT SIXEL-UNTERSTÜTZUNG
-------------------------------------
Sixel-fähig sind unter anderem (Stand allgemeiner Kenntnis; bitte die
jeweilige Version prüfen und im Zweifel "SharpSixel check" ausführen):

  * Windows Terminal (ab Version 1.22)
  * xterm (mit Option -ti vt340 bzw. aktivierter Sixel-Unterstützung)
  * WezTerm, foot, mlterm, Contour, iTerm2
  * weitere Terminals und Multiplexer je nach Version und Konfiguration

Nicht sixel-fähig sind u. a. die klassische Windows-Konsole (conhost) und
viele ältere Terminalprogramme. Dort erscheinen statt des Bildes Textzeichen.


11. FEHLERBEHEBUNG
------------------
Es erscheint Text/Zeichensalat statt eines Bildes
    Das Terminal kann kein Sixel. "SharpSixel check" ausführen, ein
    sixel-fähiges Terminal verwenden oder mit -o eine Datei erzeugen.

"check" meldet "not supported", obwohl das Terminal Sixel kann
    Ein-/Ausgabe dürfen nicht umgeleitet sein. Läuft das Programm in einem
    Multiplexer oder über SSH, muss auch dieser Sixel durchreichen/kennen.

Das Bild ist riesig, scrollt weg oder baut sich langsam auf
    Mit --fit oder --width/--height verkleinern, ggf. mit --colors weniger
    Farben verwenden.

Farbverläufe sehen gestuft aus
    --colors erhöhen oder --dither bayer / --dither floyd verwenden.

Warnung "terminal did not report its pixel size"
    Das Terminal beantwortet die Größenabfrage nicht. Die Größe wird aus
    der Zeichenanzahl geschätzt (10 × 20 Pixel pro Zelle). Für genaue Werte
    --width und --height direkt angeben.

"--fit needs a terminal on stdin and stdout"
    Die Ausgabe ist umgeleitet oder die Eingabe kommt aus einer Pipe.
    --width/--height verwenden.

"--show needs standard output to be a terminal"
    Die Ausgabe ist in eine Datei oder Pipe umgeleitet. Ohne Umleitung
    ausführen oder auf --show verzichten und mit -o speichern.

Warnung "the terminal does not report sixel support" bei --show
    Das Terminal hat auf die Abfrage nicht mit Sixel-Unterstützung geantwortet.
    Das Bild wird trotzdem ausgegeben; erscheint nur Zeichensalat, siehe oben.

Fehler beim Entpacken der nativen Bibliothek / Virenscanner meldet etwas
    Die Exe entpackt SkiaSharp nach %TEMP%\.net. Ist dieser Ordner gesperrt,
    mit der Umgebungsvariable DOTNET_BUNDLE_EXTRACT_BASE_DIR einen anderen
    beschreibbaren Ordner festlegen.

"error: The sixel image exceeds the configured size limits."
    Die Sixel-Daten beschreiben ein Bild größer als 16384 × 16384 Pixel
    bzw. mehr als 64 Mio. Pixel (Schutz vor defekten oder bösartigen Daten).

"error: The data is not a supported image format."
    Die Eingabedatei ist weder BMP noch ein von SkiaSharp lesbares Format
    oder ist beschädigt. RLE-komprimierte BMP-Dateien werden nicht
    unterstützt.


12. VERWENDUNG ALS .NET-BIBLIOTHEK (NUGET)
------------------------------------------
Die Pakete liegen im Ordner "nuget". Zum Einbinden den Ordner als lokale
NuGet-Quelle eintragen, zum Beispiel in einer nuget.config neben der
Projektdatei:

    <configuration>
      <packageSources>
        <add key="local" value="C:\Pfad\zu\publish\nuget" />
      </packageSources>
    </configuration>

und danach:

    dotnet add package SharpSixel
    dotnet add package SharpSixel.Skia      (nur für PNG/JPEG)

Zielplattformen: .NET Standard 2.0 und .NET 10. SharpSixel hat keine
Abhängigkeiten; SharpSixel.Skia benötigt SkiaSharp (MIT-Lizenz).

Beispiel: BMP laden, enkodieren, wieder dekodieren

    using SharpSixel;

    SixelImage image = BmpCodec.Decode(File.ReadAllBytes("foto.bmp"));

    var options = new SixelEncoderOptions
    {
        MaxColors = 64,
        Dithering = SixelDithering.Ordered,   // None, FloydSteinberg, Ordered
    };
    string sixel = new SixelEncoder(options).Encode(image);
    Console.Write(sixel);                      // im Sixel-Terminal anzeigen

    SixelImage back = new SixelDecoder().Decode(sixel);
    File.WriteAllBytes("ergebnis.bmp", BmpCodec.Encode(back));

Beispiel: PNG/JPEG über SharpSixel.Skia, Bild verkleinern

    using SharpSixel;
    using SharpSixel.Skia;
    using SkiaSharp;

    SixelImage image = SkiaImageCodec.Decode(File.ReadAllBytes("foto.png"));
    SixelImage small = ImageScaler.Fit(image, maxWidth: 800, maxHeight: 600);
    byte[] png = SkiaImageCodec.Encode(small, SKEncodedImageFormat.Png);

Wichtige Typen:

  SixelImage            Pixelpuffer (RGBA, 8 Bit pro Kanal, nicht vormultipliziert)
  SixelColor            Farbwert (R, G, B, A)
  SixelEncoder          Bild -> Sixel (Encode, EncodeToBytes, Ausgabe in TextWriter)
  SixelEncoderOptions   MaxColors, AlphaThreshold, EmitRasterAttributes, Dithering
  SixelDecoder          Sixel (string, byte[], Stream) -> Bild
  SixelDecoderOptions   Größenlimits, Anzahl Farbregister, Hintergrundfarbe
  BmpCodec              BMP lesen und schreiben
  ImageScaler           Resize, Fit, FitSize
  SixelTerminal         Terminal-Abfrage (Sixel-Unterstützung, Pixelgröße)
  SixelException        Fehler bei ungültigen Daten oder Limits

Die Terminal-Abfrage (SixelTerminal.Detect / Query) ist unabhängig von der
Ein-/Ausgabe: Sie erwartet einen TextWriter und eine Funktion, die das
nächste Eingabezeichen liefert (oder -1 bei Zeitüberschreitung). Das
Terminal muss dafür im Raw-Modus laufen; die Umsetzung dafür liegt beim
Aufrufer (SharpSixel.exe enthält eine fertige Variante: unter Windows getestet,
der Unix-Pfad ist implementiert, aber noch nicht auf Linux/macOS getestet).


13. TECHNISCHE DETAILS UND GRENZEN
----------------------------------
Unterstützte Sixel-Befehle (DECSIXEL):
  * Steuersequenz ESC P P1;P2;P3 q ... ESC \ (7-Bit und 8-Bit-Variante)
  * Raster Attributes  " Pan;Pad;Ph;Pv   (Seitenverhältnis und Bildgröße)
  * Farbdefinition und -auswahl  # Pc;Pu;Px;Py;Pz  (RGB und HLS, Werte in Prozent)
  * Wiederholung  ! Pn <Zeichen>
  * Zeilenanfang $ und nächstes Sixel-Band -
  * Hintergrundwahl über P2 (0/2 = Hintergrundfarbe, 1 = transparent)

Verhalten des Decoders:
  * HLS-Farben folgen der DEC-Konvention (Blau = 0°, Rot = 120°, Grün = 240°).
  * Ohne Farbauswahl wird mit Weiß gezeichnet. Nicht definierte Register
    0-15 haben die VT340-Standardfarben, höhere Register sind schwarz.
    Registernummern über der Registerzahl (Standard 256) laufen modulo um.
  * Unbekannte Zeichen und Zeilenumbrüche werden ignoriert.
  * Die Bildhöhe richtet sich nach den Raster Attributes bzw. den Daten
    (Vielfaches von 6 Zeilen); überzählige Zeilen im letzten Band werden
    abgeschnitten, wenn die Höhe deklariert ist.
  * Das Pixel-Seitenverhältnis aus P1 bzw. den Raster Attributes wird im
    Bildobjekt gespeichert, von der Kommandozeile beim Umwandeln aber nicht
    angewendet.
  * Limits (Schutz vor Speicherverbrauch): Breite und Höhe je 16384 Pixel,
    insgesamt 64 Mio. Pixel. In der Bibliothek über SixelDecoderOptions
    einstellbar.

Verhalten des Encoders:
  * Höchstens 256 Farben (Farbregister 0-255), Farbwerte in Prozent.
  * Lauflängenkodierung ab vier gleichen Zeichen, nur 7-Bit-ASCII in der
    Ausgabe (ESC P ... ESC \).
  * Die Ausgabe enthält Raster Attributes mit Seitenverhältnis und Bildgröße.
  * Keine Animationen, keine Interlace-Verfahren.

BMP: lesen 1/4/8 Bit (Palette), 16/24/32 Bit (auch Bitfields, Alpha über
Bitfields), oben/unten gespeichert; RLE-Kompression wird nicht unterstützt.
Schreiben: 24 Bit, bei Transparenz 32 Bit (BGRA, V4-Header).


14. AUS DEN QUELLEN BAUEN
-------------------------
Voraussetzung: .NET SDK 10.

    dotnet test                 alle Tests ausführen
    ./publish.ps1               SharpSixel.exe, README.txt und NuGet-Pakete
                                im Ordner "publish" erzeugen (nur Release)
    ./publish.ps1 -Version 0.2.0   dasselbe mit anderer Versionsnummer

Pack und Publish sind ausschließlich als Release-Build vorgesehen; ein
Aufruf mit "-c Debug" wird mit einer Fehlermeldung abgelehnt.


15. LIZENZ
----------
SharpSixel steht unter der MIT-Lizenz (siehe Datei LICENSE im Quellordner).

Copyright (c) 2026 Alexander Feuster

Verwendete Komponenten: SkiaSharp (MIT-Lizenz) für PNG/JPEG-Unterstützung
in SharpSixel.exe und SharpSixel.Skia.


16. HINWEIS ZUR NUTZUNG VON KI (FREIWILLIG)
-------------------------------------------
Teile dieses Projekts (Quellcode, Tests und Dokumentation) wurden mit
Unterstützung des KI-Sprachmodells Claude von Anthropic erstellt.

  Eingesetztes Modell: Claude Sonnet 5.5

Es wurde ausschließlich diese eine Modellversion eingesetzt.
Für Inhalt, Prüfung und Veröffentlichung ist der Autor verantwortlich. Dieser
Hinweis ist freiwillig, dient der Transparenz und schränkt die MIT-Lizenz nicht
ein.
