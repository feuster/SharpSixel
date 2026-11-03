using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpSixel.Ui;

/// <summary>
/// Base of <see cref="OpenFileDialog"/> and <see cref="SaveFileDialog"/>: browses the file system in a list and returns the
/// full path of the chosen file. The first row shows the current directory, below it the entries (parent <c>..</c>, then
/// directories, then files, each sorted by name; on Windows <c>..</c> at a drive root lists the drives). Up/Down (wrapping),
/// Home/End and PageUp/PageDown move, Enter opens a directory or chooses a file, Esc cancels by throwing an
/// <see cref="OperationCanceledException"/> (like Ctrl+C). The list always reserves <see cref="PageSize"/> rows so the number
/// of rows stays constant; longer lists scroll. Names longer than <see cref="Width"/> are shortened with an ellipsis. The
/// current entry has a radio marker (sixel graphic or ASCII). The optional <see cref="Prompt{TResult}.Validator"/> can reject a
/// path. Without ANSI/sixel support there is no list: the user types a file name or path (relative to the start directory).
/// </summary>
public abstract class FileDialog : Prompt<string>
{
    private const string Ellipsis = "\u2026";
    private const string NameLabel = "File name: ";

    private readonly List<Entry> _entries = new List<Entry>();
    private readonly ChoiceNavigator _navigator = new ChoiceNavigator();
    private readonly StringBuilder _name = new StringBuilder();
    private List<Regex> _filters = new List<Regex>();
    private int _pageSize = 10;
    private int _width = 60;
    private string _directory = string.Empty;
    private string _result = string.Empty;
    private string? _overwritePending;
    private bool _fieldFocus;

    internal FileDialog(PromptStyle style, string instructions)
        : base(style)
    {
        Instructions = instructions;
    }

    /// <summary>Directory shown first; null or empty = the current directory. Must exist.</summary>
    public string? InitialDirectory { get; set; }

    /// <summary>
    /// Semicolon-separated wildcard patterns (<c>*</c>, <c>?</c>, case-insensitive) of the files to list, for example
    /// <c>*.bmp;*.sixel</c>. Null or empty lists all files. Directories are always listed. Not applied to typed paths (plain mode).
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>True to list hidden and system entries. Default false.</summary>
    public bool ShowHidden { get; set; }

    /// <summary>Number of list rows (at least 1). Default 10.</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Guard.AtLeast(value, 1, nameof(value));
    }

    /// <summary>Maximum text length of the directory, the entries and the typed file name in characters (at least 20). Default 60.</summary>
    public int Width
    {
        get => _width;
        set => _width = Guard.AtLeast(value, 20, nameof(value));
    }

    /// <summary>Hint line below the list; null or empty hides it.</summary>
    public string? Instructions { get; set; }

    internal virtual bool IsSave => false;

    internal virtual string InitialName => string.Empty;

    internal virtual bool ConfirmOverwriteEnabled => false;

    internal override int RowCount =>
        (HasTitle ? 1 : 0) + 1 + _pageSize + (IsSave ? 1 : 0) + (string.IsNullOrEmpty(Instructions) ? 0 : 1) + 1;

    internal override string Result => _result;

    internal override string Summary => _result;

    internal override void Begin()
    {
        _filters = ParseFilter(Filter);
        string start = string.IsNullOrEmpty(InitialDirectory) ? Directory.GetCurrentDirectory() : InitialDirectory!;
        string directory = Normalize(start);
        if (!Directory.Exists(directory))
        {
            throw new InvalidOperationException("The initial directory does not exist.");
        }

        if (!TryEnter(directory))
        {
            throw new InvalidOperationException("The initial directory cannot be read.");
        }

        _name.Clear();
        _name.Append(InitialName);
        _result = string.Empty;
        _overwritePending = null;
        _fieldFocus = IsSave;
    }

    internal override void BuildRows(List<PromptRow> rows)
    {
        AddTitleRow(rows);
        rows.Add(new PromptRow
        {
            Text = FitTail(_directory.Length == 0 ? "Drives" : _directory, _width),
            TextColor = Style.InactiveColor,
        });
        for (int i = 0; i < _pageSize; i++)
        {
            if (i >= _navigator.Visible)
            {
                rows.Add(new PromptRow());
                continue;
            }

            int index = _navigator.Top + i;
            bool current = index == _navigator.Cursor && !_fieldFocus;
            rows.Add(new PromptRow
            {
                AsciiPrefix = Style.AsciiPrefix(current, current ? Style.AsciiRadioOn : Style.AsciiRadioOff),
                Text = Fit(_entries[index].Label),
                Shape = MarkerShape.Radio,
                MarkerColor = current ? Style.ActiveColor : Style.InactiveColor,
                Filled = current,
                TextColor = current ? (SixelColor?)Style.ActiveColor : null,
                Bar = current,
            });
        }

        if (IsSave)
        {
            rows.Add(new PromptRow
            {
                Text = NameLabel + FitTail(_name.ToString(), _width - NameLabel.Length),
                Shape = MarkerShape.Dot,
                MarkerColor = Style.ActiveColor,
                Caret = _fieldFocus,
            });
        }

        AddHintRow(rows, Instructions);
        AddErrorRow(rows);
    }

    internal override bool OnKey(ConsoleKeyInfo key)
    {
        string? pending = TakeOverwritePending();
        Error = null;
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                throw new OperationCanceledException();
            case ConsoleKey.Enter:
                return _fieldFocus ? ConfirmName(pending) : Activate(pending);
            case ConsoleKey.Backspace:
                if (IsSave)
                {
                    _fieldFocus = true;
                    if (_name.Length > 0)
                    {
                        _name.Length--;
                    }
                }

                return false;
        }

        if (IsSave && !char.IsControl(key.KeyChar))
        {
            _fieldFocus = true;
            _name.Append(key.KeyChar);
        }
        else if (_entries.Count > 0 && _navigator.Handle(key))
        {
            _fieldFocus = false;
        }

        return false;
    }

    internal override bool OnMouse(int column, int row, WidgetEnvironment environment)
    {
        string? pending = TakeOverwritePending();
        Error = null;
        int first = HasTitle ? 2 : 1;
        int item = row - first;
        if (item >= 0 && item < _navigator.Visible)
        {
            _navigator.MoveTo(_navigator.Top + item);
            _fieldFocus = false;
            return Activate(pending);
        }

        if (IsSave && row == first + _pageSize)
        {
            _fieldFocus = true;
        }

        return false;
    }

    internal override string RunPlain(TextWriter writer, Func<ConsoleKeyInfo> readKey)
    {
        if (HasTitle)
        {
            writer.WriteLine(PromptRow.Clean(Title));
        }

        writer.WriteLine("  Directory: " + PromptRow.Clean(_directory));
        string defaultName = InitialName;
        string prompt = "File name or path"
            + (defaultName.Length > 0 ? " (" + PromptRow.Clean(defaultName) + ")" : string.Empty) + ": ";
        while (true)
        {
            writer.Write(prompt);
            writer.Flush();
            string line = LineReader.ReadLine(writer, readKey, null).Trim();
            if (line.Length == 0)
            {
                line = defaultName;
            }

            string? error;
            string path = string.Empty;
            if (line.Length == 0)
            {
                error = "Enter a file name or path.";
            }
            else if (!TryResolve(line, out path))
            {
                error = "Enter a valid path.";
            }
            else
            {
                error = ValidatePath(path);
                if (error == null && NeedsOverwriteConfirmation(path) && !AskOverwrite(writer, readKey))
                {
                    continue;
                }
            }

            if (error == null)
            {
                return path;
            }

            writer.WriteLine("! " + PromptRow.Clean(error));
        }
    }

    private static List<Regex> ParseFilter(string? filter)
    {
        var patterns = new List<Regex>();
        if (filter == null)
        {
            return patterns;
        }

        foreach (string part in filter.Split(';'))
        {
            string pattern = part.Trim();
            if (pattern.Length > 0)
            {
                string expression = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                patterns.Add(new Regex(expression, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline));
            }
        }

        return patterns;
    }

    private static string Normalize(string path)
    {
        string full = Path.GetFullPath(path);
        string root = Path.GetPathRoot(full) ?? string.Empty;
        return full.Length > root.Length ? full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) : full;
    }

    /// <summary>Parent directory; empty string (the drive list) above a Windows drive root; null if there is none.</summary>
    private static string? ParentOf(string directory)
    {
        DirectoryInfo? parent = Directory.GetParent(directory);
        if (parent != null)
        {
            return parent.FullName;
        }

        return Path.DirectorySeparatorChar == '\\' ? string.Empty : null;
    }

    private static int CompareLabels(Entry a, Entry b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase);

    private string? TakeOverwritePending()
    {
        string? pending = _overwritePending;
        _overwritePending = null;
        return pending;
    }

    private string Fit(string text) => TextFit.Clip(text, _width);

    private static string FitTail(string text, int width) =>
        TextFit.Width(text) <= width ? text : Ellipsis + TextFit.TakeLast(text, width - 1);

    private bool Matches(string name)
    {
        if (_filters.Count == 0)
        {
            return true;
        }

        foreach (Regex filter in _filters)
        {
            if (filter.IsMatch(name))
            {
                return true;
            }
        }

        return false;
    }

    private void ListEntries(string directory, List<Entry> entries)
    {
        if (directory.Length == 0)
        {
            foreach (string drive in Directory.GetLogicalDrives())
            {
                entries.Add(new Entry(drive, drive, true));
            }

            return;
        }

        string? parent = ParentOf(directory);
        if (parent != null)
        {
            entries.Add(new Entry(".." + Path.DirectorySeparatorChar, parent, true));
        }

        var directories = new List<Entry>();
        var files = new List<Entry>();
        foreach (FileSystemInfo item in new DirectoryInfo(directory).EnumerateFileSystemInfos())
        {
            FileAttributes attributes = item.Attributes;
            if (!ShowHidden && (attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
            {
                continue;
            }

            if ((attributes & FileAttributes.Directory) != 0)
            {
                directories.Add(new Entry(item.Name + Path.DirectorySeparatorChar, item.FullName, true));
            }
            else if (Matches(item.Name))
            {
                files.Add(new Entry(item.Name, item.FullName, false));
            }
        }

        directories.Sort(CompareLabels);
        files.Sort(CompareLabels);
        entries.AddRange(directories);
        entries.AddRange(files);
    }

    /// <summary>Lists <paramref name="directory"/> (empty = drives) and makes it current; on failure sets <see cref="Prompt{TResult}.Error"/> and stays.</summary>
    private bool TryEnter(string directory)
    {
        var entries = new List<Entry>();
        try
        {
            ListEntries(directory, entries);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            Error = "Cannot read the directory.";
            return false;
        }

        _entries.Clear();
        _entries.AddRange(entries);
        _directory = directory;
        _navigator.Reset(_entries.Count, _pageSize);
        return true;
    }

    /// <summary>Enter on the list: opens a directory or chooses a file (a save dialog also copies its name into the name field).</summary>
    private bool Activate(string? pending)
    {
        if (_entries.Count == 0)
        {
            return false;
        }

        Entry entry = _entries[_navigator.Cursor];
        if (entry.IsDirectory)
        {
            TryEnter(entry.Target);
            return false;
        }

        if (IsSave)
        {
            _name.Clear();
            _name.Append(entry.Label);
        }

        return Confirm(entry.Target, pending);
    }

    /// <summary>Enter in the name field of a save dialog.</summary>
    private bool ConfirmName(string? pending)
    {
        string name = _name.ToString().Trim();
        if (name.Length == 0)
        {
            Error = "Enter a file name.";
            return false;
        }

        if (_directory.Length == 0)
        {
            Error = "Open a directory first.";
            return false;
        }

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            Error = "Enter a valid file name.";
            return false;
        }

        return Confirm(Path.Combine(_directory, name), pending);
    }

    /// <summary>Built-in rules, custom validator, then the overwrite confirmation (a second identical confirmation accepts).</summary>
    private bool Confirm(string path, string? pending)
    {
        Error = ValidatePath(path);
        if (Error != null)
        {
            return false;
        }

        if (NeedsOverwriteConfirmation(path) && pending != path)
        {
            _overwritePending = path;
            Error = "File exists. Press Enter again to overwrite.";
            return false;
        }

        _result = path;
        return true;
    }

    private bool NeedsOverwriteConfirmation(string path) => IsSave && ConfirmOverwriteEnabled && File.Exists(path);

    private string? ValidatePath(string path)
    {
        if (IsSave)
        {
            if (Path.GetFileName(path).Length == 0)
            {
                return "Enter a file name.";
            }

            if (Directory.Exists(path))
            {
                return "A directory with this name exists.";
            }

            string? directory = Path.GetDirectoryName(path);
            if (directory == null || !Directory.Exists(directory))
            {
                return "The directory does not exist.";
            }
        }
        else if (!File.Exists(path))
        {
            return "The file does not exist.";
        }

        return Check(path);
    }

    private bool TryResolve(string text, out string path)
    {
        try
        {
            path = Path.GetFullPath(Path.Combine(_directory, text));
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
        {
            path = string.Empty;
            return false;
        }
    }

    private static bool AskOverwrite(TextWriter writer, Func<ConsoleKeyInfo> readKey)
    {
        writer.Write("File exists. Overwrite? (y/n): ");
        writer.Flush();
        return LineReader.ReadLine(writer, readKey, null).Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class Entry
    {
        public Entry(string label, string target, bool isDirectory)
        {
            Label = label;
            Target = target;
            IsDirectory = isDirectory;
        }

        public string Label { get; }

        public string Target { get; }

        public bool IsDirectory { get; }
    }
}
