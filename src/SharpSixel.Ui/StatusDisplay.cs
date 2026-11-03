using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SharpSixel.Ui;

/// <summary>
/// Animated spinner with a changeable status text for work of unknown duration. Between <see cref="Start"/> and
/// <see cref="Stop"/> the spinner is drawn in place (sixel or ASCII, see <see cref="WidgetEnvironment"/>) and removed
/// afterwards. Without ANSI/sixel support each status text is printed as a plain line instead. Members are thread safe.
/// Do not write to the output directly while running (use <see cref="WriteLine"/>) and do not combine with other live regions.
/// </summary>
public sealed class StatusDisplay : IDisposable
{
    /// <summary>Smallest allowed refresh interval in milliseconds.</summary>
    public const int MinRefreshIntervalMs = 20;

    private const long MaxCatchUpFrames = 1000;
    private const long MinTickMs = 10;

    private readonly object _sync = new object();
    private readonly TextWriter _writer;
    private readonly WidgetEnvironment _environment;
    private readonly Spinner _spinner = new Spinner();
    private readonly Func<long> _clock;
    private readonly bool _animated;
    private TimeSpan _refreshInterval = TimeSpan.FromMilliseconds(100);
    private LiveRegion? _region;
    private Timer? _timer;
    private long _intervalMs;
    private long _startMs;
    private long _frames;
    private int _rows;
    private bool _running;
    private bool _dirty;

    /// <summary>Creates a status display on the given output.</summary>
    public StatusDisplay(TextWriter writer, WidgetEnvironment environment)
        : this(writer, environment, CreateClock())
    {
    }

    /// <summary>
    /// Optional source of the current terminal width in cells (0 = unknown), set before the status is started: if the terminal is
    /// resized the display adapts to the new width (see <see cref="LiveRegion.Resize"/>).
    /// </summary>
    public Func<int>? Columns { get; set; }

    internal StatusDisplay(TextWriter writer, WidgetEnvironment environment, Func<long> clock)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _animated = environment.UseSixel || environment.UseAnsi;
    }

    /// <summary>
    /// Look of the spinner (colors, size, ASCII frames); colors may change while running. The height in rows must not
    /// grow after <see cref="Start"/> (larger frames are skipped).
    /// </summary>
    public SpinnerStyle SpinnerStyle => _spinner.Style;

    /// <summary>True (default) to animate on a timer; false to redraw only on <see cref="Refresh"/>. Read by <see cref="Start"/>.</summary>
    public bool AutoRefresh { get; set; } = true;

    /// <summary>Time per animation step, at least <see cref="MinRefreshIntervalMs"/> ms (default 100 ms). Read by <see cref="Start"/>.</summary>
    public TimeSpan RefreshInterval
    {
        get => _refreshInterval;
        set
        {
            if (value.TotalMilliseconds < MinRefreshIntervalMs)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Interval must be at least " + MinRefreshIntervalMs + " ms.");
            }

            _refreshInterval = value;
        }
    }

    /// <summary>True between <see cref="Start"/> and <see cref="Stop"/>.</summary>
    public bool IsRunning
    {
        get
        {
            lock (_sync)
            {
                return _running;
            }
        }
    }

    /// <summary>
    /// Single-line status text shown next to the spinner. Without <see cref="AutoRefresh"/> it appears on the next
    /// <see cref="Refresh"/>. Without ANSI/sixel support every change while running is printed as a new line.
    /// </summary>
    public string Text
    {
        get
        {
            lock (_sync)
            {
                return _spinner.Caption ?? string.Empty;
            }
        }

        set
        {
            lock (_sync)
            {
                _spinner.Caption = value ?? string.Empty;
                _dirty = true;
                if (_running && !_animated)
                {
                    PrintPlainLine();
                }
            }
        }
    }

    /// <summary>Reserves the rows, hides the cursor, draws the first frame and starts the animation timer if <see cref="AutoRefresh"/> is set.</summary>
    /// <param name="text">Initial status text.</param>
    public void Start(string text = "")
    {
        lock (_sync)
        {
            if (_running)
            {
                throw new InvalidOperationException("The status display is already running.");
            }

            _spinner.Caption = text ?? string.Empty;
            _intervalMs = (long)_refreshInterval.TotalMilliseconds;
            _startMs = _clock();
            _frames = 0;
            _running = true;
            if (!_animated)
            {
                PrintPlainLine();
                return;
            }

            try
            {
                _rows = _spinner.GetRows(_environment);
                _writer.Write(AnsiCodes.HideCursor);
                _region = new LiveRegion(_writer, _environment, Columns);
                _region.Begin(_rows);
                _dirty = true;
                Redraw(force: true);
            }
            catch
            {
                Stop();
                throw;
            }

            if (AutoRefresh)
            {
                long period = Math.Max(MinTickMs, _intervalMs / 2);
                _timer = new Timer(OnTick, null, period, period);
            }
        }
    }

    /// <summary>Redraws the status immediately (needed with <see cref="AutoRefresh"/> disabled). The spinner advances by the time passed.</summary>
    public void Refresh()
    {
        lock (_sync)
        {
            if (_running && _animated)
            {
                Redraw(force: true);
            }
        }
    }

    /// <summary>Writes a line above the status display, which stays below it.</summary>
    /// <param name="text">Line to write.</param>
    public void WriteLine(string? text)
    {
        lock (_sync)
        {
            bool live = _running && _animated;
            if (live)
            {
                _region!.End(clear: true);
            }

            _writer.WriteLine(text);
            if (live)
            {
                _region!.Begin(_rows);
                _dirty = true;
                Redraw(force: true);
            }

            _writer.Flush();
        }
    }

    /// <summary>Stops the animation and removes the status from the screen; the cursor is shown again. Safe to call repeatedly.</summary>
    public void Stop()
    {
        lock (_sync)
        {
            if (!_running)
            {
                return;
            }

            _running = false;
            _timer?.Dispose();
            _timer = null;
            if (_animated)
            {
                _region?.End(clear: true);
                _region = null;
                _writer.Write(AnsiCodes.ShowCursor);
                _writer.Flush();
            }
        }
    }

    /// <inheritdoc />
    public void Dispose() => Stop();

    private static Func<long> CreateClock()
    {
        Stopwatch watch = Stopwatch.StartNew();
        return () => watch.ElapsedMilliseconds;
    }

    private void OnTick(object? state)
    {
        lock (_sync)
        {
            if (!_running)
            {
                return;
            }

            try
            {
                Redraw(force: false);
            }
            catch (IOException)
            {
                // Output went away (closed console); an unhandled exception on a timer thread would end the process.
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    private void Redraw(bool force)
    {
        long due = (_clock() - _startMs) / _intervalMs;
        long steps = due - _frames;
        _frames = due;
        for (long i = Math.Min(steps, MaxCatchUpFrames); i > 0; i--)
        {
            _spinner.Advance();
        }

        if (!force && steps <= 0 && !_dirty)
        {
            return;
        }

        _dirty = false;
        if (_spinner.GetRows(_environment) > _rows)
        {
            return;
        }

        _region!.Update(_spinner);
    }

    private void PrintPlainLine()
    {
        _writer.WriteLine(_spinner.Render(_environment));
        _writer.Flush();
    }
}
