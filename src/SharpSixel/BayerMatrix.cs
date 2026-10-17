namespace SharpSixel;

/// <summary>8x8 Bayer threshold matrix for ordered dithering.</summary>
internal static class BayerMatrix
{
    public const int Size = 8;

    private const int Cells = Size * Size;
    private static readonly int[] Values = Build();

    /// <summary>Returns the threshold for a pixel position, evenly spread within (-0.5, 0.5).</summary>
    public static double Threshold(int x, int y) =>
        (Values[(y % Size) * Size + (x % Size)] + 0.5) / Cells - 0.5;

    /// <summary>Raw matrix value (0-63) for a position.</summary>
    internal static int ValueAt(int x, int y) => Values[(y % Size) * Size + (x % Size)];

    private static int[] Build()
    {
        var values = new int[Cells];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                // Bit-interleaved construction: least significant coordinate bits become the most significant digits.
                int value = 0;
                for (int bit = 0; bit < 3; bit++)
                {
                    int xb = (x >> bit) & 1;
                    int yb = (y >> bit) & 1;
                    value = value * 4 + 2 * (xb ^ yb) + yb;
                }

                values[y * Size + x] = value;
            }
        }

        return values;
    }
}
