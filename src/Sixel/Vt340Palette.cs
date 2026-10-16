namespace Sixel;

/// <summary>Default VT340 color map (16 registers, values in percent), as used by common sixel implementations.</summary>
internal static class Vt340Palette
{
    private const int DefinedRegisters = 16;

    private static readonly int[,] Percent =
    {
        { 0, 0, 0 },
        { 20, 20, 80 },
        { 80, 13, 13 },
        { 20, 80, 20 },
        { 80, 20, 80 },
        { 20, 80, 80 },
        { 80, 80, 20 },
        { 53, 53, 53 },
        { 26, 26, 26 },
        { 33, 33, 60 },
        { 60, 26, 26 },
        { 33, 60, 33 },
        { 60, 33, 60 },
        { 33, 60, 60 },
        { 60, 60, 33 },
        { 80, 80, 80 },
    };

    /// <summary>Creates a register file; registers beyond the VT340 defaults start as opaque black.</summary>
    public static SixelColor[] CreateRegisters(int count)
    {
        var registers = new SixelColor[count];
        for (int i = 0; i < count; i++)
        {
            registers[i] = i < DefinedRegisters
                ? ColorMath.PercentRgb(Percent[i, 0], Percent[i, 1], Percent[i, 2])
                : SixelColor.Black;
        }

        return registers;
    }
}
