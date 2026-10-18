================================================================================
 SharpSixel  -  Encode and decode sixel graphics
================================================================================

 Version : 0.1.0  (current version of the program: SharpSixel --version)
 Author  : Alexander Feuster
 License : MIT
 Deutsche Version: README.txt


CONTENTS
--------
  1. What is SharpSixel?
  2. Package contents
  3. Requirements
  4. Installation
  5. Quick start
  6. Commands
       6.1  encode  - convert an image to sixel
       6.2  decode  - convert sixel to an image file
       6.3  check   - test whether the terminal supports sixel
  7. Options in detail
       7.1  Number of colors (--colors)
       7.2  Dithering (--dither)
       7.3  Resizing the image (--width, --height, --fit, --upscale)
       7.4  Transparency
       7.5  Display in the terminal (--show)
  8. Input and output via files and pipes
  9. Exit codes
 10. Terminals with sixel support
 11. Troubleshooting
 12. Using the .NET library (NuGet)
 13. Technical details and limits
 14. Building from source
 15. License
 16. Note on the use of AI (voluntary)


1. WHAT IS SHARPSIXEL?
----------------------
Sixel is a graphics format from the days of the DEC terminals (VT240/VT340). It
describes an image as a sequence of control characters that a terminal can
display directly as pixel graphics. Modern terminals (see section 10) can show
sixel images without starting an external program.

SharpSixel consists of:

  * SharpSixel.exe   Command line program: converts images (BMP, PNG, JPEG) to
                     sixel and sixel data back to image files.
  * SharpSixel       .NET library (NuGet) without dependencies: encoder,
                     decoder, BMP support, scaling, terminal query.
  * SharpSixel.Skia  Add-on package (NuGet): PNG/JPEG via SkiaSharp.


2. PACKAGE CONTENTS
-------------------
After publishing (./publish.ps1) the folder "publish" contains:

  SharpSixel.exe          the program (a single file)
  README.txt              user manual (German)
  README.en.txt           this manual (English)
  nuget\SharpSixel.<version>.nupkg
  nuget\SharpSixel.Skia.<version>.nupkg


3. REQUIREMENTS
---------------
  * Windows 10/11, 64 bit (x64)
  * No .NET installation required (the program ships its own runtime)
  * To display images: a terminal with sixel support
    (e.g. Windows Terminal 1.22 or later, see section 10)
  * Creating and converting files does not require a sixel terminal


4. INSTALLATION
---------------
There is no installer. Copy SharpSixel.exe to any folder. Optionally add that
folder to the PATH environment variable; the command "SharpSixel" is then
available in every console.

First start: the exe unpacks a native library (SkiaSharp) into a temporary
folder (%TEMP%\.net). The first start takes a moment (about 150 ms); after that
the program starts immediately. To use a different folder, set the environment
variable DOTNET_BUNDLE_EXTRACT_BASE_DIR.

Uninstall: delete the exe, optionally remove the folder %TEMP%\.net\SharpSixel.


5. QUICK START
--------------
Check whether the terminal supports sixel:

    SharpSixel check

Show an image in the terminal, fitted to the window size:

    SharpSixel encode photo.jpg --show --fit

Show an image and save it as a file at the same time:

    SharpSixel encode photo.jpg --show -o photo.six

Shrink an image, reduce it to 64 colors and save it as a file:

    SharpSixel encode photo.png --width 800 --colors 64 --dither bayer -o photo.six

Display a saved sixel file:

    SharpSixel encode photo.six --show
    (or in cmd.exe: type photo.six)

Convert a sixel file to PNG:

    SharpSixel decode photo.six -o photo.png

Help and version:

    SharpSixel --help
    SharpSixel --version


6. COMMANDS
-----------

6.1  encode  -  convert an image to sixel
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Usage:

    SharpSixel encode <input> [-o <file>] [--show] [--colors <2-256>]
                      [--dither [floyd|bayer|none]]
                      [--width <px>] [--height <px>] [--fit] [--upscale]

  <input>     Path to an image file, or "-" for standard input.
              Supported: BMP (1/4/8/16/24/32 bit, uncompressed), PNG, JPEG and
              further formats SkiaSharp can read. A sixel file (header
              ESC P ... q) is accepted as input too: it is output unchanged
              as long as no size, color or dithering option is given;
              otherwise it is decoded, adjusted and encoded again.
  -o <file>   Output file. Without -o the sixel data is written to standard
              output. In a sixel terminal the image then appears directly in
              the window.
  --show      Displays the sixel in the terminal (details in section 7.5).

6.2  decode  -  convert sixel to an image file
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Usage:

    SharpSixel decode <input> -o <file>

  <input>     Path to a sixel file, or "-" for standard input.
              The data may start with the control sequence ESC P ... q (7-bit
              or 8-bit) and end with ESC \ ; plain sixel data without a header
              is accepted as well.
  -o <file>   Required. The output format is chosen by the file extension:
                .bmp           BMP (24 bit, 32 bit with alpha if transparent)
                .png           PNG (lossless, with transparency)
                .jpg / .jpeg   JPEG (quality 90, no transparency)

6.3  check  -  test whether the terminal supports sixel
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Usage:

    SharpSixel check

The program queries the terminal with "Primary Device Attributes" (control
sequence ESC [ c). If the terminal reports attribute 4, it can display sixel.
Output: "Terminal supports sixel graphics." (exit code 0) or
"Terminal does not support sixel graphics (or did not answer)." (exit code 1).
If the terminal does not answer within about one second, or if input/output is
redirected (pipe, file), the result is "not supported".


7. OPTIONS IN DETAIL
--------------------

7.1  Number of colors (--colors)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    --colors <n>      n = 2 to 256, default: 256

Sixel uses a color palette. If the image has at most n different colors, the
palette is taken over exactly. If it has more, the palette is reduced to n
colors with the median-cut method.

Fewer colors mean smaller files and faster display, but visible color steps
(banding). Dithering (7.2) helps against that.

Note on accuracy: sixel stores color values as percentages (0-100). Converting
from 8 bit to percent causes a rounding error of at most about 1.3 levels (out
of 255). Colors whose percentages are 0, 20, 40, 60, 80 and 100 are lossless.

7.2  Dithering (--dither)
~~~~~~~~~~~~~~~~~~~~~~~~~
    --dither              Floyd-Steinberg (short form)
    --dither floyd        Floyd-Steinberg (also: floyd-steinberg)
    --dither bayer        ordered dithering with an 8x8 Bayer matrix (also: ordered)
    --dither none         no dithering (default)

Dithering mixes neighboring pixels from palette colors to create the
impression of additional colors. It only takes effect if colors actually have
to be reduced; images that fit into the palette stay unchanged.

  floyd   Error diffusion. Very natural look for photos, somewhat slower,
          larger output.
  bayer   Regular dot pattern. Faster and deterministic, usually produces
          clearly smaller output (about 15-25 % smaller than floyd for 4K test
          images); the pattern is visible in smooth gradients.

Example:  SharpSixel encode photo.jpg --colors 16 --dither bayer

7.3  Resizing the image (--width, --height, --fit, --upscale)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Large images produce a lot of data and flood the terminal. The following
options shrink the image before encoding. The aspect ratio is always kept.

    --width <px>      maximum width in pixels
    --height <px>     maximum height in pixels
    --fit             fit into the drawable area of the terminal
    --upscale         also enlarge small images (up to the limit)

Rules:

  * Without --upscale the image is only shrunk, never enlarged.
  * --width and --height may be given individually or together; the image is
    scaled so that both limits are respected.
  * --fit asks the terminal for its size in pixels. If one axis is given via
    --width or --height, that axis takes precedence; the other axis comes from
    the terminal. One text row is subtracted from the terminal height so that
    the prompt does not scroll out of the window.
  * --fit needs a terminal on standard input and standard output. If the output
    is redirected (file, pipe), the program stops with a hint; use
    --width/--height instead.
  * If the terminal does not report its pixel size, the program estimates
    10 x 20 pixels per character cell and prints the warning
    "terminal did not report its pixel size" on the error output.
  * Shrinking uses area averaging, enlarging uses bilinear interpolation.
    Transparent edges do not bleed color.

Examples:

    SharpSixel encode photo.jpg --fit
    SharpSixel encode photo.jpg --width 640
    SharpSixel encode icon.png --width 128 --upscale
    SharpSixel encode photo.jpg --fit --width 400 -o small.six

7.4  Transparency
~~~~~~~~~~~~~~~~~
When encoding, pixels with an alpha value below 128 count as transparent. If
the image contains such pixels, SharpSixel marks this in the sixel header
(parameter P2 = 1), and the terminal leaves those positions unchanged (the
background shows through).

When decoding: if P2 = 1 is set in the header, pixels that were not drawn stay
transparent (PNG/BMP with alpha). Otherwise they are filled with the
background (black). JPEG does not support transparency.

7.5  Display in the terminal (--show)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    --show            displays the generated sixel in the terminal

Only for "encode". Standard output must be a terminal (not redirected to a
file or pipe); otherwise the program stops with an error message. After the
image a line break is written so that the prompt starts on a new line.

Behavior depending on the arguments:

  input, no -o, --show         The sixel is only displayed in the terminal,
                               nothing is saved.
  input, -o <file>, --show     The sixel is saved to the file and additionally
                               displayed in the terminal.
  input, -o <file>             Save only, no display.
  sixel file as input          The file is loaded and output as terminal sixel.
  (e.g. photo.six), --show     Without a size, color or dithering option this
                               happens unchanged (byte for byte); with -o an
                               identical copy is saved.

A sixel file is recognized if it starts with the control sequence ESC P ... q
(as SharpSixel produces it). Files without this header, for example plain text
files, do not count as sixel and are rejected with an error message if they are
not a readable image either.

Before displaying, the program asks the terminal whether it supports sixel (see
"check"). If it does not report so, a warning appears on the error output; the
image is still written. If the input is read from standard input ("-"), this
query is skipped.

--show can be combined with all options of "encode":

    SharpSixel encode photo.jpg --show --fit
    SharpSixel encode photo.jpg --show --width 600 --colors 32 --dither bayer
    SharpSixel encode photo.jpg --show -o photo.six
    SharpSixel encode photo.six --show
    SharpSixel encode photo.six --show --width 400    (decodes, scales, displays)


8. INPUT AND OUTPUT VIA FILES AND PIPES
---------------------------------------
"-" stands for standard input. With "encode", the program writes to standard
output when -o is omitted (or given as -o -); "decode" always needs a file via
-o. Because images are binary data, redirection in cmd.exe with "<" and ">"
should be used. The PowerShell pipe ("|") converts binary data to text and
corrupts it.

    cmd /c "SharpSixel encode - < photo.bmp"
    cmd /c "SharpSixel encode - --width 400 < photo.bmp > photo.six"
    cmd /c "SharpSixel decode - -o photo.png < photo.six"

Error messages and warnings go to the error output (stderr), image data goes to
standard output (stdout) or into the file given with -o.


9. EXIT CODES
-------------
    0   Success (for "check": terminal supports sixel)
    1   Runtime error (file not found, invalid or unsupported image data, no
        sixel data, size limit exceeded, --fit/--show without a terminal) or, for
        "check": not supported or no answer
    2   Incorrect usage (unknown command or option, missing value, invalid
        number, missing -o for "decode", --show with "decode")

Errors are printed as a line "error: <message>" on the error output.


10. TERMINALS WITH SIXEL SUPPORT
--------------------------------
Terminals that can display sixel include (general knowledge; please check the
respective version and, when in doubt, run "SharpSixel check"):

  * Windows Terminal (1.22 or later)
  * xterm (with option -ti vt340 or sixel support enabled)
  * WezTerm, foot, mlterm, Contour, iTerm2
  * further terminals and multiplexers depending on version and configuration

Not sixel capable are, among others, the classic Windows console (conhost) and
many older terminal programs. They print text characters instead of the image.


11. TROUBLESHOOTING
-------------------
Text/garbage appears instead of an image
    The terminal does not support sixel. Run "SharpSixel check", use a sixel
    capable terminal, or create a file with -o.

"check" reports "not supported" although the terminal supports sixel
    Input/output must not be redirected. If the program runs inside a
    multiplexer or over SSH, that layer must pass sixel through/understand it.

The image is huge, scrolls away or builds up slowly
    Shrink it with --fit or --width/--height, and if necessary use fewer
    colors with --colors.

Gradients look stepped
    Increase --colors or use --dither bayer / --dither floyd.

Warning "terminal did not report its pixel size"
    The terminal does not answer the size query. The size is estimated from
    the number of characters (10 x 20 pixels per cell). For exact values give
    --width and --height directly.

"--fit needs a terminal on stdin and stdout"
    The output is redirected or the input comes from a pipe. Use
    --width/--height.

"--show needs standard output to be a terminal"
    The output is redirected to a file or pipe. Run without redirection, or
    omit --show and save with -o.

Warning "the terminal does not report sixel support" with --show
    The terminal did not answer the query with sixel support. The image is
    still written; if only garbage characters appear, see above.

Error while unpacking the native library / virus scanner reports something
    The exe unpacks SkiaSharp to %TEMP%\.net. If this folder is blocked, set
    the environment variable DOTNET_BUNDLE_EXTRACT_BASE_DIR to another writable
    folder.

"error: The sixel image exceeds the configured size limits."
    The sixel data describes an image larger than 16384 x 16384 pixels or more
    than 64 million pixels (protection against corrupt or malicious data).

"error: The data is not a supported image format."
    The input file is neither BMP nor a format SkiaSharp can read, or it is
    damaged. RLE-compressed BMP files are not supported.


12. USING THE .NET LIBRARY (NUGET)
----------------------------------
The packages are located in the folder "nuget". To use them, register the folder
as a local NuGet source, for example in a nuget.config next to the project file:

    <configuration>
      <packageSources>
        <add key="local" value="C:\path\to\publish\nuget" />
      </packageSources>
    </configuration>

and then:

    dotnet add package SharpSixel
    dotnet add package SharpSixel.Skia      (only for PNG/JPEG)

Target platforms: .NET Standard 2.0 and .NET 10. SharpSixel has no
dependencies; SharpSixel.Skia requires SkiaSharp (MIT license).

Example: load a BMP, encode it, decode it again

    using SharpSixel;

    SixelImage image = BmpCodec.Decode(File.ReadAllBytes("photo.bmp"));

    var options = new SixelEncoderOptions
    {
        MaxColors = 64,
        Dithering = SixelDithering.Ordered,   // None, FloydSteinberg, Ordered
    };
    string sixel = new SixelEncoder(options).Encode(image);
    Console.Write(sixel);                      // show in a sixel terminal

    SixelImage back = new SixelDecoder().Decode(sixel);
    File.WriteAllBytes("result.bmp", BmpCodec.Encode(back));

Example: PNG/JPEG via SharpSixel.Skia, shrinking an image

    using SharpSixel;
    using SharpSixel.Skia;
    using SkiaSharp;

    SixelImage image = SkiaImageCodec.Decode(File.ReadAllBytes("photo.png"));
    SixelImage small = ImageScaler.Fit(image, maxWidth: 800, maxHeight: 600);
    byte[] png = SkiaImageCodec.Encode(small, SKEncodedImageFormat.Png);

Important types:

  SixelImage            Pixel buffer (RGBA, 8 bit per channel, not premultiplied)
  SixelColor            Color value (R, G, B, A)
  SixelEncoder          Image -> sixel (Encode, EncodeToBytes, output to TextWriter)
  SixelEncoderOptions   MaxColors, AlphaThreshold, EmitRasterAttributes, Dithering
  SixelDecoder          Sixel (string, byte[], Stream) -> image
  SixelDecoderOptions   Size limits, number of color registers, background color
  BmpCodec              Read and write BMP
  ImageScaler           Resize, Fit, FitSize
  SixelTerminal         Terminal query (sixel support, pixel size)
  SixelException        Error for invalid data or exceeded limits

The terminal query (SixelTerminal.Detect / Query) is independent of the actual
input/output: it expects a TextWriter and a function that returns the next
input character (or -1 on timeout). The terminal has to be in raw mode for
this; providing that is up to the caller (SharpSixel.exe contains a ready-made
variant: tested on Windows, the Unix path is implemented but not yet tested on
Linux/macOS).


13. TECHNICAL DETAILS AND LIMITS
--------------------------------
Supported sixel commands (DECSIXEL):
  * Control sequence ESC P P1;P2;P3 q ... ESC \ (7-bit and 8-bit variants)
  * Raster attributes  " Pan;Pad;Ph;Pv   (aspect ratio and image size)
  * Color definition and selection  # Pc;Pu;Px;Py;Pz  (RGB and HLS, values in percent)
  * Repeat  ! Pn <character>
  * Carriage return $ and next sixel band -
  * Background selection via P2 (0/2 = background color, 1 = transparent)

Decoder behavior:
  * HLS colors follow the DEC convention (blue = 0 degrees, red = 120 degrees,
    green = 240 degrees).
  * Without a color selection, drawing uses white. Undefined registers 0-15
    have the VT340 default colors, higher registers are black. Register
    numbers above the register count (default 256) wrap around (modulo).
  * Unknown characters and line breaks are ignored.
  * The image height follows the raster attributes or the data (a multiple of
    6 rows); surplus rows in the last band are cut off if the height is
    declared.
  * The pixel aspect ratio from P1 or the raster attributes is stored in the
    image object but is not applied by the command line when converting.
  * Limits (protection against memory exhaustion): width and height 16384
    pixels each, 64 million pixels in total. Adjustable in the library via
    SixelDecoderOptions.

Encoder behavior:
  * At most 256 colors (color registers 0-255), color values in percent.
  * Run-length encoding from four identical characters on, only 7-bit ASCII in
    the output (ESC P ... ESC \).
  * The output contains raster attributes with aspect ratio and image size.
  * No animations, no interlacing.

BMP: reads 1/4/8 bit (palette), 16/24/32 bit (also bitfields, alpha via
bitfields), stored top-down or bottom-up; RLE compression is not supported.
Writes: 24 bit, 32 bit (BGRA, V4 header) if transparent.


14. BUILDING FROM SOURCE
------------------------
Requirement: .NET SDK 10.

    dotnet test                 run all tests
    ./publish.ps1               create SharpSixel.exe, the README files and
                                the NuGet packages in the folder "publish"
                                (Release only)
    ./publish.ps1 -Version 0.2.0   the same with a different version number

Pack and publish are intended for Release builds only; a call with "-c Debug"
is rejected with an error message.


15. LICENSE
-----------
SharpSixel is released under the MIT license (see the file LICENSE in the
source folder).

Copyright (c) 2026 Alexander Feuster

Components used: SkiaSharp (MIT license) for PNG/JPEG support in SharpSixel.exe
and SharpSixel.Skia.


16. NOTE ON THE USE OF AI (VOLUNTARY)
-------------------------------------
Parts of this project (source code, tests and documentation) were created with
the assistance of the AI language model Claude by Anthropic.

  Model in use: Claude Sonnet 5.5

Only this one model version was used. The author is responsible for the
content, its review and the publication. This note is voluntary, serves
transparency and does not restrict the MIT license.
