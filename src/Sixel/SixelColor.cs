using System;

namespace Sixel;

/// <summary>An 8-bit-per-channel RGBA color with straight (non-premultiplied) alpha.</summary>
public readonly struct SixelColor : IEquatable<SixelColor>
{
    /// <summary>Creates a color from its channels.</summary>
    public SixelColor(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>Red channel.</summary>
    public byte R { get; }

    /// <summary>Green channel.</summary>
    public byte G { get; }

    /// <summary>Blue channel.</summary>
    public byte B { get; }

    /// <summary>Alpha channel (255 = opaque).</summary>
    public byte A { get; }

    /// <summary>Opaque black.</summary>
    public static SixelColor Black => new SixelColor(0, 0, 0);

    /// <summary>Fully transparent black.</summary>
    public static SixelColor Transparent => new SixelColor(0, 0, 0, 0);

    /// <inheritdoc />
    public bool Equals(SixelColor other) => R == other.R && G == other.G && B == other.B && A == other.A;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is SixelColor other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => (R << 24) ^ (G << 16) ^ (B << 8) ^ A;

    /// <summary>Equality operator.</summary>
    public static bool operator ==(SixelColor left, SixelColor right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(SixelColor left, SixelColor right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}{A:X2}";
}
