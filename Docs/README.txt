================================================================================
 SharpSixel  -  Sixel-Grafiken enkodieren und dekodieren
================================================================================

 Version : 0.1.0  (aktuelle Version des Programms: SharpSixel --version)
 Autor   : Alexander Feuster
 Lizenz  : MIT


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
       6.3  check   - Sixel-Unterstuetzung des Terminals pruefen
  7. Optionen im Detail
       7.1  Farbanzahl (--colors)
       7.2  Dithering (--dither)
       7.3  Bildgroesse anpassen (--width, --height, --fit, --upscale)
       7.4  Transparenz
  8. Eingabe und Ausgabe ueber Dateien und Pipes
  9. Rueckgabewerte (Exit-Codes)
 10. Terminals mit Sixel-Unterstuetzung
 11. Fehlerbehebung
 12. Verwendung als .NET-Bibliothek (NuGet)
 13. Technische Details und Grenzen
 14. Aus den Quellen bauen
 15. Lizenz


1. WAS IST SHARPSIXEL?
----------------------
Sixel ist ein Grafikformat aus der Zeit der DEC-Terminals (VT240/VT340). Es
beschreibt ein Bild als Folge von Steuerzeichen, die ein Terminal direkt als
Pixelgrafik darstellen kann. Moderne Terminals (siehe Abschnitt 10) koennen
Sixel-Bilder anzeigen, ohne ein externes Programm zu starten.

SharpSixel besteht aus:

  * SharpSixel.exe   Kommandozeilenprogramm: Bilder (BMP, PNG, JPEG) in Sixel
                     umwandeln und Sixel-Daten wieder in Bilddateien.
  * SharpSixel       .NET-Bibliothek (NuGet) ohne Abhaengigkeiten: Encoder,
                     Decoder, BMP-Unterstuetzung, Skalierung, Terminal-Abfrage.
  * SharpSixel.Skia  Ergaenzungspaket (NuGet): PNG/JPEG ueber SkiaSharp.


2. LIEFERUMFANG
---------------
Nach dem Publish (./publish.ps1) liegen im Ordner "publish":

  SharpSixel.exe          das Programm (eine einzelne Datei)
  README.txt              diese Anleitung
  nuget\SharpSixel.<Version>.nupkg
  nuget\SharpSixel.Skia.<Version>.nupkg


3. VORAUSSETZUNGEN
------------------
  * Windows 10/11, 64 Bit (x64)
  * Keine .NET-Installation noetig (das Programm bringt die Laufzeit mit)
  * Zum Anzeigen von Bildern: ein Terminal mit Sixel-Unterstuetzung
    (z. B. Windows Terminal ab Version 1.22, siehe Abschnitt 10)
  * Zum Erzeugen und Umwandeln von Dateien ist kein Sixel-Terminal noetig


4. INSTALLATION
---------------
Es gibt keinen Installer. SharpSixel.exe in einen beliebigen Ordner kopieren.
Optional den Ordner zur Umgebungsvariable PATH hinzufuegen, dann ist der Befehl
"SharpSixel" in jeder Konsole verfuegbar.

Erster Start: Die Exe entpackt eine native Bibliothek (SkiaSharp) in einen
temporaeren Ordner (%TEMP%\.net). Das dauert beim ersten Mal einen Augenblick
(etwa 150 ms), danach startet das Programm sofort. Soll ein anderer Ordner
verwendet werden, kann die Umgebungsvariable DOTNET_BUNDLE_EXTRACT_BASE_DIR
gesetzt werden.

Deinstallation: Exe loeschen, optional den Ordner %TEMP%\.net\SharpSixel
entfernen.


5. SCHNELLSTART
---------------
Pruefen, ob das Terminal Sixel kann:

    SharpSixel check

Ein Bild im Terminal anzeigen, passend zur Fenstergroesse:

    SharpSixel encode foto.jpg --fit

Ein Bild verkleinern, auf 64 Farben reduzieren und als Datei speichern:

    SharpSixel encode foto.png --width 800 --colors 64 --dither bayer -o foto.six

Eine gespeicherte Sixel-Datei anzeigen (cmd.exe):

    type foto.six

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

    SharpSixel encode <Eingabe> [-o <Datei>] [--colors <2-256>]
                      [--dither [floyd|bayer|none]]
                      [--width <px>] [--height <px>] [--fit] [--upscale]

  <Eingabe>   Pfad zu einer Bilddatei oder "-" fuer die Standardeingabe.
              Unterstuetzt: BMP (1/4/8/16/24/32 Bit, unkomprimiert), PNG, JPEG
              sowie weitere von SkiaSharp lesbare Formate.
  -o <Datei>  Ausgabedatei. Ohne -o werden die Sixel-Daten auf die
              Standardausgabe geschrieben. Bei einem Sixel-Terminal erscheint
              das Bild dann direkt im Fenster.

6.2  decode  -  Sixel in Bilddatei umwandeln
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Aufruf:

    SharpSixel decode <Eingabe> -o <Datei>

  <Eingabe>   Pfad zu einer Sixel-Datei oder "-" fuer die Standardeingabe.
              Die Daten duerfen mit der Steuersequenz ESC P ... q beginnen
              (7-Bit oder 8-Bit) und mit ESC \ enden; reine Sixel-Daten ohne
              Kopf werden ebenfalls akzeptiert.
  -o <Datei>  Pflicht. Das Ausgabeformat ergibt sich aus der Endung:
                .bmp           BMP (24 Bit, bei Transparenz 32 Bit mit Alpha)
                .png           PNG (verlustfrei, mit Transparenz)
                .jpg / .jpeg   JPEG (Qualitaet 90, keine Transparenz)

6.3  check  -  Sixel-Unterstuetzung des Terminals pruefen
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Aufruf:

    SharpSixel check

Das Programm fragt das Terminal per "Primary Device Attributes" (Steuersequenz
ESC [ c). Meldet das Terminal das Merkmal 4, kann es Sixel darstellen.
Ausgabe: "Terminal supports sixel graphics." (Exit-Code 0) oder
"Terminal does not support sixel graphics (or did not answer)." (Exit-Code 1).
Antwortet das Terminal nicht innerhalb von etwa einer Sekunde oder sind
Ein-/Ausgabe umgeleitet (Pipe, Datei), gilt das Ergebnis als "nicht
unterstuetzt".


7. OPTIONEN IM DETAIL
---------------------

7.1  Farbanzahl (--colors)
~~~~~~~~~~~~~~~~~~~~~~~~~~
    --colors <n>      n = 2 bis 256, Standard: 256

Sixel arbeitet mit einer Farbpalette. Hat das Bild hoechstens n verschiedene
Farben, wird die Palette exakt uebernommen. Hat es mehr, wird die Palette per
Median-Cut-Verfahren auf n Farben reduziert.

Weniger Farben bedeuten kleinere Dateien und schnellere Darstellung, aber
sichtbare Farbstufen (Banding). Dagegen hilft Dithering (7.2).

Hinweis zur Genauigkeit: Sixel speichert Farbwerte in Prozent (0-100). Beim
Umrechnen von 8 Bit auf Prozent entsteht ein Rundungsfehler von hoechstens etwa
1,3 Stufen (von 255). Farben mit Prozentwerten 0, 20, 40, 60, 80 und 100 sind
verlustfrei.

7.2  Dithering (--dither)
~~~~~~~~~~~~~~~~~~~~~~~~~
    --dither              Floyd-Steinberg (Kurzform)
    --dither floyd        Floyd-Steinberg (auch: floyd-steinberg)
    --dither bayer        geordnetes Dithering mit 8x8-Bayer-Matrix (auch: ordered)
    --dither none         kein Dithering (Standard)

Dithering mischt benachbarte Pixel aus Paletten-Farben, damit der Eindruck
zusaetzlicher Farben entsteht. Es wirkt nur, wenn die Farben tatsaechlich
reduziert werden muessen; Bilder, die in die Palette passen, bleiben
unveraendert.

  floyd   Fehlerdiffusion. Sehr natuerlicher Eindruck bei Fotos, etwas
          langsamer, groessere Ausgabedatei.
  bayer   Regelmaessiges Punktraster. Schneller und deterministisch, erzeugt
          meist deutlich kleinere Ausgaben (bei 4K-Testbildern etwa 15-25 %
          kleiner als floyd), das Raster ist bei flaechigen Verlaeufen sichtbar.

Beispiel:  SharpSixel encode foto.jpg --colors 16 --dither bayer

7.3  Bildgroesse anpassen (--width, --height, --fit, --upscale)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Grosse Bilder erzeugen sehr viele Daten und ueberfuellen das Terminal. Mit den
folgenden Optionen wird das Bild vor dem Enkodieren verkleinert. Das
Seitenverhaeltnis bleibt immer erhalten.

    --width <px>      maximale Breite in Pixeln
    --height <px>     maximale Hoehe in Pixeln
    --fit             in den darstellbaren Bereich des Terminals einpassen
    --upscale         kleine Bilder auch vergroessern (bis zum Limit)

Regeln:

  * Ohne --upscale wird nur verkleinert, nie vergroessert.
  * --width und --height duerfen einzeln oder zusammen angegeben werden; das
    Bild wird so skaliert, dass beide Grenzen eingehalten werden.
  * --fit fragt das Terminal nach seiner Groesse in Pixeln. Ist eine Achse per
    --width oder --height vorgegeben, hat diese Vorrang; die andere Achse
    kommt vom Terminal. Von der Terminalhoehe wird eine Textzeile abgezogen,
    damit der Prompt nicht aus dem Fenster scrollt.
  * --fit braucht ein Terminal an Standardeingabe und Standardausgabe. Bei
    umgeleiteter Ausgabe (Datei, Pipe) bricht das Programm mit einem Hinweis
    ab; dann --width/--height verwenden.
  * Meldet das Terminal seine Pixelgroesse nicht, schaetzt das Programm
    10 x 20 Pixel pro Zeichenzelle und gibt auf der Fehlerausgabe die Warnung
    "terminal did not report its pixel size" aus.
  * Beim Verkleinern wird der Flaechenmittelwert gebildet, beim Vergroessern
    bilinear interpoliert. Transparente Raender faerben nicht ein.

Beispiele:

    SharpSixel encode foto.jpg --fit
    SharpSixel encode foto.jpg --width 640
    SharpSixel encode icon.png --width 128 --upscale
    SharpSixel encode foto.jpg --fit --width 400 -o klein.six

7.4  Transparenz
~~~~~~~~~~~~~~~~
Beim Enkodieren gelten Pixel mit einem Alphawert unter 128 als transparent.
Enthaelt das Bild solche Pixel, kennzeichnet SharpSixel dies im Sixel-Kopf
(Parameter P2 = 1), und das Terminal laesst diese Stellen unveraendert
(Hintergrund scheint durch).

Beim Dekodieren gilt: Ist im Kopf P2 = 1 gesetzt, bleiben nicht gezeichnete
Pixel transparent (PNG/BMP mit Alpha). Sonst werden sie mit dem Hintergrund
(schwarz) gefuellt. JPEG unterstuetzt keine Transparenz.


8. EINGABE UND AUSGABE UEBER DATEIEN UND PIPES
----------------------------------------------
"-" steht als Eingabe fuer die Standardeingabe. Bei "encode" schreibt ohne -o (oder mit -o -)
das Programm auf die Standardausgabe; "decode" braucht immer eine Datei mit -o. Da Bilder
Binaerdaten sind, sollte die Umleitung in cmd.exe mit "<" und ">" erfolgen.
Die PowerShell-Pipe ("|") wandelt Binaerdaten in Text und beschaedigt sie.

    cmd /c "SharpSixel encode - < foto.bmp"
    cmd /c "SharpSixel encode - --width 400 < foto.bmp > foto.six"
    cmd /c "SharpSixel decode - -o foto.png < foto.six"

Fehlermeldungen und Warnungen gehen auf die Fehlerausgabe (stderr), Bilddaten
auf die Standardausgabe (stdout) bzw. in die mit -o angegebene Datei.


9. RUECKGABEWERTE (EXIT-CODES)
------------------------------
    0   Erfolg (bei "check": Terminal unterstuetzt Sixel)
    1   Laufzeitfehler (Datei nicht gefunden, ungueltige oder nicht
        unterstuetzte Bilddaten, keine Sixel-Daten, Groessenlimit
        ueberschritten, --fit ohne Terminal) bzw. bei "check": nicht
        unterstuetzt oder keine Antwort
    2   Falsche Bedienung (unbekannter Befehl oder unbekannte Option,
        fehlender Wert, ungueltige Zahl, fehlendes -o bei "decode")

Fehler werden als Zeile "error: <Meldung>" auf der Fehlerausgabe ausgegeben.


10. TERMINALS MIT SIXEL-UNTERSTUETZUNG
--------------------------------------
Sixel-faehig sind unter anderem (Stand allgemeiner Kenntnis; bitte die
jeweilige Version pruefen und im Zweifel "SharpSixel check" ausfuehren):

  * Windows Terminal (ab Version 1.22)
  * xterm (mit Option -ti vt340 bzw. aktivierter Sixel-Unterstuetzung)
  * WezTerm, foot, mlterm, Contour, iTerm2
  * weitere Terminals und Multiplexer je nach Version und Konfiguration

Nicht sixel-faehig sind u. a. die klassische Windows-Konsole (conhost) und
viele aeltere Terminalprogramme. Dort erscheinen statt des Bildes Textzeichen.


11. FEHLERBEHEBUNG
------------------
Es erscheint Text/Zeichensalat statt eines Bildes
    Das Terminal kann kein Sixel. "SharpSixel check" ausfuehren, ein
    sixel-faehiges Terminal verwenden oder mit -o eine Datei erzeugen.

"check" meldet "not supported", obwohl das Terminal Sixel kann
    Ein-/Ausgabe duerfen nicht umgeleitet sein. Laeuft das Programm in einem
    Multiplexer oder ueber SSH, muss auch dieser Sixel durchreichen/kennen.

Das Bild ist riesig, scrollt weg oder baut sich langsam auf
    Mit --fit oder --width/--height verkleinern, ggf. mit --colors weniger
    Farben verwenden.

Farbverlaeufe sehen gestuft aus
    --colors erhoehen oder --dither bayer / --dither floyd verwenden.

Warnung "terminal did not report its pixel size"
    Das Terminal beantwortet die Groessenabfrage nicht. Die Groesse wird aus
    der Zeichenanzahl geschaetzt (10 x 20 Pixel pro Zelle). Fuer genaue Werte
    --width und --height direkt angeben.

"--fit needs a terminal on stdin and stdout"
    Die Ausgabe ist umgeleitet oder die Eingabe kommt aus einer Pipe.
    --width/--height verwenden.

Fehler beim Entpacken der nativen Bibliothek / Virenscanner meldet etwas
    Die Exe entpackt SkiaSharp nach %TEMP%\.net. Ist dieser Ordner gesperrt,
    mit der Umgebungsvariable DOTNET_BUNDLE_EXTRACT_BASE_DIR einen anderen
    beschreibbaren Ordner festlegen.

"error: The sixel image exceeds the configured size limits."
    Die Sixel-Daten beschreiben ein Bild groesser als 16384 x 16384 Pixel
    bzw. mehr als 64 Mio. Pixel (Schutz vor defekten oder boesartigen Daten).

"error: The data is not a supported image format."
    Die Eingabedatei ist weder BMP noch ein von SkiaSharp lesbares Format
    oder ist beschaedigt. RLE-komprimierte BMP-Dateien werden nicht
    unterstuetzt.


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
    dotnet add package SharpSixel.Skia      (nur fuer PNG/JPEG)

Zielplattformen: .NET Standard 2.0 und .NET 10. SharpSixel hat keine
Abhaengigkeiten; SharpSixel.Skia benoetigt SkiaSharp (MIT-Lizenz).

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
    File.WriteAllBytes("zurueck.bmp", BmpCodec.Encode(back));

Beispiel: PNG/JPEG ueber SharpSixel.Skia, Bild verkleinern

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
  SixelDecoderOptions   Groessenlimits, Anzahl Farbregister, Hintergrundfarbe
  BmpCodec              BMP lesen und schreiben
  ImageScaler           Resize, Fit, FitSize
  SixelTerminal         Terminal-Abfrage (Sixel-Unterstuetzung, Pixelgroesse)
  SixelException        Fehler bei ungueltigen Daten oder Limits

Die Terminal-Abfrage (SixelTerminal.Detect / Query) ist unabhaengig von der
Ein-/Ausgabe: Sie erwartet einen TextWriter und eine Funktion, die das
naechste Eingabezeichen liefert (oder -1 bei Zeitueberschreitung). Das
Terminal muss dafuer im Raw-Modus laufen; die Umsetzung dafuer liegt beim
Aufrufer (SharpSixel.exe enthaelt eine fertige Variante: unter Windows getestet,\nder Unix-Pfad ist implementiert, aber noch nicht auf Linux/macOS getestet).


13. TECHNISCHE DETAILS UND GRENZEN
----------------------------------
Unterstuetzte Sixel-Befehle (DECSIXEL):
  * Steuersequenz ESC P P1;P2;P3 q ... ESC \ (7-Bit und 8-Bit-Variante)
  * Raster Attributes  " Pan;Pad;Ph;Pv   (Seitenverhaeltnis und Bildgroesse)
  * Farbdefinition und -auswahl  # Pc;Pu;Px;Py;Pz  (RGB und HLS, Werte in Prozent)
  * Wiederholung  ! Pn <Zeichen>
  * Zeilenanfang $ und naechstes Sixel-Band -
  * Hintergrundwahl ueber P2 (0/2 = Hintergrundfarbe, 1 = transparent)

Verhalten des Decoders:
  * HLS-Farben folgen der DEC-Konvention (Blau = 0 Grad, Rot = 120 Grad,
    Gruen = 240 Grad).
  * Ohne Farbauswahl wird mit Weiss gezeichnet. Nicht definierte Register
    0-15 haben die VT340-Standardfarben, hoehere Register sind schwarz.
    Registernummern ueber der Registerzahl (Standard 256) laufen modulo um.
  * Unbekannte Zeichen und Zeilenumbrueche werden ignoriert.
  * Die Bildhoehe richtet sich nach den Raster Attributes bzw. den Daten
    (Vielfaches von 6 Zeilen); ueberzaehlige Zeilen im letzten Band werden
    abgeschnitten, wenn die Hoehe deklariert ist.
  * Das Pixel-Seitenverhaeltnis aus P1 bzw. den Raster Attributes wird im
    Bildobjekt gespeichert, von der Kommandozeile beim Umwandeln aber nicht
    angewendet.
  * Limits (Schutz vor Speicherverbrauch): Breite und Hoehe je 16384 Pixel,
    insgesamt 64 Mio. Pixel. In der Bibliothek ueber SixelDecoderOptions
    einstellbar.

Verhalten des Encoders:
  * Hoechstens 256 Farben (Farbregister 0-255), Farbwerte in Prozent.
  * Lauflaengenkodierung ab vier gleichen Zeichen, nur 7-Bit-ASCII in der
    Ausgabe (ESC P ... ESC \).
  * Die Ausgabe enthaelt Raster Attributes mit Seitenverhaeltnis und Bildgroesse.
  * Keine Animationen, keine Interlace-Verfahren.

BMP: lesen 1/4/8 Bit (Palette), 16/24/32 Bit (auch Bitfields, Alpha ueber
Bitfields), oben/unten gespeichert; RLE-Kompression wird nicht unterstuetzt.
Schreiben: 24 Bit, bei Transparenz 32 Bit (BGRA, V4-Header).


14. AUS DEN QUELLEN BAUEN
-------------------------
Voraussetzung: .NET SDK 10.

    dotnet test                 alle Tests ausfuehren
    ./publish.ps1               SharpSixel.exe, README.txt und NuGet-Pakete
                                im Ordner "publish" erzeugen (nur Release)
    ./publish.ps1 -Version 0.2.0   dasselbe mit anderer Versionsnummer

Pack und Publish sind ausschliesslich als Release-Build vorgesehen; ein
Aufruf mit "-c Debug" wird mit einer Fehlermeldung abgelehnt.


15. LIZENZ
----------
SharpSixel steht unter der MIT-Lizenz (siehe Datei LICENSE im Quellordner).

Copyright (c) 2026 Alexander Feuster

Verwendete Komponenten: SkiaSharp (MIT-Lizenz) fuer PNG/JPEG-Unterstuetzung
in SharpSixel.exe und SharpSixel.Skia.
