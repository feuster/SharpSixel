namespace SharpSixel.Tests;

public class ColorMathTests
{
    [Fact]
    public void PercentConversion_RoundTripsForAllPercentValues()
    {
        for (int percent = 0; percent <= 100; percent++)
        {
            Assert.Equal(percent, ColorMath.ByteToPercent(ColorMath.PercentToByte(percent)));
        }
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 255)]
    [InlineData(20, 51)]
    [InlineData(50, 128)]
    [InlineData(-5, 0)]
    [InlineData(500, 255)]
    public void PercentToByte_ClampsAndRounds(int percent, int expected)
    {
        Assert.Equal(expected, ColorMath.PercentToByte(percent));
    }

    [Fact]
    public void Vt340Palette_HasDefinedAndBlackRegisters()
    {
        SixelColor[] registers = Vt340Palette.CreateRegisters(20);

        Assert.Equal(SixelColor.Black, registers[0]);
        Assert.Equal(new SixelColor(51, 51, 204), registers[1]);
        Assert.Equal(SixelColor.Black, registers[19]);
    }
}
