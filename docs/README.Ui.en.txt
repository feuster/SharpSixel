================================================================================
 SharpSixelUI / SharpSixel.Ui  -  Terminal widgets drawn as sixel graphics
================================================================================

 Version : 1.3.0
 Author  : Alexander Feuster
 License : MIT
 Deutsche Version: README.Ui.txt


1. WHAT IS IT?
--------------
SharpSixel.Ui is a .NET library (NuGet, depends only on SharpSixel) that draws
terminal widgets. If the terminal supports sixel, the widget is drawn as a
graphic; otherwise it is built from ASCII characters. Styles and colors are
customizable. Available widgets:

  * ProgressBar  with optional caption and percent or value/maximum text
  * Spinner      with optional caption
  * StatusDisplay  spinner with a changeable status text for work of unknown duration
  * BarChart     horizontal bar chart: label, bar, value; optional caption
  * Table        table with column headers and aligned text cells in a grid; optional caption
  * Button       label with state (normal, focused, pressed, disabled)
  * Panel        frame around text lines with an optional title in the top edge
  * Clock        time: sixel draws an analog clock (face, hour, minute and second hand) with the digital time next to it, ASCII only digital
  * DateDisplay  date: sixel draws a calendar page with the day number and the date text next to it, ASCII only text
  * Tree         hierarchical list with guide lines, optional title as root and collapsible nodes
  * ImageView    image fitted into a view of fixed size, optional frame, own
                 background; image as graphic or loaded from a file
  * Prompts      interactive input with validation: Dialog (message with buttons),
                 SelectionPrompt (one item),
                 MultiSelectionPrompt (several items), TextPrompt (text input),
                 OpenFileDialog / SaveFileDialog (choose a file path)

SharpSixelUI.exe is a demo program for these widgets. The main program
SharpSixel.exe is described in README.en.txt.


2. THE DEMO PROGRAM
-------------------
    SharpSixelUI <progress|spinner|status|chart|table|button|panel|clock|date|tree|image|prompts|open|save|osc|demo> [options]

  progress    animated progress bar from 0 to 100 %
  spinner     animated spinner
  status      status display: spinner with changing text, messages scroll above it
  chart       animated bar chart (demo data grows to its values)
  table       table with demo data; --caption sets the caption, --border the grid color, --active the header text color,
              --inactive the header background, --track the background of every second row
  button      one button that cycles through its states (normal, focused, pressed, disabled); --caption sets the label
  panel       framed box with demo lines; --caption sets the title (default Status), --width the text width in cells
              (default: widest line), --border the frame color, --active the title color, --background the inside
              of the frame, --radius the corner radius
  clock       shows the current time, updated continuously (--duration seconds, default 5); --caption sets a label before it,
              --rows the size of the graphic (1-20, default 3), --border the rim, --fill the face, --active the hands,
              --background the background of the graphic
  date        shows the current date (e.g. Wed, 28 Oct 2026); --caption, --rows, --background as for clock, plus
              --radius (corner radius), --border (outline), --fill (page) and --active (day number)
  tree        shows a tree with demo nodes (--caption sets the root title, default "Files"; the collapsed node "tests" is
              marked [+]); --border sets the guide color, --active the title color
  image       shows an image fitted into a view: --file (BMP or sixel) or a generated
              demo image; --border adds a frame, --background sets the view background
  prompts     interactive: dialog, selection, multi-selection, text and password (clickable with the mouse)
              with validation (Up/Down, Space, Enter; Ctrl+C cancels); needs a
              terminal and is not part of demo
  open        interactive open-file dialog: --dir sets the start directory, --filter the listed files
              (e.g. *.bmp;*.sixel), --caption the title; prints the chosen path; needs a terminal, not part of demo
  save        interactive save-file dialog (like open, plus a name field); only prints the path, writes nothing
  osc         changes the terminal with SharpSixel.Osc: --caption sets the title (default "SharpSixel OSC demo"), --fill the text
              color, --background the background color, --active the cursor color (defaults: gold, dark blue, red); the cursor
              style cycles through box, underscore and bar (blinking and static) during --duration seconds; Ctrl+C stops it;
              everything is reset afterwards; needs a terminal, not part of demo
  demo        progress bar, spinner, status display, bar chart, table, button, then panel, then time, date and tree (default captions "Download"/"Loading"/"Fruit sales"/"Fruit stock"/"Status"/"Files")

Options:

  --mode auto|sixel|ascii        auto (default): sixel if the terminal reports it
  --caption <text>               caption of the widget (status: fixed status text instead of demo stages)
  --text none|percent|fraction   text behind the bar (progress, default percent)
  --duration <seconds>           run time per widget (default 5)
  --width <cells>                width in cells (progress: bar, at least 3; chart: bar area, at least 1; button: minimum width; panel: text width; image: view, default 40)
  --rows <n>                     rows of the sixel graphic (progress 1-20; chart: rows per bar 1-10; button 1-10; image: view height, default 10; clock, date: size 1-20, default 3)
  --file <path>                  image file of the image command (BMP or sixel)
  --dir <path>                   start directory of open/save (default: current directory)
  --filter <patterns>            open/save: list only matching files, semicolon separated (e.g. *.bmp;*.sixel)
  --radius <px>                  corner radius in pixels (progress, chart, button, panel, date)
  --fill | --track | --border <color>   colors of the bar (progress; chart: --track; table: --border = grid, --track = stripes; image: --border = frame color; panel: --border = frame; clock: --fill = face, --border = rim; date: --fill = page, --border = outline; tree: --border = guide lines)
  --active | --inactive <color>         colors of the spinner (prompts: current/other items; button: focused/normal face; table: header text/background; panel: --active = title text; clock: --active = hands; date: --active = day number; tree: --active = title text)
  --background <color>           background of the graphic (prompts, open, save: of the markers; image: of the view; panel: inside of the frame; clock, date: of the graphic)
  --no-ansi                      no ANSI colors in ASCII mode
  --no-mouse                     no mouse input (prompts, open, save, button --click)
  --click                        button: wait until the button is clicked (or Enter/Space is pressed); needs a terminal
  -h, --help                     help

Colors: #RRGGBB, #RRGGBBAA (alpha) or "transparent".

Examples:

    SharpSixelUI demo
    SharpSixelUI progress --caption Upload --text fraction --fill #E67E22 --rows 2
    SharpSixelUI spinner --mode ascii --caption "Please wait"
    SharpSixelUI status
    SharpSixelUI chart --caption Sales --rows 2 --track #333333
    SharpSixelUI table --caption Stock --border #3498DB --track #202830
    SharpSixelUI button --caption Save --rows 2 --active #E67E22
    SharpSixelUI panel --caption Status --border #3498DB --background #101820
    SharpSixelUI clock --caption Time --rows 4 --active #E67E22
    SharpSixelUI date --caption Today --border #3498DB
    SharpSixelUI tree --caption Project --border #3498DB
    SharpSixelUI image --file logo.bmp --width 30 --rows 8 --border #3498DB --background #101820
    SharpSixelUI prompts --mode ascii
    SharpSixelUI open --dir C:\Images --filter "*.bmp;*.sixel"
    SharpSixelUI save --caption Export --mode ascii
    SharpSixelUI progress --duration 3 > out.txt      (redirected: one ASCII line)

Ctrl+C ends the animation cleanly. If the output is redirected (file, pipe),
no animation is made: the widget is printed once as ASCII text
(chart, table, panel: one line per bar/row/text line).
Exit codes: 0 success, 1 error (invalid argument, unknown command).


3. USING THE LIBRARY
--------------------
    dotnet add package SharpSixel.Ui      (from the local "nuget" folder)

Target platforms: .NET Standard 2.0 and .NET 10, no dependencies besides SharpSixel.

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

readChar is the input function of the terminal query (see README.en.txt, section 12).
The terminal must process VT sequences (enable them on Windows).

Status display for work of unknown duration (spinner with text, timer animation):

    using (var status = new StatusDisplay(Console.Out, env))
    {
        status.Start("Loading data ...");
        status.Text = "Connecting ...";
        status.WriteLine("Configuration loaded");   // appears above the spinner
    }   // Dispose removes the display and shows the cursor again

Bar chart (add items with Add, change values at any time):

    var chart = new BarChart { Caption = "Sales" };
    chart.Style.Width = 30;
    chart.Add("Apples", 42);
    chart.Add("Pears", 28, new SixelColor(231, 76, 60));   // own color

    using (var region = new LiveRegion(Console.Out, env))
    {
        region.Begin(chart.GetRows(env));
        chart.Items[0].Value = 50;
        region.Update(chart);
    }

Table (columns with alignment, rows as texts; change cells at any time):

    var table = new Table { Caption = "Stock" };
    table.AddColumn("Fruit");
    table.AddColumn("Quantity", TableAlignment.Right, width: 8);   // fixed width keeps the size stable
    string[] apples = table.AddRow("Apples", "42");
    table.AddRow("Pears", "28");
    table.Style.ShowRowSeparators = true;
    table.Style.StripeColor = new SixelColor(32, 40, 48);          // every second row (sixel)

    using (var region = new LiveRegion(Console.Out, env))
    {
        region.Begin(table.GetRows(env));
        apples[1] = "50";
        region.Update(table);
    }

Button (draws only; you change State between frames, e.g. on key presses):

    var button = new Button("Save");
    button.Style.MinWidth = 12;
    button.State = ButtonState.Focused;      // Normal, Focused, Pressed, Disabled

    using (var region = new LiveRegion(Console.Out, env))
    {
        region.Begin(button.GetRows(env));
        region.Update(button);
        button.State = ButtonState.Pressed;
        region.Update(button);
    }

Panel (frame around text lines, optional title; change lines at any time):

    var panel = new Panel { Title = "Status", Width = 20 };   // fixed width keeps the size stable
    panel.AddLine("Connected");
    panel.AddLine("Files: 0");
    panel.Style.BackgroundColor = new SixelColor(16, 24, 32); // inside of the frame (sixel)

    using (var region = new LiveRegion(Console.Out, env))
    {
        region.Begin(panel.GetRows(env));
        panel.Lines[1] = "Files: 42";   // lines can change, their number must not grow
        region.Update(panel);
    }

Time and date (the widgets do not tick by themselves: set Time/Date before each frame):

    var clock = new Clock { Caption = "Time" };
    clock.Style.HeightRows = 2;            // size of the analog clock (sixel)
    clock.Style.TimeFormat = "HH:mm:ss";   // format of the digital time (default)

    using (var region = new LiveRegion(Console.Out, env))
    {
        region.Begin(clock.GetRows(env));
        clock.Time = DateTime.Now;
        region.Update(clock);
    }

    var date = new DateDisplay { Date = DateTime.Today };   // default format "ddd, dd MMM yyyy"
    // as above: region.Begin(date.GetRows(env)); region.Update(date);

Tree (hierarchical list with guide lines; the title is the root, collapsed nodes hide their children):

    var tree = new Tree { Title = "Files" };
    TreeNode src = tree.AddNode("src");
    src.AddNode("Program.cs");
    tree.AddNode("docs").AddNode("README.md");
    TreeNode tests = tree.AddNode("tests");
    tests.AddNode("TreeTests.cs");
    tests.Expanded = false;                 // shown as "tests [+]"

    using (var region = new LiveRegion(Console.Out, env))
    {
        region.Begin(tree.GetRows(env));    // rows = title + visible nodes, must not grow later
        region.Update(tree);
    }

Image view (the image is fitted into the view automatically):

    var view = new ImageView(40, 10, new ImageViewStyle { ShowBorder = true });
    view.Style.BackgroundColor = new SixelColor(16, 24, 32);   // independent of the terminal
    view.Load("logo.bmp");                                     // or view.Image = someSixelImage;
    view.Decoder = SkiaImageCodec.Decode;                      // optional: PNG/JPEG via SharpSixel.Skia

    using (var region = new LiveRegion(Console.Out, env))
    {
        region.Begin(view.GetRows(env));
        region.Update(view);
    }

Prompts (Show returns the validated answer; keys from Console.ReadKey(true)):

    var dialog = new Dialog { Title = "Delete file?", Message = "report.txt will be removed." };
    dialog.SetButtons("Delete", "Cancel");
    dialog.DefaultButton = 1;
    bool delete = dialog.Show(Console.Out, env) == 0;

    var fruit = new SelectionPrompt<string> { Title = "Favorite fruit?" };
    fruit.AddChoices(new[] { "Apples", "Pears", "Plums" });
    string one = fruit.Show(Console.Out, env);

    var toppings = new MultiSelectionPrompt<string> { Title = "Toppings?", MinSelected = 1, MaxSelected = 3 };
    toppings.AddChoice("Cream", selected: true);
    toppings.AddChoice("Sugar");
    IReadOnlyList<string> many = toppings.Show(Console.Out, env);

    var name = new TextPrompt { Title = "Name?", MinLength = 2 };
    name.Validator = v => v == "root" ? ValidationResult.Error("Reserved name.") : ValidationResult.Success();
    string text = name.Show(Console.Out, env);

    var open = new OpenFileDialog { Title = "Open image", InitialDirectory = @"C:\Images", Filter = "*.bmp;*.sixel" };
    string path = open.Show(Console.Out, env);       // full path; Esc throws OperationCanceledException

    var save = new SaveFileDialog { FileName = "image.sixel", ConfirmOverwrite = true };
    string target = save.Show(Console.Out, env);     // chooses a path, writes nothing

Mouse (prompts, file dialogs and button): ShowWithMouse takes a source of keys and mouse reports.
The terminal sends them as VT sequences on standard input; VtInputParser turns the
characters into InputEvents (key, mouse, cursor position):

    var parser = new VtInputParser(readChar, readCharShort);   // -1 = end / timeout
    bool delete = dialog.ShowWithMouse(Console.Out, env, parser.Read) == 0;

A left click chooses a dialog button or a selection item and toggles a
multi-selection item and opens a directory or chooses a file in a file dialog;
the wheel moves the cursor. For your own widgets:

    using (var region = new LiveRegion(Console.Out, env))
    {
        region.Begin(button.GetRows(env));
        region.EnableMouse(parser.Read);              // mouse reporting on, origin queried
        region.Update(button);
        InputEvent e = parser.Read();
        if (e.Mouse != null && region.TryGetLocal(e.Mouse, out int col, out int row)
            && button.Contains(env, col, row)) { /* clicked */ }
    }

Terminal control (separate package SharpSixel.Osc, depends only on SharpSixel; every method takes an optional
TextWriter, default Console.Out):

    using SharpSixel.Osc;

    TerminalControl.SetTitle("My tool");                            // control characters are removed
    TerminalControl.SetForegroundColor("#FFD700");                  // #RGB or #RRGGBB, otherwise ArgumentException
    TerminalControl.SetBackgroundColor(new SixelColor(16, 32, 64)); // SixelColor, alpha is ignored
    TerminalControl.SetCursorColor("#FF4040");
    TerminalControl.SetCursorStyle(CursorStyle.BarBlinking);        // Default, Box*, Underscore*, Bar*
    TerminalControl.Reset();   // colors and cursor style to the defaults; title restored on Windows only

Types:

  Prompt<T>           base of the prompts: Title, Style, Validator, Show(writer, env, readKey),
                      ShowWithMouse(writer, env, readInput)
  InputEvent          Kind (Key, Mouse, CursorPosition), Key, Mouse, Row, Column
  MouseEvent          Column, Row (0-based), Button (Left, Middle, Right, WheelUp, WheelDown), IsPress
  VtInputParser       Read(): VT characters to InputEvents (keys, SGR mouse, cursor report)
  Dialog              Message, Buttons, SetButtons, DefaultButton; Show returns the button index
  SelectionPrompt<T>  AddChoice, AddChoices, Choices, Converter, PageSize, Instructions
  MultiSelectionPrompt<T>  like SelectionPrompt plus AddChoice(item, selected),
                      MinSelected, MaxSelected
  TextPrompt          DefaultValue, AllowEmpty, MinLength, MaxLength, Pattern,
                      PatternMessage, IsSecret, Mask
  FileDialog          base of OpenFileDialog and SaveFileDialog: InitialDirectory, Filter, ShowHidden,
                      PageSize, Width, Instructions; Show returns the full path
  OpenFileDialog      choose an existing file
  SaveFileDialog      FileName, ConfirmOverwrite; name field, chooses a path (writes nothing)
  ValidationResult    Success(), Error(message), IsValid, Message
  PromptStyle         ActiveColor, InactiveColor, ErrorColor, BackgroundColor,
                      AsciiPointer, AsciiRadioOn/Off, AsciiCheckOn/Off
  WidgetEnvironment   sixel or ASCII, cell size in pixels, ANSI usage;
                      Create(RenderMode, SixelTerminalInfo?) decides by mode
  RenderMode          Auto, Sixel, Ascii
  Widget              base class: Render(env) returns one frame, GetRows(env)
  BarChart            Items, Add(label, value, color), Caption, Maximum, ShowValues,
                      ValueFormat, Style
  BarChartItem        Label, Value, Color (null = color from Style.Palette)
  BarChartStyle       Width, BarHeightRows, BarThickness, CornerRadius, Palette,
                      TrackColor, BackgroundColor, AsciiFill, AsciiEmpty
  Button              Label, State, Columns, Style
  ButtonState         Normal, Focused, Pressed, Disabled
  ButtonStyle         Padding, MinWidth, HeightRows, CornerRadius, NormalColor,
                      FocusedColor, PressedColor, DisabledColor, TextColor,
                      DisabledTextColor, BackgroundColor, AsciiBorderLeft/Right,
                      AsciiFocusLeft/Right, AsciiPressedMarker, AsciiDisabledMarker
  Panel               Lines, AddLine(text), Title, Width (null = fit to the widest line/the title), Style
  PanelStyle          Padding, LineThickness, CornerRadius, BorderColor, TitleColor,
                      BackgroundColor, AsciiHorizontal, AsciiVertical, AsciiCorner
  Clock               Time, Caption, Style
  ClockStyle          HeightRows, LineThickness, ShowSecondHand, TimeFormat, BorderColor, FaceColor,
                      TickColor, HandColor, SecondHandColor, TextColor, BackgroundColor
  DateDisplay         Date, Caption, Style
  DateDisplayStyle    HeightRows, LineThickness, CornerRadius, DateFormat, BorderColor, AccentColor,
                      DigitColor, PaperColor, TextColor, BackgroundColor
  Tree                Title, Nodes, AddNode(text), Style
  TreeNode            Text, Expanded, Nodes, AddNode(text)
  TreeStyle           LineThickness, CollapsedSuffix, GuideColor, TitleColor,
                      AsciiVertical, AsciiBranch, AsciiLastBranch, AsciiHorizontal
  ImageView           Image, Load(path|stream|bytes), Decoder, AllowUpscale, Columns, Rows, Style
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
  TableColumn         Header, Alignment, Width (null = fit to the widest text)
  TableAlignment      Left, Right, Center
  TableStyle          Padding, LineThickness, ShowBorder, ShowHeader, ShowColumnSeparators,
                      ShowRowSeparators, BorderColor, HeaderTextColor, HeaderBackgroundColor,
                      StripeColor, AsciiHorizontal, AsciiVertical, AsciiCorner
  StatusDisplay       Start(text), Text, WriteLine(text), Refresh(), Stop(), IsRunning,
                      AutoRefresh, RefreshInterval, SpinnerStyle
  LiveRegion          Begin(rows), Update(widget), End(): redraws in place;
                      EnableMouse(readInput), TryGetLocal(mouse, out col, out row)


4. NOTES AND LIMITS
-------------------
  * Captions and progress text are normal terminal text next to the graphic
    (sixel has no font). One character is assumed to occupy one cell.
  * LiveRegion reserves the rows first, saves the cursor position (ESC 7 / ESC 8)
    and moves relative to it. It writes to any TextWriter. Console.SetCursorPosition
    is not used: it works with absolute buffer coordinates, throws outside the
    buffer, and cannot be tested without a console.
  * Where the cursor ends up after a sixel image differs between terminals,
    therefore every sixel frame is positioned relative to the saved origin.
  * With a transparent background (default), the covered cells are erased
    (ECH, ESC [ n X) before each frame. If your terminal leaves old pixels,
    set an opaque BackgroundColor.
  * StatusDisplay animates on a timer (RefreshInterval, default 100 ms; without
    AutoRefresh only on Refresh()). While it runs, do not write to the output
    directly; use WriteLine. Without ANSI/sixel every text change is printed as
    its own line.
  * BarChart: after LiveRegion.Begin the number of items must not grow beyond the
    reserved rows and should stay constant while shown. Without Maximum the
    largest value fills the full width; negative values and NaN give empty bars.
    In ASCII mode Render returns one line per bar (separated by \n, caption
    first); LiveRegion places each line on its row. Without LiveRegion print the
    lines directly.
  * Table: sixel and ASCII use the same rows (caption, top rule, header, header rule,
    one row per data row, bottom rule; row rules only with ShowRowSeparators), so
    GetRows is the same in both modes. After LiveRegion.Begin the number of rows
    must not grow beyond the reserved rows; fixed TableColumn.Width values keep the
    width stable while cells change (otherwise columns fit their widest text).
    Longer text is cut with an ellipsis, missing cells are empty, control characters
    become spaces. Sixel: the grid is drawn as thin pixel lines in separate cells
    (terminals may erase graphics below text), header and cell texts are terminal
    text; header and stripe backgrounds (ANSI) and LineThickness are sixel only.
    ASCII: grid of + - | with optional ANSI colors for grid and header.
  * Button: draws a button, it does not handle keys or clicks; set State
    between frames (see Dialog for an interactive row of buttons; Contains tells
    whether a mouse position hit the button). Sixel: rounded
    face in the color of the state, the label is drawn as terminal text centered
    on the face (with the face color as text background, so it also works if the
    terminal clears graphics under text); with one row the graphic is slightly
    lower than the cell (multiple of 6 pixels). ASCII: [ OK ] with the markers
    [>OK<] focused, [#OK#] pressed, [-OK-] disabled (colored with ANSI). Width =
    label + 2 * (Padding + 1) cells, at least MinWidth.
  * Panel: frame around text lines with an optional title in the top edge. Sixel and
    ASCII use the same rows (top edge, one row per text line, bottom edge), so
    GetRows is the same in both modes; after LiveRegion.Begin the number of lines
    must not grow beyond the reserved rows. Width is the width of the text area
    without padding and frame: null fits it to the widest line or the title, a fixed
    value keeps the size stable (longer text and a title that does not fit are cut
    with an ellipsis; the title is dropped if the panel is very narrow). Control
    characters become spaces. Sixel: the rounded frame is one image (LineThickness,
    CornerRadius and BackgroundColor as the inside of the frame are sixel only); the
    title cells get no line pixels because terminals may erase graphics below text;
    title and lines are terminal text (with an ANSI background if BackgroundColor is
    opaque). ASCII: frame of + - | with optional ANSI colors for frame and title.
  * Clock: shows Time (time of day only); the widget does not tick by itself, set Time
    before each frame (e.g. DateTime.Now). Sixel: square analog clock (rim, 12 hour
    marks, hour and minute hand, optional second hand) as one image, followed by the
    digital time as terminal text (TimeFormat, invariant culture, default "HH:mm:ss";
    TextColor, otherwise the terminal color). HeightRows, LineThickness, ShowSecondHand
    and the other colors are sixel only. ASCII: the digital time only, one line.
    Caption is shown before it.
  * DateDisplay: shows Date (date part only), set like Clock. Sixel: square calendar
    page (outline, separator line, two binder rings, day number in seven-segment
    digits) as one image, followed by the formatted date as terminal text (DateFormat,
    default "ddd, dd MMM yyyy", e.g. Wed, 28 Oct 2026). ASCII: the date text only.
    The style setters reject invalid formats with a FormatException.
  * Tree: hierarchical list of TreeNode objects (Text, Expanded, Nodes) with an optional
    Title as root row. Sixel and ASCII use the same rows (title, then one row per visible
    node; children of collapsed nodes are hidden and the node gets CollapsedSuffix,
    default " [+]"), so after LiveRegion.Begin the number of rows must not grow beyond
    the reserved rows. A node must not be added to itself or to one of its descendants.
    Without a title the top-level nodes start at the left edge. Sixel: the guide lines
    (LineThickness, GuideColor) are one image in their own 4-cell columns per level; the
    node texts are terminal text after the guides because terminals may erase graphics
    below text, and should fit on one terminal row. ASCII: guides of | + ` - (Ascii*
    properties of the style) with optional ANSI colors for guides and title.
  * ImageView: the view has a fixed size (Columns x Rows cells; sixel image
    Columns * cell width by GetImageHeight(Rows) pixels). The image is fitted into
    the area inside the frame when drawn: aspect ratio kept, centered, enlarged
    unless AllowUpscale is false; the fitted copy is cached, assign Image again
    after changing its pixels. Load reads BMP and sixel; other formats need a
    Decoder (SharpSixel.Ui itself has no image codec dependency). BackgroundColor
    is drawn inside the frame regardless of the terminal background; semi-
    transparent pixels are blended onto an opaque background. The ASCII fallback
    maps one pixel to one cell (character of AsciiRamp by brightness, ANSI color
    of the pixel), draws a one-character frame if the view is at least 3x3 and
    ignores the background color. Demo: SharpSixelUI image reads BMP and sixel
    files only.
  * Prompts: Show draws the prompt in place (sixel markers: circle for selection,
    square for multi-selection, dot for text; ASCII: pointer and (*) / [x]),
    reads keys until the answer is valid, removes the prompt and prints one
    summary line. Dialog: Left/Right/Tab (wrapping), Home/End, Enter; the selected
    button is drawn as [ Label ] (reverse video with ANSI), a multi-line message
    is allowed. Keys: Up/Down (wrapping), Home/End, PageUp/PageDown, Space
    (multi-selection), Enter; text: characters, Backspace, Enter (no cursor
    movement inside the text). Validation runs on Enter: built-in rules first
    (MinSelected/MaxSelected; AllowEmpty, MinLength, MaxLength, Pattern), then
    the custom Validator. Invalid input shows its message below the prompt.
    Keys come from Console.ReadKey(true) unless readKey is given. Without
    ANSI/sixel support the choices are numbered and a line is typed (several
    numbers separated by commas; empty keeps the preselection). Labels and text
    should fit on one terminal row. The demo program reads keys while the
    terminal is not in raw mode (except with the mouse, see below).
  * Look of the prompts in sixel mode (PromptStyle): a rounded frame around the
    prompt (ShowFrame, FrameColor, CornerRadius; two more rows and columns, mouse
    positions are corrected), a bar behind the current row of the selection
    prompts and file dialogs (HighlightColor; Transparent turns it off), larger
    radio/check/dot markers, check boxes with a check mark cut out of the box and
    dialog buttons as rounded faces (ButtonColor, ButtonTextColor; the selected
    button uses ActiveColor). An opaque BackgroundColor fills the prompt as a
    panel. The graphic is drawn first, the text on top with the matching ANSI
    background (text in a cell replaces the graphic below it). ASCII mode is
    unchanged.
  * File dialogs: OpenFileDialog and SaveFileDialog are prompts that browse the
    file system. Row 1 shows the current directory, below it PageSize rows
    (constant, padded; longer lists scroll) with ".." (on Windows above a drive
    root: the drive list), the directories, then the files, sorted by name;
    hidden and system entries only with ShowHidden; Filter (semicolon separated
    wildcards * and ?) limits the files. Keys: Up/Down (wrapping), Home/End,
    PageUp/PageDown; Enter opens a directory or chooses a file; Esc throws
    OperationCanceledException (like Ctrl+C in the demo). Names longer than Width
    are shortened with an ellipsis. SaveFileDialog adds a name field: typing and
    Backspace select the field, the list keys select the list; Enter in the field
    confirms the name in the shown directory, Enter on a file in the list copies
    its name and confirms. With ConfirmOverwrite (default) an existing file is
    only accepted on a second Enter (or click); the validator sees the full path.
    Nothing is created or written. Without ANSI/sixel support the user types a
    file name or path (relative to the start directory; empty = FileName) and
    answers an overwrite question with y/n; Filter is not applied then. A missing
    InitialDirectory throws InvalidOperationException. A click on a list entry
    opens the directory or chooses the file, a click on the name field focuses it.
  * Mouse: ShowWithMouse and LiveRegion.EnableMouse turn on mouse reporting
    (ESC [?1000h, ?1006h) and ask the terminal for the cursor position (ESC [6n)
    to know where the region is; the input must be read in VT mode (Windows:
    ENABLE_VIRTUAL_TERMINAL_INPUT, quick edit off; Unix: stty -icanon -echo -isig).
    Mouse positions are valid as long as the screen does not scroll or resize.
    Without an answer to the position request the call blocks. A click on a
    selection item confirms it (the validator may reject it), a click on a
    multi-selection item toggles it (confirm with Enter), text prompts ignore the
    mouse. SharpSixelUI: prompts, open, save and button --click use the mouse unless
    --no-mouse is given; Ctrl+C is read as a character and cancels. Tested in Windows
    Terminal, not in other terminals.
  * If the output is not a terminal, do not use LiveRegion; call widget.Render(env)
    with an ASCII environment instead and print the line(s).


5. LICENSE
----------
MIT (see LICENSE). Copyright (c) 2026 Alexander Feuster.
Parts of this project were created with the assistance of the AI language model
Claude by Anthropic (see README.en.txt, section 16).
