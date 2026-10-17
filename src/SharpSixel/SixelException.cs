using System;

namespace SharpSixel;

/// <summary>Thrown when sixel or image data is malformed, unsupported or exceeds configured limits.</summary>
public class SixelException : Exception
{
    /// <summary>Creates the exception with a message.</summary>
    public SixelException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and an inner exception.</summary>
    public SixelException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
