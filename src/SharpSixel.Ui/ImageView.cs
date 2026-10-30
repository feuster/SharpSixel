using System;
using System.IO;
using System.Text;

namespace SharpSixel.Ui;

/// <summary>
/// Shows an image in a view of fixed size (<see cref="Columns"/> x <see cref="Rows"/> cells) with an optional frame and a
/// background that is independent of the terminal background (<see cref="ImageViewStyle"/>). The image is fitted into the
/// view automatically (aspect ratio kept, centered, enlarged unless <see cref="AllowUpscale"/> is false) whenever it is
/// drawn, so it also follows other cell sizes; the fitted copy is cached. Pass the image as graphic
/// (<see cref="Image"/>) or load it from a file, stream or bytes (<see cref="Load(string)"/>).
/// Sixel mode draws the image; the ASCII fallback maps one image pixel to one cell (character by brightness, ANSI color
/// of the pixel if available) and ignores the background color.
/// </summary>
public sealed class ImageView : Widget
{
    private const int TransparentAlphaLimit = 128;
    private SixelImage? _image;
    private SixelImage? _fitted;
    private int _fittedWidth;
    private int _fittedHeight;
    private bool _fittedUpscale;

    /// <summary>Creates a view with the default style.</summary>
    /// <param name="columns">Width in cells (at least 1).</param>
    /// <param name="rows">Height in rows (at least 1).</param>
    public ImageView(int columns, int rows)
        : this(columns, rows, new ImageViewStyle())
    {
    }

    /// <summary>Creates a view with the given style.</summary>
    /// <param name="columns">Width in cells (at least 1).</param>
    /// <param name="rows">Height in rows (at least 1).</param>
    /// <param name="style">Look of the view.</param>
    public ImageView(int columns, int rows, ImageViewStyle style)
    {
        Columns = Guard.AtLeast(columns, 1, nameof(columns));
        Rows = Guard.AtLeast(rows, 1, nameof(rows));
        Style = style ?? throw new ArgumentNullException(nameof(style));
    }

    /// <summary>Width of the view in cells.</summary>
    public int Columns { get; }

    /// <summary>Height of the view in rows.</summary>
    public int Rows { get; }

    /// <summary>Look of the view; may be changed between frames.</summary>
    public ImageViewStyle Style { get; }

    /// <summary>True (default) to enlarge small images to fill the view; false keeps them at most at their own size.</summary>
    public bool AllowUpscale { get; set; } = true;

    /// <summary>
    /// Decoder for <see cref="Load(byte[])"/> and the other load methods, for example <c>SkiaImageCodec.Decode</c> of
    /// SharpSixel.Skia for PNG/JPEG. Null (default) supports BMP and sixel data.
    /// </summary>
    public Func<byte[], SixelImage>? Decoder { get; set; }

    /// <summary>
    /// The shown image (null = empty view). The image is not copied: assign it again after changing its pixels so the
    /// fitted copy is recreated.
    /// </summary>
    public SixelImage? Image
    {
        get => _image;
        set
        {
            _image = value;
            _fitted = null;
        }
    }

    /// <summary>Loads and shows an image file; the previous image stays if loading fails.</summary>
    /// <exception cref="SixelException">The data is invalid.</exception>
    /// <exception cref="NotSupportedException">The format is neither BMP nor sixel and no <see cref="Decoder"/> is set.</exception>
    public void Load(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        Load(File.ReadAllBytes(path));
    }

    /// <summary>Loads and shows an image from a stream (read to the end).</summary>
    public void Load(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        using (var buffer = new MemoryStream())
        {
            stream.CopyTo(buffer);
            Load(buffer.ToArray());
        }
    }

    /// <summary>Loads and shows an image from encoded bytes (BMP or sixel, or whatever <see cref="Decoder"/> reads).</summary>
    public void Load(byte[] data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        Image = Decode(data);
    }

    /// <inheritdoc />
    protected override int SixelRows => Rows;

    /// <inheritdoc />
    protected override int AsciiRows => Rows;

    /// <inheritdoc />
    protected override string RenderAscii(WidgetEnvironment environment)
    {
        bool frame = Style.ShowBorder && Columns >= 3 && Rows >= 3;
        int inset = frame ? 1 : 0;
        int innerWidth = Columns - (2 * inset);
        string[] body = RenderCells(environment, innerWidth, Rows - (2 * inset));
        if (!frame)
        {
            return string.Join("\n", body);
        }

        string edge = Paint(Style.AsciiCorner + new string(Style.AsciiHorizontal, innerWidth) + Style.AsciiCorner, environment);
        string side = Paint(Style.AsciiVertical.ToString(), environment);
        var lines = new string[Rows];
        lines[0] = edge;
        for (int i = 0; i < body.Length; i++)
        {
            lines[i + 1] = side + body[i] + side;
        }

        lines[Rows - 1] = edge;
        return string.Join("\n", lines);
    }

    /// <inheritdoc />
    protected override string RenderSixel(WidgetEnvironment environment)
    {
        var sb = new StringBuilder(AnsiCodes.SaveCursor);
        if (Style.BackgroundColor.A < TransparentAlphaLimit)
        {
            for (int row = 0; row < Rows; row++)
            {
                sb.Append(AnsiCodes.Down(row)).Append(AnsiCodes.EraseChars(Columns)).Append(AnsiCodes.RestoreCursor);
            }
        }

        return sb.Append(new SixelEncoder().Encode(CreateImage(environment))).Append(AnsiCodes.RestoreCursor).ToString();
    }

    internal SixelImage CreateImage(WidgetEnvironment environment)
    {
        int width = Columns * environment.CellWidth;
        int height = environment.GetImageHeight(Rows);
        var canvas = new SixelImage(width, height);
        int border = Style.ShowBorder ? Math.Min(Style.BorderThickness, Math.Min(width, height) / 2) : 0;
        if (border > 0)
        {
            Raster.Fill(canvas, Style.BorderColor);
        }

        int innerWidth = width - (2 * border);
        int innerHeight = height - (2 * border);
        Raster.FillRoundedRect(canvas, border, border, innerWidth, innerHeight, 0, Style.BackgroundColor);
        if (_image != null && innerWidth > 0 && innerHeight > 0)
        {
            SixelImage fitted = Fitted(innerWidth, innerHeight);
            Blit(canvas, fitted, border + ((innerWidth - fitted.Width) / 2), border + ((innerHeight - fitted.Height) / 2));
        }

        return canvas;
    }

    private SixelImage Decode(byte[] data)
    {
        if (Decoder != null)
        {
            return Decoder(data) ?? throw new InvalidOperationException("The decoder returned no image.");
        }

        if (BmpCodec.IsBmp(data))
        {
            return BmpCodec.Decode(data);
        }

        if (SixelDecoder.LooksLikeSixel(data))
        {
            return new SixelDecoder().Decode(data);
        }

        throw new NotSupportedException(
            "Unsupported image format: without a Decoder only BMP and sixel can be loaded (for example set SkiaImageCodec.Decode for PNG/JPEG).");
    }

    private SixelImage Fitted(int width, int height)
    {
        if (_fitted == null || _fittedWidth != width || _fittedHeight != height || _fittedUpscale != AllowUpscale)
        {
            _fitted = ImageScaler.Fit(_image!, width, height, AllowUpscale);
            _fittedWidth = width;
            _fittedHeight = height;
            _fittedUpscale = AllowUpscale;
        }

        return _fitted;
    }

    private static void Blit(SixelImage canvas, SixelImage source, int left, int top)
    {
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                SixelColor pixel = source.GetPixel(x, y);
                if (pixel.A == 0)
                {
                    continue;
                }

                int cx = left + x;
                int cy = top + y;
                if (cx < 0 || cy < 0 || cx >= canvas.Width || cy >= canvas.Height)
                {
                    continue;
                }

                if (pixel.A == 255)
                {
                    canvas.SetPixel(cx, cy, pixel);
                    continue;
                }

                SixelColor under = canvas.GetPixel(cx, cy);
                if (under.A == 255)
                {
                    canvas.SetPixel(cx, cy, new SixelColor(
                        Mix(pixel.R, under.R, pixel.A), Mix(pixel.G, under.G, pixel.A), Mix(pixel.B, under.B, pixel.A)));
                }
                else if (pixel.A >= TransparentAlphaLimit)
                {
                    canvas.SetPixel(cx, cy, new SixelColor(pixel.R, pixel.G, pixel.B));
                }
            }
        }
    }

    private static byte Mix(byte top, byte bottom, byte alpha) => (byte)(((top * alpha) + (bottom * (255 - alpha)) + 127) / 255);

    private string[] RenderCells(WidgetEnvironment environment, int width, int height)
    {
        SixelImage? scaled = null;
        int offsetX = 0;
        int offsetY = 0;
        if (_image != null)
        {
            ImageScaler.FitSize(
                _image.Width, _image.Height, width * environment.CellWidth, height * environment.CellHeight, AllowUpscale,
                out int pixelWidth, out int pixelHeight);
            int cols = Math.Max(1, Math.Min(width, (int)Math.Round((double)pixelWidth / environment.CellWidth, MidpointRounding.AwayFromZero)));
            int rows = Math.Max(1, Math.Min(height, (int)Math.Round((double)pixelHeight / environment.CellHeight, MidpointRounding.AwayFromZero)));
            scaled = ImageScaler.Resize(_image, cols, rows);
            offsetX = (width - cols) / 2;
            offsetY = (height - rows) / 2;
        }

        string ramp = Style.AsciiRamp;
        var lines = new string[height];
        for (int y = 0; y < height; y++)
        {
            var sb = new StringBuilder();
            for (int x = 0; x < width; x++)
            {
                bool inside = scaled != null && x >= offsetX && x < offsetX + scaled.Width && y >= offsetY && y < offsetY + scaled.Height;
                SixelColor pixel = inside ? scaled!.GetPixel(x - offsetX, y - offsetY) : SixelColor.Transparent;
                if (pixel.A < TransparentAlphaLimit)
                {
                    sb.Append(' ');
                    continue;
                }

                int brightness = ((299 * pixel.R) + (587 * pixel.G) + (114 * pixel.B)) / 1000;
                char c = ramp[brightness * (ramp.Length - 1) / 255];
                sb.Append(c == ' ' ? " " : AnsiCodes.Colorize(c.ToString(), pixel, environment.UseAnsi));
            }

            lines[y] = sb.ToString();
        }

        return lines;
    }

    private string Paint(string text, WidgetEnvironment environment) =>
        AnsiCodes.Colorize(text, Style.BorderColor, environment.UseAnsi);
}
