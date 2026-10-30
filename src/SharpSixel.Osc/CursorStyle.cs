namespace SharpSixel.Osc;

/// <summary>Cursor styles (DECSCUSR), compatible with Windows Terminal.</summary>
public enum CursorStyle
{
    /// <summary>The default style of the terminal.</summary>
    Default = 0,

    /// <summary>Blinking block.</summary>
    BoxBlinking = 1,

    /// <summary>Steady block.</summary>
    BoxStatic = 2,

    /// <summary>Blinking underscore.</summary>
    UnderscoreBlinking = 3,

    /// <summary>Steady underscore.</summary>
    UnderscoreStatic = 4,

    /// <summary>Blinking vertical bar.</summary>
    BarBlinking = 5,

    /// <summary>Steady vertical bar.</summary>
    BarStatic = 6,
}
