using System;

namespace SharpSixel;

/// <summary>Scales <see cref="SixelImage"/> instances (area averaging when shrinking, bilinear when enlarging).</summary>
public static class ImageScaler
{
    /// <summary>
    /// Computes the size that fits into the given limits while keeping the aspect ratio. A null limit means unlimited.
    /// Without <paramref name="allowUpscale"/> the image is never enlarged.
    /// </summary>
    public static void FitSize(
        int sourceWidth,
        int sourceHeight,
        int? maxWidth,
        int? maxHeight,
        bool allowUpscale,
        out int width,
        out int height)
    {
        RequirePositive(sourceWidth, nameof(sourceWidth));
        RequirePositive(sourceHeight, nameof(sourceHeight));
        if (maxWidth.HasValue)
        {
            RequirePositive(maxWidth.Value, nameof(maxWidth));
        }

        if (maxHeight.HasValue)
        {
            RequirePositive(maxHeight.Value, nameof(maxHeight));
        }

        double scale = double.MaxValue;
        if (maxWidth.HasValue)
        {
            scale = Math.Min(scale, (double)maxWidth.Value / sourceWidth);
        }

        if (maxHeight.HasValue)
        {
            scale = Math.Min(scale, (double)maxHeight.Value / sourceHeight);
        }

        if (scale == double.MaxValue || (!allowUpscale && scale >= 1))
        {
            scale = 1;
        }

        width = Math.Max(1, (int)Math.Round(sourceWidth * scale));
        height = Math.Max(1, (int)Math.Round(sourceHeight * scale));
    }

    /// <summary>Scales the image into the limits (see <see cref="FitSize"/>); returns the same instance if nothing changes.</summary>
    public static SixelImage Fit(SixelImage source, int? maxWidth, int? maxHeight, bool allowUpscale = false)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        FitSize(source.Width, source.Height, maxWidth, maxHeight, allowUpscale, out int width, out int height);
        return width == source.Width && height == source.Height ? source : Resize(source, width, height);
    }

    /// <summary>Returns a resized copy. Colors are averaged alpha-weighted, so transparent pixels do not bleed.</summary>
    public static SixelImage Resize(SixelImage source, int width, int height)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        RequirePositive(width, nameof(width));
        RequirePositive(height, nameof(height));

        float[] horizontal = FilterRows(source, width);
        SixelImage result = FilterColumns(horizontal, source.Height, width, height);
        result.PixelAspectVertical = source.PixelAspectVertical;
        result.PixelAspectHorizontal = source.PixelAspectHorizontal;
        return result;
    }

    /// <summary>Pass 1: scales every row to the target width; output is premultiplied RGB plus alpha as floats.</summary>
    private static float[] FilterRows(SixelImage source, int targetWidth)
    {
        Contributions contributions = Contributions.Build(source.Width, targetWidth);
        var output = new float[checked(source.Height * targetWidth * 4)];
        byte[] px = source.Pixels;

        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                float r = 0, g = 0, b = 0, a = 0;
                float[] weights = contributions.Weights[x];
                for (int k = 0; k < weights.Length; k++)
                {
                    int i = (y * source.Width + contributions.Start[x] + k) * SixelImage.BytesPerPixel;
                    float alpha = px[i + 3];
                    float weight = weights[k];
                    float premultiplied = weight * alpha / 255f;
                    r += px[i] * premultiplied;
                    g += px[i + 1] * premultiplied;
                    b += px[i + 2] * premultiplied;
                    a += weight * alpha;
                }

                int o = (y * targetWidth + x) * 4;
                output[o] = r;
                output[o + 1] = g;
                output[o + 2] = b;
                output[o + 3] = a;
            }
        }

        return output;
    }

    /// <summary>Pass 2: scales every column to the target height and converts back to straight-alpha bytes.</summary>
    private static SixelImage FilterColumns(float[] rows, int sourceHeight, int width, int targetHeight)
    {
        Contributions contributions = Contributions.Build(sourceHeight, targetHeight);
        var result = new SixelImage(width, targetHeight);
        byte[] px = result.Pixels;

        for (int y = 0; y < targetHeight; y++)
        {
            float[] weights = contributions.Weights[y];
            for (int x = 0; x < width; x++)
            {
                float r = 0, g = 0, b = 0, a = 0;
                for (int k = 0; k < weights.Length; k++)
                {
                    int i = ((contributions.Start[y] + k) * width + x) * 4;
                    r += rows[i] * weights[k];
                    g += rows[i + 1] * weights[k];
                    b += rows[i + 2] * weights[k];
                    a += rows[i + 3] * weights[k];
                }

                int o = (y * width + x) * SixelImage.BytesPerPixel;
                if (a > 0.0001f)
                {
                    float scale = 255f / a;
                    px[o] = ToByte(r * scale);
                    px[o + 1] = ToByte(g * scale);
                    px[o + 2] = ToByte(b * scale);
                }

                px[o + 3] = ToByte(a);
            }
        }

        return result;
    }

    private static byte ToByte(float value) =>
        (byte)(value <= 0 ? 0 : value >= 255 ? 255 : (int)(value + 0.5f));

    private static void RequirePositive(int value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, "Value must be positive.");
        }
    }

    /// <summary>For every target index the first source index and the weights of the contributing source pixels.</summary>
    private sealed class Contributions
    {
        private Contributions(int count)
        {
            Start = new int[count];
            Weights = new float[count][];
        }

        public int[] Start { get; }

        public float[][] Weights { get; }

        public static Contributions Build(int sourceSize, int targetSize)
        {
            var result = new Contributions(targetSize);
            double scale = (double)sourceSize / targetSize;
            for (int t = 0; t < targetSize; t++)
            {
                if (scale > 1)
                {
                    result.SetAreaAverage(t, sourceSize, scale);
                }
                else
                {
                    result.SetBilinear(t, sourceSize, scale);
                }
            }

            return result;
        }

        private void SetAreaAverage(int target, int sourceSize, double scale)
        {
            double from = target * scale;
            double to = from + scale;
            int first = (int)Math.Floor(from);
            int last = Math.Min(sourceSize - 1, (int)Math.Ceiling(to) - 1);
            var weights = new float[last - first + 1];
            for (int i = first; i <= last; i++)
            {
                double overlap = Math.Min(to, i + 1) - Math.Max(from, i);
                weights[i - first] = (float)(overlap / scale);
            }

            Start[target] = first;
            Weights[target] = weights;
        }

        private void SetBilinear(int target, int sourceSize, double scale)
        {
            double position = (target + 0.5) * scale - 0.5;
            int lower = (int)Math.Floor(position);
            double fraction = position - lower;
            int a = Math.Min(Math.Max(lower, 0), sourceSize - 1);
            int b = Math.Min(Math.Max(lower + 1, 0), sourceSize - 1);
            Start[target] = a;
            Weights[target] = a == b
                ? new[] { 1f }
                : new[] { (float)(1 - fraction), (float)fraction };
        }
    }
}
