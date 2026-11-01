using System;

namespace SharpSixel.Ui;

internal static class Guard
{
    public static string NotEmpty(string value, string name)
    {
        if (value == null)
        {
            throw new ArgumentNullException(name);
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("The value must not be empty.", name);
        }

        return value;
    }

    public static int AtLeast(int value, int min, string name)
    {
        if (value < min)
        {
            throw new ArgumentOutOfRangeException(name, "Value must be at least " + min + ".");
        }

        return value;
    }

    public static int InRange(int value, int min, int max, string name)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(name, "Value must be between " + min + " and " + max + ".");
        }

        return value;
    }

    public static double InRange(double value, double min, double max, string name)
    {
        if (!(value >= min && value <= max))
        {
            throw new ArgumentOutOfRangeException(name, "Value must be between " + min + " and " + max + ".");
        }

        return value;
    }
}
