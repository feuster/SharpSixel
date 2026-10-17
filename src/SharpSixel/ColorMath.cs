using System;

namespace SharpSixel;

internal static class ColorMath
{
    public static byte PercentToByte(int percent)
    {
        int p = Math.Min(Math.Max(percent, 0), 100);
        return (byte)((p * 255 + 50) / 100);
    }

    public static int ByteToPercent(byte value) => (value * 100 + 127) / 255;

    public static SixelColor PercentRgb(int red, int green, int blue) =>
        new SixelColor(PercentToByte(red), PercentToByte(green), PercentToByte(blue));

    /// <summary>
    /// Converts a DEC HLS color (hue 0-360 where blue = 0, red = 120, green = 240;
    /// lightness and saturation in percent) to RGB.
    /// </summary>
    public static SixelColor HlsToRgb(int hue, int lightness, int saturation)
    {
        double l = Clamp01(lightness / 100.0);
        double s = Clamp01(saturation / 100.0);
        double h = (((hue % 360) + 240) % 360) / 360.0;

        if (s == 0)
        {
            byte gray = ToByte(l);
            return new SixelColor(gray, gray, gray);
        }

        double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        double p = 2 * l - q;
        return new SixelColor(
            ToByte(Channel(p, q, h + 1.0 / 3)),
            ToByte(Channel(p, q, h)),
            ToByte(Channel(p, q, h - 1.0 / 3)));
    }

    private static double Channel(double p, double q, double t)
    {
        if (t < 0)
        {
            t += 1;
        }

        if (t > 1)
        {
            t -= 1;
        }

        if (t < 1.0 / 6)
        {
            return p + (q - p) * 6 * t;
        }

        if (t < 0.5)
        {
            return q;
        }

        if (t < 2.0 / 3)
        {
            return p + (q - p) * (2.0 / 3 - t) * 6;
        }

        return p;
    }

    private static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;

    private static byte ToByte(double value) =>
        (byte)Math.Round(Clamp01(value) * 255, MidpointRounding.AwayFromZero);
}
