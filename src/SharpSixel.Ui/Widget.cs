using System;

namespace SharpSixel.Ui;

/// <summary>Base class of widgets: renders a frame either as sixel graphics or as ASCII, depending on the environment.</summary>
public abstract class Widget
{
    /// <summary>Number of terminal rows the widget occupies in the given environment.</summary>
    public int GetRows(WidgetEnvironment environment)
    {
        if (environment == null)
        {
            throw new ArgumentNullException(nameof(environment));
        }

        return environment.UseSixel ? SixelRows : AsciiRows;
    }

    /// <summary>
    /// Renders one frame. The frame starts at the cursor position (column 0 of the widget's first row).
    /// ASCII frames are text with one line per row (lines separated by '\n', none for single-row widgets); sixel frames
    /// contain cursor sequences and must be presented via <see cref="LiveRegion"/>, which also positions multi-row ASCII lines.
    /// </summary>
    public string Render(WidgetEnvironment environment)
    {
        if (environment == null)
        {
            throw new ArgumentNullException(nameof(environment));
        }

        return environment.UseSixel ? RenderSixel(environment) : RenderAscii(environment);
    }

    /// <summary>Rows occupied in sixel mode.</summary>
    protected abstract int SixelRows { get; }

    /// <summary>Rows (lines) occupied in ASCII mode. Default 1.</summary>
    protected virtual int AsciiRows => 1;

    /// <summary>Renders the ASCII frame.</summary>
    protected abstract string RenderAscii(WidgetEnvironment environment);

    /// <summary>Renders the sixel frame.</summary>
    protected abstract string RenderSixel(WidgetEnvironment environment);
}
