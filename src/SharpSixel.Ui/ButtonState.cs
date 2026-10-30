namespace SharpSixel.Ui;

/// <summary>Visual state of a <see cref="Button"/>.</summary>
public enum ButtonState
{
    /// <summary>Idle button.</summary>
    Normal,

    /// <summary>Button that has the focus (selected).</summary>
    Focused,

    /// <summary>Button that is being pressed.</summary>
    Pressed,

    /// <summary>Button that cannot be used.</summary>
    Disabled,
}
