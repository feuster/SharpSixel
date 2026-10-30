namespace SharpSixel.Ui;

/// <summary>How widgets are drawn.</summary>
public enum RenderMode
{
    /// <summary>Sixel graphics if the terminal reports sixel support, otherwise ASCII.</summary>
    Auto,

    /// <summary>Always sixel graphics.</summary>
    Sixel,

    /// <summary>Always ASCII characters.</summary>
    Ascii,
}
