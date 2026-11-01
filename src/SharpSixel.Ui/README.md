# SharpSixel.Ui

Terminal UI widgets drawn as sixel graphics, with automatic ASCII fallback. No dependencies except `SharpSixel`.

  `ProgressBar` (optional caption and percent/fraction text)
  `Spinner` and `StatusDisplay`: spinner with changeable status text for work of unknown duration (`Start`, `Text`, `WriteLine`, `Refresh`, `Stop`; timer animation or manual refresh)
- `BarChart`: horizontal bar chart with labels, values, per-item colors and optional caption (`Add`, `Items`, `Maximum`, `ShowValues`; one ASCII line per bar)
- `Table`: table with column headers, aligned text cells and a grid, optional caption (`AddColumn`, `AddRow`, `Columns`, `Rows`; `TableColumn.Width`, `TableAlignment`; sixel: pixel-line grid, ASCII: `+ - |` grid; same rows in both modes)
- `Button`: label with a visual `State` (`Normal`, `Focused`, `Pressed`, `Disabled`); sixel: rounded face per state with the label on it, ASCII: `[ OK ]` with state markers (`[>OK<]`, `[#OK#]`, `[-OK-]`); draws only, the caller changes `State` between frames
- `Panel`: framed box around text lines with an optional title in the top edge (`AddLine`, `Lines`, `Title`, `Width`; sixel: rounded outline graphic, optional inside fill via `PanelStyle.BackgroundColor`, ASCII: `+ - |` frame; same rows in both modes: top edge, one row per line, bottom edge)
- `Clock`: time of day (`Time`, set by the caller before each frame; optional `Caption`); sixel: square analog clock (rim, 12 hour marks, hour/minute hand, optional second hand via `ClockStyle.ShowSecondHand`) followed by the digital time as text (`ClockStyle.TimeFormat`, default `HH:mm:ss`), ASCII: the digital time only
- `DateDisplay`: date (`Date`, set by the caller; optional `Caption`); sixel: square calendar page with the day number in seven-segment digits followed by the date text (`DateDisplayStyle.DateFormat`, default `ddd, dd MMM yyyy`), ASCII: the date text only
- `Tree`: hierarchical list of `TreeNode`s (`Text`, `Expanded`, `Nodes`) with an optional `Title` as root row (`AddNode`, `Nodes`, `Title`; collapsed nodes hide their children and get `TreeStyle.CollapsedSuffix`, default ` [+]`); sixel: guide lines as pixel lines (one image, own 4-cell columns per level) with the node texts as terminal text after them, ASCII: `+-- ` / `` `-- `` / `|` guides; same rows in both modes: title, then one row per visible node
- `Rule`: separator line, horizontal (`Width`, default terminal width; optional centered `Title`) or vertical (`Orientation`, `Height`); sixel: pixel line, ASCII: `---` / `|`
- `CheckBox` and `RadioGroup` (`ToggleStyle`): label with a rounded box / options with rings; `Checked`/`SelectedIndex` and `Focused`/`FocusedIndex` are set by the caller, `Enabled`, `Contains`/`IndexAt` for mouse clicks; sixel: same markers as the prompts, ASCII: `[x]`, `(*)` with a pointer on the focused row
- `HorizontalLayout`: widgets side by side (`Add`, `Children`, `Spacing`); every child draws its own frame shifted to its column, width via `Widget.GetColumns`, rows of the highest child
- `ImageView`: shows an image in a view of fixed size (`Columns` x `Rows` cells) with optional frame and a background independent of the terminal background (`ImageViewStyle`: `ShowBorder`, `BorderColor`, `BorderThickness`, `BackgroundColor`). The image is fitted into the view automatically (aspect ratio kept, centered, `AllowUpscale`); pass it as graphic (`Image = SixelImage`) or load it from file/stream/bytes (`Load`; BMP and sixel built in, other formats via `Decoder`, e.g. `SkiaImageCodec.Decode`). ASCII fallback: one pixel per cell (character by brightness, ANSI color)
- Prompts (interactive, key driven, sixel markers or ASCII, `Show` returns the validated answer): `Dialog` (message with buttons, returns the button index), `SelectionPrompt<T>` (one item), `MultiSelectionPrompt<T>` (any number; `MinSelected`/`MaxSelected`) and `TextPrompt` (`AllowEmpty`, `MinLength`, `MaxLength`, `Pattern`, `DefaultValue`, `IsSecret`); all accept a custom `Validator` returning a `ValidationResult`
- File dialogs (prompts that browse the file system and return the full path of the chosen file; nothing is written): `OpenFileDialog` (choose an existing file) and `SaveFileDialog` (name field, `FileName`, `ConfirmOverwrite`); common options `InitialDirectory`, `Filter` (e.g. `*.bmp;*.sixel`), `ShowHidden`, `PageSize`, `Width`, `Instructions`; Esc cancels with an `OperationCanceledException`
- `WidgetEnvironment` selects sixel or ASCII (`RenderMode.Auto` uses the terminal's sixel support)
- `LiveRegion` redraws a widget in place (ANSI cursor save/restore, scroll safe); `EnableMouse`/`TryGetLocal` map mouse reports to region coordinates
- Mouse: `ShowWithMouse(writer, env, readInput)` on the prompts (left click chooses a dialog button / selection item, toggles a multi-selection item, opens a directory / chooses a file in a file dialog, wheel moves the cursor), `VtInputParser` turns VT input characters into `InputEvent`s (keys, SGR mouse, cursor position), `Button.Contains` hit-tests a button; the terminal must deliver input in VT mode (not tested in every terminal)
- Styles (`ProgressBarStyle`, `SpinnerStyle`, `BarChartStyle`, `TableStyle`, `ButtonStyle`, `PanelStyle`, `ClockStyle`, `DateDisplayStyle`, `TreeStyle`, `RuleStyle`, `ToggleStyle`, `ImageViewStyle`, `PromptStyle`) customize colors, size, border and ASCII characters

```csharp
var info = SixelTerminal.Query(Console.Out, readChar);          // from SharpSixel
var env = WidgetEnvironment.Create(RenderMode.Auto, info);
var bar = new ProgressBar { Caption = "Download" };
using (var region = new LiveRegion(Console.Out, env))
{
    region.Begin(bar.GetRows(env));
    for (int i = 0; i <= 100; i++) { bar.Value = i; region.Update(bar); }
}
```

```csharp
var chart = new BarChart { Caption = "Sales" };
chart.Add("Apples", 42);
chart.Add("Pears", 28, new SixelColor(231, 76, 60));
using (var region = new LiveRegion(Console.Out, env))
{
    region.Begin(chart.GetRows(env));
    chart.Items[0].Value = 50;       // values can change between frames
    region.Update(chart);
}
```

```csharp
var table = new Table { Caption = "Stock" };
table.AddColumn("Fruit");
table.AddColumn("Quantity", TableAlignment.Right, width: 8);   // fixed width keeps the size stable
string[] apples = table.AddRow("Apples", "42");
table.AddRow("Pears", "28");
using (var region = new LiveRegion(Console.Out, env))
{
    region.Begin(table.GetRows(env));
    apples[1] = "50";                // cells can change between frames
    region.Update(table);
}
```

```csharp
var button = new Button("Save") { State = ButtonState.Focused };
button.Style.MinWidth = 12;
using (var region = new LiveRegion(Console.Out, env))
{
    region.Begin(button.GetRows(env));
    region.Update(button);
    button.State = ButtonState.Pressed;     // state changes are drawn with the next Update
    region.Update(button);
}
```

```csharp
var panel = new Panel { Title = "Status", Width = 20 };   // fixed width keeps the size stable
panel.AddLine("Connected");
panel.AddLine("Files: 0");
using (var region = new LiveRegion(Console.Out, env))
{
    region.Begin(panel.GetRows(env));
    panel.Lines[1] = "Files: 42";   // lines can change between frames, their number must not grow
    region.Update(panel);
}
```

```csharp
var clock = new Clock { Caption = "Time" };   // DateDisplay works the same way with Date
using (var region = new LiveRegion(Console.Out, env))
{
    region.Begin(clock.GetRows(env));
    clock.Time = DateTime.Now;   // the widget does not tick by itself
    region.Update(clock);
}
```

```csharp
var tree = new Tree { Title = "Files" };
TreeNode src = tree.AddNode("src");
src.AddNode("Program.cs");
TreeNode tests = tree.AddNode("tests");
tests.AddNode("TreeTests.cs");
tests.Expanded = false;          // shown as "tests [+]", children hidden
using (var region = new LiveRegion(Console.Out, env))
{
    region.Begin(tree.GetRows(env));   // rows = title + visible nodes, must not grow later
    region.Update(tree);
}
```

```csharp
using (var status = new StatusDisplay(Console.Out, env))
{
    status.Start("Loading...");
    status.Text = "Connecting...";
    status.WriteLine("Config loaded");   // printed above the spinner
}
```

```csharp
var view = new ImageView(40, 10, new ImageViewStyle { ShowBorder = true, BackgroundColor = new SixelColor(20, 20, 30) });
view.Load("logo.bmp");                       // or view.Image = someSixelImage;
view.Decoder = SkiaImageCodec.Decode;        // optional: PNG/JPEG via SharpSixel.Skia
view.Load("photo.jpg");                      // fitted into 40 x 10 cells automatically
Console.WriteLine(view.Render(env));         // ASCII; in sixel mode present it via LiveRegion
```

```csharp
var dialog = new Dialog { Title = "Delete file?", Message = "report.txt will be removed.\nThis cannot be undone." };
dialog.SetButtons("Delete", "Cancel");
dialog.DefaultButton = 1;
bool delete = dialog.Show(Console.Out, env) == 0;                // Left/Right/Tab, Enter

var fruit = new SelectionPrompt<string> { Title = "Favorite fruit?" };
fruit.AddChoices(new[] { "Apples", "Pears", "Plums" });
string one = fruit.Show(Console.Out, env);                      // Up/Down, Enter

var toppings = new MultiSelectionPrompt<string> { Title = "Toppings?", MinSelected = 1, MaxSelected = 3 };
toppings.AddChoice("Cream", selected: true);
toppings.AddChoice("Sugar");
IReadOnlyList<string> many = toppings.Show(Console.Out, env);   // Space toggles, Enter confirms

var name = new TextPrompt
{
    Title = "Name?",
    MinLength = 2,
    Pattern = new Regex("^[A-Za-z ]+$"),
    Validator = v => v == "root" ? ValidationResult.Error("Reserved name.") : ValidationResult.Success(),
};
string text = name.Show(Console.Out, env, readKey: () => Console.ReadKey(true));

var open = new OpenFileDialog { Title = "Open image", InitialDirectory = @"C:\Images", Filter = "*.bmp;*.sixel" };
string path = open.Show(Console.Out, env);                      // full path; Esc throws OperationCanceledException

var save = new SaveFileDialog { Title = "Save image", FileName = "image.sixel" };
string target = save.Show(Console.Out, env);                    // chooses a path, writes nothing
```

Prompts read keys from `Console.ReadKey(true)` unless a `readKey` function is given (tests, other input sources). Invalid input shows its message below the prompt and the user corrects it. The prompt is removed after confirmation and one summary line (title and answer) is printed. Without ANSI and sixel support a plain dialog is used (numbered choices, typed line). Labels and typed text should fit on one terminal row.

In sixel mode the prompts are drawn with a rounded frame around them (`PromptStyle.ShowFrame`, `FrameColor`, `CornerRadius`), a highlight bar behind the current row (`HighlightColor`, transparent turns it off), larger markers, check boxes with a check mark and dialog buttons as rounded faces (`ButtonColor`, `ButtonTextColor`; the selected button uses `ActiveColor`); an opaque `BackgroundColor` fills the prompt as a panel. The ASCII look is unchanged.

The file dialogs show the current directory, then `PageSize` list rows (`..`, directories, files, sorted by name; on Windows `..` above a drive root lists the drives) and, for `SaveFileDialog`, a name field. Up/Down, Home/End and PageUp/PageDown move, Enter opens a directory or chooses a file, Esc cancels. In the save dialog typing edits the name (Enter confirms it in the shown directory), Enter on a listed file copies its name, and an existing file needs a second Enter while `ConfirmOverwrite` is true (default). Names longer than `Width` are shortened with an ellipsis. Without ANSI and sixel support the user types a file name or path (relative to `InitialDirectory`) instead; `Filter` does not apply then.
