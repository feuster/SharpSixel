using System.Text;
using SharpSixel.Ui;

namespace SharpSixel.Tests.Ui;

public class ImageViewTests
{
    private static readonly WidgetEnvironment SixelEnv = new WidgetEnvironment(true, 10, 20);
    private static readonly WidgetEnvironment Plain = new WidgetEnvironment(false, useAnsi: false);
    private static readonly WidgetEnvironment AnsiAscii = new WidgetEnvironment(false);
    private static readonly SixelColor Green = new SixelColor(0, 200, 0);
    private static readonly SixelColor Red = new SixelColor(220, 0, 0);

    private static SixelImage Solid(int width, int height, SixelColor color)
    {
        var image = new SixelImage(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image.SetPixel(x, y, color);
            }
        }

        return image;
    }

    private static SixelColor At(ImageView view, int x, int y) => view.CreateImage(SixelEnv).GetPixel(x, y);

    [Fact]
    public void Constructor_ValidatesArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageView(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageView(1, 0));
        Assert.Throws<ArgumentNullException>(() => new ImageView(1, 1, null!));
        var view = new ImageView(4, 2);
        Assert.Equal(4, view.Columns);
        Assert.Equal(2, view.Rows);
        Assert.Equal(2, view.GetRows(SixelEnv));
        Assert.Equal(2, view.GetRows(Plain));
    }

    [Fact]
    public void Style_ValidatesValues()
    {
        var style = new ImageViewStyle();
        Assert.False(style.ShowBorder);
        Assert.Equal(SixelColor.Transparent, style.BackgroundColor);
        Assert.Throws<ArgumentOutOfRangeException>(() => style.BorderThickness = 0);
        Assert.Throws<ArgumentException>(() => style.AsciiRamp = string.Empty);
        Assert.Throws<ArgumentNullException>(() => style.AsciiRamp = null!);
    }

    [Fact]
    public void Image_IsFittedKeepingAspectAndCentered()
    {
        var view = new ImageView(4, 2) { Image = Solid(80, 20, Green) };   // canvas 40x36, fitted 40x10 at y=13
        SixelImage canvas = view.CreateImage(SixelEnv);
        Assert.Equal(40, canvas.Width);
        Assert.Equal(36, canvas.Height);
        Assert.Equal(Green, canvas.GetPixel(0, 13));
        Assert.Equal(Green, canvas.GetPixel(39, 22));
        Assert.Equal(0, canvas.GetPixel(20, 12).A);
        Assert.Equal(0, canvas.GetPixel(20, 23).A);
    }

    [Fact]
    public void SmallImage_IsEnlargedUnlessUpscaleIsOff()
    {
        var view = new ImageView(4, 2) { Image = Solid(2, 2, Green) };      // fitted 36x36 at x=2
        Assert.Equal(Green, At(view, 2, 0));
        Assert.Equal(Green, At(view, 37, 35));
        Assert.Equal(0, At(view, 1, 0).A);
        Assert.Equal(0, At(view, 38, 0).A);

        view.AllowUpscale = false;                                           // stays 2x2 at (19,17)
        Assert.Equal(Green, At(view, 19, 17));
        Assert.Equal(Green, At(view, 20, 18));
        Assert.Equal(0, At(view, 18, 17).A);
        Assert.Equal(0, At(view, 21, 17).A);
    }

    [Fact]
    public void Border_IsDrawnAroundTheImageArea()
    {
        var style = new ImageViewStyle { ShowBorder = true, BorderColor = Red, BorderThickness = 2 };
        var view = new ImageView(4, 2, style) { Image = Solid(1, 1, Green) };   // inner 36x32, fitted 32x32 at x=4,y=2
        Assert.Equal(Red, At(view, 0, 0));
        Assert.Equal(Red, At(view, 39, 35));
        Assert.Equal(Red, At(view, 1, 10));
        Assert.Equal(0, At(view, 2, 10).A);
        Assert.Equal(Green, At(view, 4, 2));
        Assert.Equal(Green, At(view, 35, 33));
    }

    [Fact]
    public void Background_IsIndependentOfTerminalBackground()
    {
        var view = new ImageView(4, 2) { Image = Solid(80, 20, Green) };
        Assert.Equal(0, At(view, 0, 0).A);

        view.Style.BackgroundColor = new SixelColor(10, 20, 30);
        Assert.Equal(new SixelColor(10, 20, 30), At(view, 0, 0));
        Assert.Equal(Green, At(view, 20, 15));
    }

    [Fact]
    public void BorderThickness_InsideBackground()
    {
        var style = new ImageViewStyle { ShowBorder = true, BorderColor = Red, BorderThickness = 3, BackgroundColor = new SixelColor(1, 2, 3) };
        var view = new ImageView(4, 2, style);
        Assert.Equal(Red, At(view, 2, 20));
        Assert.Equal(new SixelColor(1, 2, 3), At(view, 3, 20));
    }

    [Fact]
    public void SemiTransparentPixels_BlendOntoOpaqueBackgroundOnly()
    {
        var style = new ImageViewStyle { BackgroundColor = new SixelColor(10, 10, 10) };
        var half = new SixelColor(255, 255, 255, 128);
        var view = new ImageView(1, 1, style) { Image = Solid(1, 1, half) };
        SixelColor blended = At(view, 5, 5);
        Assert.Equal(255, blended.A);
        Assert.InRange(blended.R, 132, 134);

        var open = new ImageView(1, 1) { Image = Solid(1, 1, half) };
        Assert.Equal(new SixelColor(255, 255, 255), At(open, 5, 5));

        var faint = new ImageView(1, 1) { Image = Solid(1, 1, new SixelColor(255, 255, 255, 40)) };
        Assert.Equal(0, At(faint, 5, 5).A);
    }

    [Fact]
    public void TransparentImagePixels_KeepTheBackground()
    {
        var style = new ImageViewStyle { BackgroundColor = new SixelColor(9, 9, 9) };
        var view = new ImageView(1, 1, style) { Image = Solid(1, 1, SixelColor.Transparent) };
        Assert.Equal(new SixelColor(9, 9, 9), At(view, 5, 5));
    }

    [Fact]
    public void AssigningImageAgain_RefreshesTheFittedCopy()
    {
        SixelImage image = Solid(2, 2, Green);
        var view = new ImageView(2, 1) { Image = image };
        Assert.Equal(Green, At(view, 10, 8));

        image.SetPixel(0, 0, Red);                // same instance, changed pixels
        view.Image = image;
        Assert.Equal(Red, At(view, 1, 0));

        view.Image = Solid(2, 2, Red);
        Assert.Equal(Red, At(view, 10, 8));

        view.Image = null;
        Assert.Equal(0, At(view, 10, 8).A);
    }

    [Fact]
    public void ViewSize_FollowsCellSize()
    {
        var view = new ImageView(2, 1) { Image = Solid(10, 10, Green) };
        SixelImage small = view.CreateImage(new WidgetEnvironment(true, 8, 16));
        Assert.Equal(16, small.Width);
        Assert.Equal(12, small.Height);
        Assert.Equal(Green, small.GetPixel(8, 6));
    }

    [Fact]
    public void Load_ReadsBmpBytesStreamAndFile()
    {
        byte[] bmp = BmpCodec.Encode(Solid(3, 2, Green));
        var view = new ImageView(4, 2);

        view.Load(bmp);
        Assert.Equal((3, 2), (view.Image!.Width, view.Image.Height));

        view.Image = null;
        using (var stream = new MemoryStream(bmp))
        {
            view.Load(stream);
        }

        Assert.NotNull(view.Image);

        view.Image = null;
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, bmp);
            view.Load(path);
        }
        finally
        {
            File.Delete(path);
        }

        Assert.Equal(Green, view.Image!.GetPixel(0, 0));
        Assert.Equal(Green, At(view, 20, 17));
    }

    [Fact]
    public void Load_ReadsSixelData()
    {
        string sixel = new SixelEncoder().Encode(Solid(4, 6, Green));
        var view = new ImageView(4, 2);
        view.Load(Encoding.ASCII.GetBytes(sixel));
        Assert.Equal((4, 6), (view.Image!.Width, view.Image.Height));
    }

    [Fact]
    public void Load_UsesDecoderWhenSet()
    {
        SixelImage decoded = Solid(2, 2, Red);
        byte[]? received = null;
        var view = new ImageView(4, 2) { Decoder = data => { received = data; return decoded; } };
        view.Load(new byte[] { 1, 2, 3 });
        Assert.Same(decoded, view.Image);
        Assert.Equal(new byte[] { 1, 2, 3 }, received);

        view.Decoder = _ => null!;
        Assert.Throws<InvalidOperationException>(() => view.Load(new byte[] { 1 }));
    }

    [Fact]
    public void Load_FailureKeepsPreviousImage()
    {
        SixelImage image = Solid(2, 2, Green);
        var view = new ImageView(4, 2) { Image = image };
        Assert.Throws<NotSupportedException>(() => view.Load(new byte[] { 1, 2, 3 }));
        Assert.Same(image, view.Image);
        Assert.Throws<FileNotFoundException>(() => view.Load(Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid() + ".bmp")));
        Assert.Throws<ArgumentNullException>(() => view.Load((string)null!));
        Assert.Throws<ArgumentNullException>(() => view.Load((Stream)null!));
        Assert.Throws<ArgumentNullException>(() => view.Load((byte[])null!));
        Assert.Same(image, view.Image);
    }

    [Fact]
    public void Sixel_FrameErasesTransparentBackgroundAndDrawsGraphic()
    {
        var view = new ImageView(4, 2) { Image = Solid(2, 2, Green) };
        string frame = view.Render(SixelEnv);
        Assert.StartsWith(AnsiCodes.SaveCursor, frame, StringComparison.Ordinal);
        Assert.EndsWith(AnsiCodes.RestoreCursor, frame, StringComparison.Ordinal);
        Assert.Contains(AnsiCodes.EraseChars(4), frame, StringComparison.Ordinal);
        Assert.Contains("\u001bP", frame, StringComparison.Ordinal);

        view.Style.BackgroundColor = new SixelColor(0, 0, 0);
        Assert.DoesNotContain(AnsiCodes.EraseChars(4), view.Render(SixelEnv), StringComparison.Ordinal);
    }

    [Fact]
    public void Ascii_MapsPixelsToCells()
    {
        var view = new ImageView(6, 3) { Image = Solid(1, 1, new SixelColor(255, 255, 255)) };
        Assert.Equal("@@@@@@\n@@@@@@\n@@@@@@", view.Render(Plain));
    }

    [Fact]
    public void Ascii_KeepsAspectRatioAndCenters()
    {
        var view = new ImageView(6, 3) { Image = Solid(4, 1, new SixelColor(255, 255, 255)) };   // 60x60 px box: 60x15 px = 6x1 cells
        Assert.Equal("      \n@@@@@@\n      ", view.Render(Plain));
    }

    [Fact]
    public void Ascii_BorderAndInnerArea()
    {
        var style = new ImageViewStyle { ShowBorder = true };
        var view = new ImageView(6, 3, style) { Image = Solid(1, 1, new SixelColor(255, 255, 255)) };
        Assert.Equal("+----+\n| @@ |\n+----+", view.Render(Plain));
    }

    [Fact]
    public void Ascii_EmptyViewAndTooSmallFrame()
    {
        var style = new ImageViewStyle { ShowBorder = true };
        Assert.Equal("+--+\n|  |\n+--+", new ImageView(4, 3, style).Render(Plain));
        Assert.Equal("  \n  ", new ImageView(2, 2, style).Render(Plain));
        Assert.Equal("      ", new ImageView(6, 1).Render(Plain));
    }

    [Fact]
    public void Ascii_RampDarkPixelsAndTransparency()
    {
        var style = new ImageViewStyle { AsciiRamp = "ab" };
        var white = new ImageView(2, 1, style) { Image = Solid(1, 1, new SixelColor(255, 255, 255)) };
        Assert.Equal("bb", white.Render(Plain));
        var black = new ImageView(2, 1, style) { Image = Solid(1, 1, SixelColor.Black) };
        Assert.Equal("aa", black.Render(Plain));
        var clear = new ImageView(2, 1, style) { Image = Solid(1, 1, SixelColor.Transparent) };
        Assert.Equal("  ", clear.Render(Plain));
        var dark = new ImageView(2, 1) { Image = Solid(1, 1, SixelColor.Black) };
        Assert.Equal("  ", dark.Render(Plain));
    }

    [Fact]
    public void Ascii_AnsiColorsPixelsAndBorder()
    {
        var style = new ImageViewStyle { ShowBorder = true, BorderColor = Red };
        var view = new ImageView(5, 3, style) { Image = Solid(1, 1, Green) };
        string frame = view.Render(AnsiAscii);
        Assert.Contains("\u001b[38;2;0;200;0m", frame, StringComparison.Ordinal);
        Assert.StartsWith("\u001b[38;2;220;0;0m+---+", frame, StringComparison.Ordinal);
        Assert.Equal(3, frame.Split('\n').Length);
    }
}
