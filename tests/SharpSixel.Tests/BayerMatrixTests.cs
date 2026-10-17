namespace SharpSixel.Tests;

public class BayerMatrixTests
{
    [Fact]
    public void Matrix_IsPermutationOfZeroToSixtyThree()
    {
        var values = new List<int>();
        for (int y = 0; y < BayerMatrix.Size; y++)
        {
            for (int x = 0; x < BayerMatrix.Size; x++)
            {
                values.Add(BayerMatrix.ValueAt(x, y));
            }
        }

        Assert.Equal(Enumerable.Range(0, 64), values.OrderBy(v => v));
    }

    [Fact]
    public void Matrix_MatchesKnownBayerRows()
    {
        int[] firstRow = Enumerable.Range(0, 8).Select(x => BayerMatrix.ValueAt(x, 0)).ToArray();
        int[] secondRow = Enumerable.Range(0, 8).Select(x => BayerMatrix.ValueAt(x, 1)).ToArray();

        Assert.Equal(new[] { 0, 32, 8, 40, 2, 34, 10, 42 }, firstRow);
        Assert.Equal(new[] { 48, 16, 56, 24, 50, 18, 58, 26 }, secondRow);
    }

    [Fact]
    public void Matrix_RepeatsWithPeriodEight()
    {
        Assert.Equal(BayerMatrix.ValueAt(3, 5), BayerMatrix.ValueAt(11, 21));
    }

    [Fact]
    public void Threshold_IsCenteredAndWithinHalfRange()
    {
        double sum = 0;
        for (int y = 0; y < BayerMatrix.Size; y++)
        {
            for (int x = 0; x < BayerMatrix.Size; x++)
            {
                double t = BayerMatrix.Threshold(x, y);
                Assert.InRange(t, -0.5, 0.5);
                sum += t;
            }
        }

        Assert.InRange(Math.Abs(sum), 0, 1e-9);
    }
}
