using SharpSixel;
using SharpSixel.Osc;
using SharpSixel.Ui;

namespace SharpSixelUI;

/// <summary>Demo CLI: shows the widgets of SharpSixel.Ui.</summary>
internal static class UiCliApp
{
    internal const string HelpText = @"SharpSixelUI - demo of the SharpSixel.Ui widgets

Usage: SharpSixelUI <progress|spinner|status|chart|table|button|panel|clock|date|tree|image|prompts|open|save|osc|demo> [options]

table shows a table with demo data (--caption sets the caption); --border sets the grid color, --active the header text
color, --inactive the header background, --track the background of every second row.

button shows one button cycling through its states (normal, focused, pressed, disabled); --caption sets the label.

panel shows a framed box with demo lines; --caption sets the title (default Status), --width the text width in cells
(default: fits the widest line); --border sets the frame color, --active the title color, --background the inside of the
frame, --radius the corner radius.

clock shows the current time for --duration seconds: sixel draws an analog clock face (--rows = size) followed by the digital
time, ASCII shows the digital time; --caption sets a label before it; --border sets the rim, --fill the face, --active the
hands, --background the background of the graphic.

date shows the current date: sixel draws a calendar page with the day number (--rows = size) followed by the date text (in the
language and date order of the system, for example Wed, 10/28/2026 or Mi, 28.10.2026), ASCII shows the date text; --caption
sets a label before it; --radius sets the corner radius,
--border the outline, --fill the page, --active the day number, --background the background of the graphic.

tree shows a tree with demo nodes (--caption sets the root title, default Files; a collapsed node is marked [+]): sixel draws
the guide lines as pixel lines, ASCII uses | + ` -; --border sets the guide color, --active the title color.

image shows a file (--file, BMP or sixel) or a generated demo image fitted into a view of --width x --rows cells
(default 40 x 10); --border <color> adds a frame, --background <color> sets the view background.

prompts is interactive (needs a terminal): dialog, selection, multi-selection and text prompts with validation
(Up/Down, Space, Enter; Ctrl+C cancels). It is not part of demo.

open and save are interactive (need a terminal): an open-file / save-file dialog starting in --dir (default: the current
directory), listing only the files matching --filter (for example *.bmp;*.sixel); --caption sets the title. Up/Down,
PageUp/PageDown, Home/End and the mouse move, Enter opens a directory or chooses a file, Esc or Ctrl+C cancels; save also
has a name field (type a name, Enter confirms). The chosen path is printed; nothing is written. Not part of demo.

osc changes the terminal with SharpSixel.Osc (needs a terminal): --caption sets the title (default SharpSixel OSC demo),
--fill the text color, --background the background color, --active the cursor color (defaults: gold, dark blue, red).
The cursor style cycles through box, underscore and bar (blinking and static) during --duration seconds; Ctrl+C stops
it. Everything is reset afterwards (on Windows including the title). Not part of demo.

Options:
  --mode auto|sixel|ascii   Rendering (default auto: sixel if the terminal supports it)
  --caption <text>          Caption of the widget (status: fixed status text instead of demo stages)
  --text none|percent|fraction   Progress text (progress, default percent)
  --duration <seconds>      Run time per widget (default 5)
  --width <cells>           Widget width in cells (progress: bar, chart: bar area, button: minimum width, panel: text width, image: view)
  --rows <n>                Rows of the sixel graphic (chart: rows per bar, 1-10; button: 1-10; image: view height; clock, date: size)
  --file <path>             Image file of the image command (BMP or sixel)
  --dir <path>              Start directory of open/save (default: current directory)
  --filter <patterns>       open/save: list only matching files, e.g. *.bmp;*.sixel
  --radius <px>             Corner radius in pixels (progress, chart, button, panel, date)
  --fill|--track|--border <color>    Progress colors (chart: --track; table: --border = grid, --track = stripes; image: --border = frame color; panel: --border = frame; clock: --fill = face, --border = rim; date: --fill = page, --border = outline; tree: --border = guide lines)
  --active|--inactive <color>        Spinner/prompt colors (button: focused/normal face; table: header text/background; panel: --active = title text; clock: --active = hands; date: --active = day number; tree: --active = title text)
  --background <color>      Background of the graphic (prompts, open, save: of the markers; image: of the view; panel: inside of the frame; clock, date: of the graphic)
  --no-ansi                 No ANSI colors in ASCII mode
  --no-mouse                No mouse input (prompts, open, save, button --click)
  --click                   button: wait until the button is clicked (or Enter/Space is pressed); needs a terminal
  -h, --help                Show this help

Colors: #RRGGBB, #RRGGBBAA or transparent.";

    private const int ProgressFrameMs = 50;
    private const int ChartFrameMs = 50;
    private const int ButtonFrameMs = 50;
    private const int ClockFrameMs = 100;
    private const int SpinnerFrameMs = 100;
    private const string HideCursor = "\u001b[?25l";
    private const string ShowCursor = "\u001b[?25h";

    private static readonly KeyValuePair<string, double>[] ChartData =
    {
        new KeyValuePair<string, double>("Apples", 42),
        new KeyValuePair<string, double>("Pears", 28),
        new KeyValuePair<string, double>("Cherries", 65),
        new KeyValuePair<string, double>("Plums", 17),
    };

    public static int Run(string[] args, UiCliEnvironment env)
    {
        if (!UiCliArguments.TryParse(args, out UiCliArguments arguments, out string error))
        {
            env.Error.WriteLine(error);
            env.Error.WriteLine("Use --help for usage.");
            return 1;
        }

        if (arguments.Help)
        {
            env.Output.WriteLine(HelpText);
            return 0;
        }

        if (arguments.Command != "progress" && arguments.Command != "spinner" && arguments.Command != "status"
            && arguments.Command != "chart" && arguments.Command != "table" && arguments.Command != "button"
            && arguments.Command != "panel" && arguments.Command != "clock" && arguments.Command != "date" && arguments.Command != "tree"
            && arguments.Command != "image"
            && arguments.Command != "prompts" && arguments.Command != "open" && arguments.Command != "save" && arguments.Command != "osc"
            && arguments.Command != "demo")
        {
            env.Error.WriteLine(arguments.Command == null ? "No command given." : "Unknown command " + arguments.Command + ".");
            env.Error.WriteLine("Use --help for usage.");
            return 1;
        }

        try
        {
            if (arguments.Command == "prompts")
            {
                return RunPrompts(arguments, env);
            }

            if (arguments.Command == "osc")
            {
                return RunOsc(arguments, env);
            }

            if (arguments.Command == "open" || arguments.Command == "save")
            {
                return RunFileDialog(arguments, env, save: arguments.Command == "save");
            }

            if (arguments.Command == "button" && arguments.Click)
            {
                return RunButtonClick(arguments, env);
            }

            if (arguments.Command == "image")
            {
                return RunImage(arguments, env);
            }

            Execute(arguments, env);
            return 0;
        }
        catch (ArgumentException ex)
        {
            env.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int RunOsc(UiCliArguments a, UiCliEnvironment env)
    {
        if (!env.IsOutputTerminal())
        {
            env.Error.WriteLine("The osc command needs a terminal (output must not be redirected).");
            return 1;
        }

        CursorStyle[] styles =
        {
            CursorStyle.BoxBlinking, CursorStyle.BoxStatic,
            CursorStyle.UnderscoreBlinking, CursorStyle.UnderscoreStatic,
            CursorStyle.BarBlinking, CursorStyle.BarStatic,
        };
        int stepMs = Math.Max(1, (int)(a.DurationSeconds * 1000 / styles.Length));

        using (env.EnterTerminalMode())
        {
            try
            {
                TerminalControl.SetTitle(a.Caption ?? "SharpSixel OSC demo", env.Output);
                TerminalControl.SetForegroundColor(a.Fill ?? new SixelColor(0xFF, 0xD7, 0x00), env.Output);
                TerminalControl.SetBackgroundColor(a.Background ?? new SixelColor(0x10, 0x20, 0x40), env.Output);
                TerminalControl.SetCursorColor(a.Active ?? new SixelColor(0xFF, 0x40, 0x40), env.Output);
                foreach (CursorStyle style in styles)
                {
                    if (env.IsCancelled())
                    {
                        break;
                    }

                    TerminalControl.SetCursorStyle(style, env.Output);
                    env.Output.WriteLine("Cursor style: " + style);
                    env.Output.Flush();
                    env.Delay(stepMs);
                }
            }
            finally
            {
                TerminalControl.Reset(env.Output);
                env.Output.Flush();
            }
        }

        return 0;
    }

    private static int RunImage(UiCliArguments a, UiCliEnvironment env)
    {
        bool live = env.IsOutputTerminal();
        var widgets = WidgetEnvironment.Create(
            live ? a.Mode : RenderMode.Ascii,
            live ? env.QueryTerminal() : null,
            useAnsi: live && !a.NoAnsi, columns: live ? env.Columns() : 0);
        var style = new ImageViewStyle();
        if (a.Border is SixelColor border) { style.ShowBorder = true; style.BorderColor = border; }
        if (a.Background is SixelColor background) { style.BackgroundColor = background; }

        var view = new ImageView(a.Width ?? 40, a.Rows ?? 10, style);
        try
        {
            if (a.ImagePath != null)
            {
                view.Load(a.ImagePath);
            }
            else
            {
                view.Image = DemoImage();
            }
        }
        catch (Exception ex) when (ex is SixelException || ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException)
        {
            env.Error.WriteLine(ex.Message);
            return 1;
        }

        if (!live)
        {
            env.Output.WriteLine(view.Render(widgets).Replace("\n", env.Output.NewLine));
            return 0;
        }

        using (env.EnterOutputMode())
        using (var region = new LiveRegion(env.Output, widgets))
        {
            region.Begin(view.GetRows(widgets));
            region.Update(view);
        }

        return 0;
    }

    private static SixelImage DemoImage()
    {
        const int width = 200;
        const int height = 100;
        var image = new SixelImage(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte r = (byte)(255 * x / (width - 1));
                image.SetPixel(x, y, new SixelColor(r, (byte)(255 * y / (height - 1)), (byte)(255 - r)));
            }
        }

        return image;
    }

    private static PromptStyle CreatePromptStyle(UiCliArguments a)
    {
        var style = new PromptStyle();
        if (a.Active is SixelColor active) { style.ActiveColor = active; }
        if (a.Inactive is SixelColor inactive) { style.InactiveColor = inactive; }
        if (a.Background is SixelColor background) { style.BackgroundColor = background; }
        return style;
    }

    private static int RunFileDialog(UiCliArguments a, UiCliEnvironment env, bool save)
    {
        if (!env.IsOutputTerminal())
        {
            env.Error.WriteLine("The open and save commands need a terminal (output must not be redirected).");
            return 1;
        }

        var widgets = WidgetEnvironment.Create(a.Mode, env.QueryTerminal(), useAnsi: !a.NoAnsi, columns: env.Columns());
        PromptStyle style = CreatePromptStyle(a);
        FileDialog dialog = save ? new SaveFileDialog(style) : new OpenFileDialog(style);
        dialog.Title = a.Caption ?? (save ? "Save file" : "Open file");
        dialog.InitialDirectory = a.StartDirectory;
        dialog.Filter = a.Filter;

        try
        {
            bool mouse = !a.NoMouse && env.ReadInput != null && env.EnterInputMode != null;
            using (mouse ? env.EnterInputMode!() : env.EnterOutputMode())
            {
                string path = mouse ? dialog.ShowWithMouse(env.Output, widgets, env.ReadInput!) : dialog.Show(env.Output, widgets, env.ReadKey);
                env.Output.WriteLine((save ? "Save as: " : "Selected: ") + path);
            }

            return 0;
        }
        catch (OperationCanceledException)
        {
            env.Error.WriteLine("Cancelled.");
            return 1;
        }
        catch (InvalidOperationException ex)
        {
            env.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int RunPrompts(UiCliArguments a, UiCliEnvironment env)
    {
        if (!env.IsOutputTerminal())
        {
            env.Error.WriteLine("The prompts command needs a terminal (output must not be redirected).");
            return 1;
        }

        var widgets = WidgetEnvironment.Create(a.Mode, env.QueryTerminal(), useAnsi: !a.NoAnsi, columns: env.Columns());
        PromptStyle style = CreatePromptStyle(a);

        try
        {
            bool mouse = !a.NoMouse && env.ReadInput != null && env.EnterInputMode != null;
            using (mouse ? env.EnterInputMode!() : env.EnterOutputMode())
            {
                T Ask<T>(Prompt<T> prompt) =>
                    mouse ? prompt.ShowWithMouse(env.Output, widgets, env.ReadInput!) : prompt.Show(env.Output, widgets, env.ReadKey);

                var intro = new Dialog(style) { Title = "Prompts demo", Message = "Selection, multi-selection, text and password prompts follow.\nLeft/Right and Enter choose a button." };
                intro.SetButtons("Start", "Cancel");
                if (Ask(intro) != 0)
                {
                    return 0;
                }

                var fruit = new SelectionPrompt<string>(style) { Title = "Favorite fruit?" };
                fruit.AddChoices(new[] { "Apples", "Pears", "Cherries", "Plums" });
                string chosen = Ask(fruit);
                env.Output.WriteLine("Selected: " + chosen);

                var toppings = new MultiSelectionPrompt<string>(style) { Title = "Toppings (1-3)?", MinSelected = 1, MaxSelected = 3 };
                toppings.AddChoice("Cream", selected: true);
                toppings.AddChoice("Sugar");
                toppings.AddChoice("Cinnamon");
                toppings.AddChoice("Nuts");
                env.Output.WriteLine("Selected: " + string.Join(", ", Ask(toppings)));

                var name = new TextPrompt(style)
                {
                    Title = "Your name?",
                    MinLength = 2,
                    MaxLength = 30,
                    Pattern = new System.Text.RegularExpressions.Regex(@"^[\p{L} .'-]+$"),
                    PatternMessage = "Letters, spaces and . ' - only.",
                };
                env.Output.WriteLine("Hello, " + Ask(name) + "!");

                var secret = new TextPrompt(style) { Title = "Password (min. 4)?", IsSecret = true, MinLength = 4 };
                env.Output.WriteLine("Password length: " + Ask(secret).Length);
            }

            return 0;
        }
        catch (OperationCanceledException)
        {
            env.Error.WriteLine("Cancelled.");
            return 1;
        }
        catch (InvalidOperationException ex)
        {
            env.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void Execute(UiCliArguments arguments, UiCliEnvironment env)
    {
        bool live = env.IsOutputTerminal();
        var widgets = WidgetEnvironment.Create(
            live ? arguments.Mode : RenderMode.Ascii,
            live ? env.QueryTerminal() : null,
            useAnsi: live && !arguments.NoAnsi, columns: live ? env.Columns() : 0);

        using (live ? env.EnterTerminalMode() : null)
        {
            if (live)
            {
                env.Output.Write(HideCursor);
            }

            try
            {
                string command = arguments.Command!;
                bool all = command == "demo";
                if (all || command == "progress")
                {
                    RunProgress(arguments, widgets, env, live, all ? "Download" : null);
                }

                if ((all || command == "spinner") && !(live && env.IsCancelled()))
                {
                    RunSpinner(arguments, widgets, env, live, all ? "Loading" : null);
                }

                if ((all || command == "status") && !(live && env.IsCancelled()))
                {
                    RunStatus(arguments, widgets, env, live);
                }

                if ((all || command == "chart") && !(live && env.IsCancelled()))
                {
                    RunChart(arguments, widgets, env, live, all ? "Fruit sales" : null);
                }

                if ((all || command == "table") && !(live && env.IsCancelled()))
                {
                    RunTable(arguments, widgets, env, live, all ? "Fruit stock" : null);
                }

                if ((all || command == "button") && !(live && env.IsCancelled()))
                {
                    RunButton(arguments, widgets, env, live);
                }

                if ((all || command == "panel") && !(live && env.IsCancelled()))
                {
                    RunPanel(arguments, widgets, env, live);
                }

                if ((all || command == "clock") && !(live && env.IsCancelled()))
                {
                    RunClock(arguments, widgets, env, live);
                }

                if ((all || command == "date") && !(live && env.IsCancelled()))
                {
                    RunDate(arguments, widgets, env, live);
                }

                if ((all || command == "tree") && !(live && env.IsCancelled()))
                {
                    RunTree(arguments, widgets, env, live);
                }
            }
            finally
            {
                if (live)
                {
                    env.Output.Write(ShowCursor);
                    env.Output.Flush();
                }
            }
        }
    }

    private static void RunProgress(UiCliArguments a, WidgetEnvironment widgets, UiCliEnvironment env, bool live, string? defaultCaption)
    {
        var bar = new ProgressBar { Caption = a.Caption ?? defaultCaption, ProgressText = a.Text };
        ProgressBarStyle style = bar.Style;
        if (a.Width is int width) { style.Width = width; }
        if (a.Rows is int rows) { style.HeightRows = rows; }
        if (a.Radius is int radius) { style.CornerRadius = radius; }
        if (a.Fill is SixelColor fill) { style.FillColor = fill; }
        if (a.Track is SixelColor track) { style.TrackColor = track; }
        if (a.Border is SixelColor border) { style.BorderColor = border; }
        if (a.Background is SixelColor background) { style.BackgroundColor = background; }

        if (!live)
        {
            bar.Value = bar.Maximum;
            env.Output.WriteLine(bar.Render(widgets));
            return;
        }

        int steps = Math.Max(1, (int)(a.DurationSeconds * 1000 / ProgressFrameMs));
        using (var region = new LiveRegion(env.Output, widgets))
        {
            region.Begin(bar.GetRows(widgets));
            for (int step = 0; step <= steps && !env.IsCancelled(); step++)
            {
                bar.Value = bar.Maximum * step / steps;
                region.Update(bar);
                env.Delay(ProgressFrameMs);
            }
        }
    }

    private static void RunChart(UiCliArguments a, WidgetEnvironment widgets, UiCliEnvironment env, bool live, string? defaultCaption)
    {
        var chart = new BarChart { Caption = a.Caption ?? defaultCaption };
        BarChartStyle style = chart.Style;
        if (a.Width is int width) { style.Width = width; }
        if (a.Rows is int rows) { style.BarHeightRows = rows; }
        if (a.Radius is int radius) { style.CornerRadius = radius; }
        if (a.Track is SixelColor track) { style.TrackColor = track; }
        if (a.Background is SixelColor background) { style.BackgroundColor = background; }

        var items = new List<BarChartItem>();
        foreach (KeyValuePair<string, double> entry in ChartData)
        {
            items.Add(chart.Add(entry.Key, live ? 0 : entry.Value));
        }

        if (!live)
        {
            env.Output.WriteLine(chart.Render(widgets).Replace("\n", env.Output.NewLine));
            return;
        }

        int steps = Math.Max(1, (int)(a.DurationSeconds * 1000 / ChartFrameMs));
        using (var region = new LiveRegion(env.Output, widgets))
        {
            region.Begin(chart.GetRows(widgets));
            for (int step = 0; step <= steps && !env.IsCancelled(); step++)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].Value = ChartData[i].Value * step / steps;
                }

                region.Update(chart);
                env.Delay(ChartFrameMs);
            }
        }
    }

    private static void RunTable(UiCliArguments a, WidgetEnvironment widgets, UiCliEnvironment env, bool live, string? defaultCaption)
    {
        var table = new Table { Caption = a.Caption ?? defaultCaption };
        TableStyle style = table.Style;
        if (a.Border is SixelColor border) { style.BorderColor = border; }
        if (a.Active is SixelColor active) { style.HeaderTextColor = active; }
        if (a.Inactive is SixelColor inactive) { style.HeaderBackgroundColor = inactive; }
        if (a.Track is SixelColor stripes) { style.StripeColor = stripes; }

        table.AddColumn("Fruit");
        table.AddColumn("Quantity", TableAlignment.Right);
        table.AddColumn("Price", TableAlignment.Right);
        foreach (KeyValuePair<string, double> entry in ChartData)
        {
            table.AddRow(entry.Key, entry.Value.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                (entry.Value / 10).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
        }

        if (!live)
        {
            env.Output.WriteLine(table.Render(widgets).Replace("\n", env.Output.NewLine));
            return;
        }

        using (var region = new LiveRegion(env.Output, widgets))
        {
            region.Begin(table.GetRows(widgets));
            region.Update(table);
        }
    }

    private static void RunPanel(UiCliArguments a, WidgetEnvironment widgets, UiCliEnvironment env, bool live)
    {
        var panel = new Panel { Title = a.Caption ?? "Status" };
        PanelStyle style = panel.Style;
        if (a.Width is int width) { panel.Width = width; }
        if (a.Radius is int radius) { style.CornerRadius = radius; }
        if (a.Border is SixelColor border) { style.BorderColor = border; }
        if (a.Active is SixelColor active) { style.TitleColor = active; }
        if (a.Background is SixelColor background) { style.BackgroundColor = background; }

        panel.AddLine("Server: online");
        panel.AddLine("Files: 42");
        panel.AddLine("Errors: 0");

        if (!live)
        {
            env.Output.WriteLine(panel.Render(widgets).Replace("\n", env.Output.NewLine));
            return;
        }

        using (var region = new LiveRegion(env.Output, widgets))
        {
            region.Begin(panel.GetRows(widgets));
            region.Update(panel);
        }
    }

    private static void RunClock(UiCliArguments a, WidgetEnvironment widgets, UiCliEnvironment env, bool live)
    {
        var clock = new Clock { Caption = a.Caption, Time = env.Now() };
        ClockStyle style = clock.Style;
        if (a.Rows is int rows) { style.HeightRows = rows; }
        if (a.Border is SixelColor border) { style.BorderColor = border; }
        if (a.Fill is SixelColor fill) { style.FaceColor = fill; }
        if (a.Active is SixelColor active) { style.HandColor = active; }
        if (a.Background is SixelColor background) { style.BackgroundColor = background; }

        if (!live)
        {
            env.Output.WriteLine(clock.Render(widgets));
            return;
        }

        int steps = Math.Max(1, (int)(a.DurationSeconds * 1000 / ClockFrameMs));
        long shown = -1;
        using (var region = new LiveRegion(env.Output, widgets))
        {
            region.Begin(clock.GetRows(widgets));
            for (int step = 0; step < steps && !env.IsCancelled(); step++)
            {
                // The clock shows whole seconds: redraw only when the second changes (erase + redraw flickers).
                DateTime now = env.Now();
                long second = now.Ticks / TimeSpan.TicksPerSecond;
                if (second != shown)
                {
                    shown = second;
                    clock.Time = now;
                    region.Update(clock);
                }

                env.Delay(ClockFrameMs);
            }
        }
    }

    private static void RunDate(UiCliArguments a, WidgetEnvironment widgets, UiCliEnvironment env, bool live)
    {
        var date = new DateDisplay { Caption = a.Caption, Date = env.Now() };
        date.Style.Culture = env.Culture;
        DateDisplayStyle style = date.Style;
        if (a.Rows is int rows) { style.HeightRows = rows; }
        if (a.Radius is int radius) { style.CornerRadius = radius; }
        if (a.Border is SixelColor border) { style.BorderColor = border; }
        if (a.Fill is SixelColor fill) { style.PaperColor = fill; }
        if (a.Active is SixelColor active) { style.DigitColor = active; }
        if (a.Background is SixelColor background) { style.BackgroundColor = background; }

        if (!live)
        {
            env.Output.WriteLine(date.Render(widgets));
            return;
        }

        using (var region = new LiveRegion(env.Output, widgets))
        {
            region.Begin(date.GetRows(widgets));
            region.Update(date);
        }
    }

    private static void RunTree(UiCliArguments a, WidgetEnvironment widgets, UiCliEnvironment env, bool live)
    {
        var tree = new Tree { Title = a.Caption ?? "Files" };
        TreeStyle style = tree.Style;
        if (a.Border is SixelColor border) { style.GuideColor = border; }
        if (a.Active is SixelColor active) { style.TitleColor = active; }

        TreeNode source = tree.AddNode("src");
        source.AddNode("Program.cs");
        TreeNode ui = source.AddNode("Ui");
        ui.AddNode("Tree.cs");
        ui.AddNode("Clock.cs");
        tree.AddNode("docs").AddNode("README.md");
        TreeNode tests = tree.AddNode("tests");
        tests.AddNode("TreeTests.cs");
        tests.Expanded = false;

        if (!live)
        {
            env.Output.WriteLine(tree.Render(widgets).Replace("\n", env.Output.NewLine));
            return;
        }

        using (var region = new LiveRegion(env.Output, widgets))
        {
            region.Begin(tree.GetRows(widgets));
            region.Update(tree);
        }
    }

    private static Button CreateButton(UiCliArguments a)
    {
        var button = new Button(a.Caption ?? "OK");
        ButtonStyle style = button.Style;
        if (a.Width is int width) { style.MinWidth = width; }
        if (a.Rows is int rows) { style.HeightRows = rows; }
        if (a.Radius is int radius) { style.CornerRadius = radius; }
        if (a.Inactive is SixelColor normal) { style.NormalColor = normal; }
        if (a.Active is SixelColor focused) { style.FocusedColor = focused; }
        if (a.Background is SixelColor background) { style.BackgroundColor = background; }
        return button;
    }

    private static int RunButtonClick(UiCliArguments a, UiCliEnvironment env)
    {
        if (!env.IsOutputTerminal() || env.ReadInput == null || env.EnterInputMode == null)
        {
            env.Error.WriteLine("The --click option needs a terminal (output and input must not be redirected).");
            return 1;
        }

        var widgets = WidgetEnvironment.Create(a.Mode, env.QueryTerminal(), useAnsi: !a.NoAnsi, columns: env.Columns());
        Button button = CreateButton(a);
        bool clicked = false;
        bool cancelled = false;
        try
        {
            using (env.EnterInputMode())
            using (var region = new LiveRegion(env.Output, widgets))
            {
                region.Begin(button.GetRows(widgets));
                if (!a.NoMouse)
                {
                    region.EnableMouse(env.ReadInput);
                }

                button.State = ButtonState.Focused;
                region.Update(button);
                bool pressed = false;
                while (!clicked && !cancelled)
                {
                    InputEvent input = env.ReadInput();
                    if (input.Kind == InputKind.Key)
                    {
                        ConsoleKey key = input.Key.Key;
                        clicked = key == ConsoleKey.Enter || key == ConsoleKey.Spacebar;
                        cancelled = key == ConsoleKey.Escape;
                        button.State = clicked ? ButtonState.Pressed : ButtonState.Focused;
                    }
                    else if (input.Mouse is MouseEvent mouse && mouse.Button == MouseButton.Left)
                    {
                        bool inside = region.TryGetLocal(mouse, out int column, out int row) && button.Contains(widgets, column, row);
                        if (mouse.IsPress)
                        {
                            pressed = inside;
                        }
                        else
                        {
                            clicked = pressed && inside;
                            pressed = false;
                        }

                        button.State = pressed || clicked ? ButtonState.Pressed : ButtonState.Focused;
                    }

                    region.Update(button);
                }
            }
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }

        env.Output.WriteLine(clicked ? "Clicked." : "Cancelled.");
        return clicked ? 0 : 1;
    }

    private static void RunButton(UiCliArguments a, WidgetEnvironment widgets, UiCliEnvironment env, bool live)
    {
        Button button = CreateButton(a);
        ButtonState[] states = { ButtonState.Normal, ButtonState.Focused, ButtonState.Pressed, ButtonState.Disabled };
        if (!live)
        {
            var lines = new List<string>();
            foreach (ButtonState state in states)
            {
                button.State = state;
                lines.Add(button.Render(widgets));
            }

            env.Output.WriteLine(string.Join(env.Output.NewLine, lines));
            return;
        }

        int stateSteps = Math.Max(1, (int)(a.DurationSeconds * 1000 / ButtonFrameMs) / states.Length);
        using (var region = new LiveRegion(env.Output, widgets))
        {
            region.Begin(button.GetRows(widgets));
            foreach (ButtonState state in states)
            {
                button.State = state;
                region.Update(button);
                for (int step = 0; step < stateSteps && !env.IsCancelled(); step++)
                {
                    env.Delay(ButtonFrameMs);
                }
            }
        }
    }

    private static void RunStatus(UiCliArguments a, WidgetEnvironment widgets, UiCliEnvironment env, bool live)
    {
        string[] stages = a.Caption != null
            ? new[] { a.Caption }
            : new[] { "Loading configuration", "Connecting to server", "Fetching data", "Processing records" };
        var status = new StatusDisplay(env.Output, widgets);
        SpinnerStyle style = status.SpinnerStyle;
        if (a.Rows is int rows) { style.HeightRows = rows; }
        if (a.Active is SixelColor active) { style.ActiveColor = active; }
        if (a.Inactive is SixelColor inactive) { style.InactiveColor = inactive; }
        if (a.Background is SixelColor background) { style.BackgroundColor = background; }

        int stageSteps = Math.Max(1, (int)(a.DurationSeconds * 1000 / SpinnerFrameMs) / stages.Length);
        using (status)
        {
            status.Start(stages[0]);
            for (int i = 0; i < stages.Length && !(live && env.IsCancelled()); i++)
            {
                if (i > 0)
                {
                    status.Text = stages[i];
                }

                for (int step = 0; live && step < stageSteps && !env.IsCancelled(); step++)
                {
                    env.Delay(SpinnerFrameMs);
                }

                status.WriteLine(stages[i] + " done");
            }
        }
    }

    private static void RunSpinner(UiCliArguments a, WidgetEnvironment widgets, UiCliEnvironment env, bool live, string? defaultCaption)
    {
        var spinner = new Spinner { Caption = a.Caption ?? defaultCaption };
        SpinnerStyle style = spinner.Style;
        if (a.Rows is int rows) { style.HeightRows = rows; }
        if (a.Active is SixelColor active) { style.ActiveColor = active; }
        if (a.Inactive is SixelColor inactive) { style.InactiveColor = inactive; }
        if (a.Background is SixelColor background) { style.BackgroundColor = background; }

        if (!live)
        {
            env.Output.WriteLine(spinner.Render(widgets));
            return;
        }

        int steps = Math.Max(1, (int)(a.DurationSeconds * 1000 / SpinnerFrameMs));
        using (var region = new LiveRegion(env.Output, widgets))
        {
            region.Begin(spinner.GetRows(widgets));
            for (int step = 0; step < steps && !env.IsCancelled(); step++)
            {
                region.Update(spinner);
                spinner.Advance();
                env.Delay(SpinnerFrameMs);
            }

            region.End(clear: true);
        }
    }
}
