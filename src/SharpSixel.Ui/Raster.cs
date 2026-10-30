using System;

namespace SharpSixel.Ui;

/// <summary>Minimal pixel drawing on a <see cref="SixelImage"/> (binary coverage, no blending).</summary>
internal static class Raster
{
    public static void Fill(SixelImage image, SixelColor color)
    {
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                image.SetPixel(x, y, color);
            }
        }
    }

    /// <summary>Fills a rounded rectangle; pixels with x >= <paramref name="clipRight"/> are skipped.</summary>
    public static void FillRoundedRect(
        SixelImage image, int x, int y, int width, int height, int radius, SixelColor color, int clipRight = int.MaxValue)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        int r = Math.Min(radius, Math.Min(width, height) / 2);
        int xEnd = Math.Min(Math.Min(x + width, clipRight), image.Width);
        int yEnd = Math.Min(y + height, image.Height);
        for (int py = Math.Max(y, 0); py < yEnd; py++)
        {
            for (int px = Math.Max(x, 0); px < xEnd; px++)
            {
                if (IsInside(px - x, py - y, width, height, r))
                {
                    image.SetPixel(px, py, color);
                }
            }
        }
    }

    /// <summary>Draws a line segment of the given thickness (round ends) between two points.</summary>
    public static void FillLine(SixelImage image, double x1, double y1, double x2, double y2, double thickness, SixelColor color)
    {
        double half = thickness / 2.0;
        int left = Math.Max(0, (int)Math.Floor(Math.Min(x1, x2) - half));
        int right = Math.Min(image.Width - 1, (int)Math.Ceiling(Math.Max(x1, x2) + half));
        int top = Math.Max(0, (int)Math.Floor(Math.Min(y1, y2) - half));
        int bottom = Math.Min(image.Height - 1, (int)Math.Ceiling(Math.Max(y1, y2) + half));
        double dx = x2 - x1;
        double dy = y2 - y1;
        double lengthSquared = (dx * dx) + (dy * dy);
        for (int py = top; py <= bottom; py++)
        {
            for (int px = left; px <= right; px++)
            {
                double cx = px + 0.5;
                double cy = py + 0.5;
                double t = lengthSquared <= 0 ? 0 : Math.Max(0, Math.Min(1, (((cx - x1) * dx) + ((cy - y1) * dy)) / lengthSquared));
                double ex = cx - (x1 + (t * dx));
                double ey = cy - (y1 + (t * dy));
                if ((ex * ex) + (ey * ey) <= half * half)
                {
                    image.SetPixel(px, py, color);
                }
            }
        }
    }

    private static bool IsInside(int localX, int localY, int width, int height, int radius)
    {
        if (radius <= 0)
        {
            return true;
        }

        double cx = localX + 0.5;
        double cy = localY + 0.5;
        double dx = cx < radius ? radius - cx : cx > width - radius ? cx - (width - radius) : 0;
        double dy = cy < radius ? radius - cy : cy > height - radius ? cy - (height - radius) : 0;
        return (dx * dx) + (dy * dy) <= (double)radius * radius;
    }
}
