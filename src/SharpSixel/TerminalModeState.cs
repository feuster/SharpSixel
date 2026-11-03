namespace SharpSixel;

/// <summary>State of a terminal mode as reported to a mode query (DECRQM, <c>CSI ? Ps $ p</c>).</summary>
public enum TerminalModeState
{
    /// <summary>The terminal did not answer or does not recognize the mode.</summary>
    NotRecognized = 0,

    /// <summary>The mode is set.</summary>
    Set = 1,

    /// <summary>The mode is reset.</summary>
    Reset = 2,

    /// <summary>The mode is set and cannot be changed.</summary>
    PermanentlySet = 3,

    /// <summary>The mode is reset and cannot be changed.</summary>
    PermanentlyReset = 4,
}