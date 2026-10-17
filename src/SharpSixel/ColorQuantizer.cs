using System;
using System.Collections.Generic;

namespace SharpSixel;

internal sealed class QuantizedImage
{
    public QuantizedImage(SixelColor[] palette, short[] indices, bool hasTransparentPixels)
    {
        Palette = palette;
        Indices = indices;
        HasTransparentPixels = hasTransparentPixels;
    }

    public SixelColor[] Palette { get; }

    /// <summary>Palette index per pixel; -1 marks a transparent pixel.</summary>
    public short[] Indices { get; }

    public bool HasTransparentPixels { get; }
}

/// <summary>Reduces an image to a palette: exact if it fits, otherwise median cut on a 5-5-5 histogram.</summary>
internal static class ColorQuantizer
{
    private const int HistogramSize = 1 << 15;

    public static QuantizedImage Quantize(SixelImage image, int maxColors, byte alphaThreshold, SixelDithering dithering)
    {
        return TryExactPalette(image, maxColors, alphaThreshold)
               ?? MedianCut(image, maxColors, alphaThreshold, dithering);
    }

    private static QuantizedImage? TryExactPalette(SixelImage image, int maxColors, byte alphaThreshold)
    {
        byte[] px = image.Pixels;
        int count = px.Length / SixelImage.BytesPerPixel;
        var indices = new short[count];
        var lookup = new Dictionary<int, int>();
        var palette = new List<SixelColor>();
        bool transparent = false;

        for (int i = 0; i < count; i++)
        {
            int o = i * SixelImage.BytesPerPixel;
            if (px[o + 3] < alphaThreshold)
            {
                indices[i] = -1;
                transparent = true;
                continue;
            }

            int key = (px[o] << 16) | (px[o + 1] << 8) | px[o + 2];
            if (!lookup.TryGetValue(key, out int index))
            {
                if (palette.Count >= maxColors)
                {
                    return null;
                }

                index = palette.Count;
                lookup.Add(key, index);
                palette.Add(new SixelColor(px[o], px[o + 1], px[o + 2]));
            }

            indices[i] = (short)index;
        }

        return new QuantizedImage(palette.ToArray(), indices, transparent);
    }

    private static QuantizedImage MedianCut(SixelImage image, int maxColors, byte alphaThreshold, SixelDithering dithering)
    {
        byte[] px = image.Pixels;
        int count = px.Length / SixelImage.BytesPerPixel;
        var population = new long[HistogramSize];
        var sumR = new long[HistogramSize];
        var sumG = new long[HistogramSize];
        var sumB = new long[HistogramSize];

        for (int i = 0; i < count; i++)
        {
            int o = i * SixelImage.BytesPerPixel;
            if (px[o + 3] < alphaThreshold)
            {
                continue;
            }

            int bin = BinOf(px[o], px[o + 1], px[o + 2]);
            population[bin]++;
            sumR[bin] += px[o];
            sumG[bin] += px[o + 1];
            sumB[bin] += px[o + 2];
        }

        var entries = new List<Entry>();
        for (int bin = 0; bin < HistogramSize; bin++)
        {
            if (population[bin] > 0)
            {
                entries.Add(new Entry(
                    population[bin],
                    Average(sumR[bin], population[bin]),
                    Average(sumG[bin], population[bin]),
                    Average(sumB[bin], population[bin])));
            }
        }

        SixelColor[] palette = BuildPalette(entries.ToArray(), maxColors);
        switch (dithering)
        {
            case SixelDithering.FloydSteinberg:
                return MapPixelsFloydSteinberg(image, palette, alphaThreshold);
            case SixelDithering.Ordered:
                return MapPixelsOrdered(image, palette, alphaThreshold);
            default:
                return MapPixels(px, palette, alphaThreshold, population, sumR, sumG, sumB);
        }
    }

    /// <summary>Floyd-Steinberg error diffusion; transparent pixels neither receive nor spread error.</summary>
    private static QuantizedImage MapPixelsFloydSteinberg(SixelImage image, SixelColor[] palette, byte alphaThreshold)
    {
        const int Channels = 3;
        int width = image.Width;
        byte[] px = image.Pixels;
        var indices = new short[width * image.Height];
        var nearest = new NearestColorCache(palette);

        // One padding column on each side keeps the diffusion free of bounds checks.
        var current = new float[(width + 2) * Channels];
        var next = new float[(width + 2) * Channels];
        bool transparent = false;

        for (int y = 0; y < image.Height; y++)
        {
            Array.Clear(next, 0, next.Length);
            for (int x = 0; x < width; x++)
            {
                int pixel = y * width + x;
                int o = pixel * SixelImage.BytesPerPixel;
                if (px[o + 3] < alphaThreshold)
                {
                    indices[pixel] = -1;
                    transparent = true;
                    continue;
                }

                int e = (x + 1) * Channels;
                int r = ClampToByte(px[o] + current[e]);
                int g = ClampToByte(px[o + 1] + current[e + 1]);
                int b = ClampToByte(px[o + 2] + current[e + 2]);

                short index = nearest.Find(r, g, b);
                indices[pixel] = index;
                Diffuse(current, next, e, r - palette[index].R, g - palette[index].G, b - palette[index].B);
            }

            float[] swap = current;
            current = next;
            next = swap;
        }

        return new QuantizedImage(palette, indices, transparent);
    }

    /// <summary>Ordered dithering: a position dependent Bayer offset is added before the nearest-color lookup.</summary>
    private static QuantizedImage MapPixelsOrdered(SixelImage image, SixelColor[] palette, byte alphaThreshold)
    {
        int width = image.Width;
        byte[] px = image.Pixels;
        var indices = new short[width * image.Height];
        var nearest = new NearestColorCache(palette);
        double spread = AverageChannelSpacing(palette);
        bool transparent = false;

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int pixel = y * width + x;
                int o = pixel * SixelImage.BytesPerPixel;
                if (px[o + 3] < alphaThreshold)
                {
                    indices[pixel] = -1;
                    transparent = true;
                    continue;
                }

                double offset = BayerMatrix.Threshold(x, y) * spread;
                indices[pixel] = nearest.Find(
                    ApplyOffset(px[o], offset),
                    ApplyOffset(px[o + 1], offset),
                    ApplyOffset(px[o + 2], offset));
            }
        }

        return new QuantizedImage(palette, indices, transparent);
    }

    /// <summary>Typical step between palette colors per channel: mean nearest-neighbor distance divided by sqrt(3).</summary>
    private static double AverageChannelSpacing(SixelColor[] palette)
    {
        if (palette.Length < 2)
        {
            return 0;
        }

        double sum = 0;
        for (int i = 0; i < palette.Length; i++)
        {
            int best = int.MaxValue;
            for (int j = 0; j < palette.Length; j++)
            {
                if (i == j)
                {
                    continue;
                }

                int dr = palette[i].R - palette[j].R;
                int dg = palette[i].G - palette[j].G;
                int db = palette[i].B - palette[j].B;
                best = Math.Min(best, dr * dr + dg * dg + db * db);
            }

            sum += Math.Sqrt(best);
        }

        return Math.Min(sum / palette.Length / Math.Sqrt(3), 255);
    }

    private static int ApplyOffset(byte value, double offset)
    {
        double shifted = value + offset;
        return shifted < 0 ? 0 : shifted > 255 ? 255 : (int)(shifted + 0.5);
    }

    private static void Diffuse(float[] current, float[] next, int e, int errorR, int errorG, int errorB)
    {
        Spread(current, e + 3, errorR, errorG, errorB, 7f / 16);
        Spread(next, e - 3, errorR, errorG, errorB, 3f / 16);
        Spread(next, e, errorR, errorG, errorB, 5f / 16);
        Spread(next, e + 3, errorR, errorG, errorB, 1f / 16);
    }

    private static void Spread(float[] row, int at, int errorR, int errorG, int errorB, float weight)
    {
        row[at] += errorR * weight;
        row[at + 1] += errorG * weight;
        row[at + 2] += errorB * weight;
    }

    private static int ClampToByte(float value) => value < 0 ? 0 : value > 255 ? 255 : (int)(value + 0.5f);

    private static QuantizedImage MapPixels(
        byte[] px,
        SixelColor[] palette,
        byte alphaThreshold,
        long[] population,
        long[] sumR,
        long[] sumG,
        long[] sumB)
    {
        int count = px.Length / SixelImage.BytesPerPixel;
        var indices = new short[count];
        var binToIndex = new short[HistogramSize];
        for (int i = 0; i < binToIndex.Length; i++)
        {
            binToIndex[i] = -1;
        }

        bool transparent = false;
        for (int i = 0; i < count; i++)
        {
            int o = i * SixelImage.BytesPerPixel;
            if (px[o + 3] < alphaThreshold)
            {
                indices[i] = -1;
                transparent = true;
                continue;
            }

            int bin = BinOf(px[o], px[o + 1], px[o + 2]);
            if (binToIndex[bin] < 0)
            {
                binToIndex[bin] = Nearest(
                    palette,
                    Average(sumR[bin], population[bin]),
                    Average(sumG[bin], population[bin]),
                    Average(sumB[bin], population[bin]));
            }

            indices[i] = binToIndex[bin];
        }

        return new QuantizedImage(palette, indices, transparent);
    }

    private static SixelColor[] BuildPalette(Entry[] entries, int maxColors)
    {
        var boxes = new List<Box> { Box.Create(entries, 0, entries.Length) };
        while (boxes.Count < maxColors)
        {
            int best = -1;
            int bestRange = 0;
            for (int i = 0; i < boxes.Count; i++)
            {
                if (boxes[i].Length > 1 && boxes[i].Range > bestRange)
                {
                    best = i;
                    bestRange = boxes[i].Range;
                }
            }

            if (best < 0)
            {
                break;
            }

            Box box = boxes[best];
            Array.Sort(entries, box.Start, box.Length, new ChannelComparer(box.Channel));
            int split = FindMedianSplit(entries, box);
            boxes[best] = Box.Create(entries, box.Start, split);
            boxes.Add(Box.Create(entries, split, box.End));
        }

        var palette = new SixelColor[boxes.Count];
        for (int i = 0; i < palette.Length; i++)
        {
            palette[i] = AverageColor(entries, boxes[i]);
        }

        return palette;
    }

    private static int FindMedianSplit(Entry[] entries, Box box)
    {
        long total = 0;
        for (int i = box.Start; i < box.End; i++)
        {
            total += entries[i].Count;
        }

        long accumulated = 0;
        int split = box.Start + 1;
        for (int i = box.Start; i < box.End - 1; i++)
        {
            accumulated += entries[i].Count;
            split = i + 1;
            if (accumulated * 2 >= total)
            {
                break;
            }
        }

        return split;
    }

    private static SixelColor AverageColor(Entry[] entries, Box box)
    {
        long weight = 0;
        long r = 0;
        long g = 0;
        long b = 0;
        for (int i = box.Start; i < box.End; i++)
        {
            weight += entries[i].Count;
            r += entries[i].R * entries[i].Count;
            g += entries[i].G * entries[i].Count;
            b += entries[i].B * entries[i].Count;
        }

        return new SixelColor((byte)Average(r, weight), (byte)Average(g, weight), (byte)Average(b, weight));
    }

    private static short Nearest(SixelColor[] palette, int r, int g, int b)
    {
        int best = 0;
        int bestDistance = int.MaxValue;
        for (int i = 0; i < palette.Length; i++)
        {
            int dr = r - palette[i].R;
            int dg = g - palette[i].G;
            int db = b - palette[i].B;
            int distance = dr * dr + dg * dg + db * db;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return (short)best;
    }

    private static int BinOf(byte r, byte g, byte b) => ((r >> 3) << 10) | ((g >> 3) << 5) | (b >> 3);

    private static int Average(long sum, long weight) => (int)((sum + weight / 2) / weight);

    private readonly struct Entry
    {
        public Entry(long count, int r, int g, int b)
        {
            Count = count;
            R = r;
            G = g;
            B = b;
        }

        public long Count { get; }

        public int R { get; }

        public int G { get; }

        public int B { get; }

        public int Get(int channel) => channel == 0 ? R : channel == 1 ? G : B;
    }

    private readonly struct Box
    {
        private Box(int start, int end, int channel, int range)
        {
            Start = start;
            End = end;
            Channel = channel;
            Range = range;
        }

        public int Start { get; }

        public int End { get; }

        public int Length => End - Start;

        /// <summary>Channel (0 = R, 1 = G, 2 = B) with the widest spread.</summary>
        public int Channel { get; }

        /// <summary>Spread of <see cref="Channel"/>.</summary>
        public int Range { get; }

        public static Box Create(Entry[] entries, int start, int end)
        {
            int bestChannel = 0;
            int bestRange = 0;
            for (int channel = 0; channel < 3; channel++)
            {
                int min = int.MaxValue;
                int max = int.MinValue;
                for (int i = start; i < end; i++)
                {
                    int value = entries[i].Get(channel);
                    min = Math.Min(min, value);
                    max = Math.Max(max, value);
                }

                if (max - min > bestRange)
                {
                    bestRange = max - min;
                    bestChannel = channel;
                }
            }

            return new Box(start, end, bestChannel, bestRange);
        }
    }

    /// <summary>Caches the nearest palette entry per 5-5-5 color bin.</summary>
    private sealed class NearestColorCache
    {
        private readonly SixelColor[] _palette;
        private readonly short[] _byBin = new short[HistogramSize];

        public NearestColorCache(SixelColor[] palette)
        {
            _palette = palette;
            for (int i = 0; i < _byBin.Length; i++)
            {
                _byBin[i] = -1;
            }
        }

        public short Find(int r, int g, int b)
        {
            int bin = BinOf((byte)r, (byte)g, (byte)b);
            if (_byBin[bin] < 0)
            {
                _byBin[bin] = Nearest(_palette, (r & ~7) + 4, (g & ~7) + 4, (b & ~7) + 4);
            }

            return _byBin[bin];
        }
    }

    private sealed class ChannelComparer : IComparer<Entry>
    {
        private readonly int _channel;

        public ChannelComparer(int channel)
        {
            _channel = channel;
        }

        public int Compare(Entry x, Entry y) => x.Get(_channel).CompareTo(y.Get(_channel));
    }
}
