using System;

namespace Sixel;

/// <summary>State machine for one decode run over the sixel data string (after the DCS header).</summary>
internal sealed class DecodeSession
{
    private const byte Esc = 0x1B;
    private const byte Can = 0x18;
    private const byte Sub = 0x1A;
    private const byte St8 = 0x9C;
    private const int SixelHeight = 6;
    private const int FirstDataChar = 63;
    private const int LastDataChar = 126;

    private static readonly SixelColor Foreground = new SixelColor(255, 255, 255);

    private readonly SixelDecoderOptions _options;
    private readonly PixelCanvas _canvas;
    private readonly SixelColor[] _registers;
    private readonly int[] _numbers = new int[5];
    private readonly bool _transparentBackground;

    private int _aspectVertical;
    private int _aspectHorizontal;
    private int _declaredWidth;
    private int _declaredHeight;
    private int _x;
    private int _y;
    private int _maxX;
    private int _usedHeight;
    private int _repeat = 1;
    private int _color = -1;

    public DecodeSession(SixelDecoderOptions options, int macroParameter, int backgroundSelect)
    {
        _options = options;
        _canvas = new PixelCanvas(options.MaxWidth, options.MaxHeight, options.MaxPixels);
        _registers = Vt340Palette.CreateRegisters(options.MaxColorRegisters);
        _transparentBackground = backgroundSelect == 1;
        SetMacroAspect(macroParameter);
    }

    public SixelImage Run(byte[] data, int start)
    {
        int i = start;
        while (i < data.Length)
        {
            byte b = data[i++];
            if (b == Esc || b == St8 || b == Can || b == Sub)
            {
                break;
            }

            if (b >= FirstDataChar && b <= LastDataChar)
            {
                Paint(b - FirstDataChar);
                continue;
            }

            switch ((char)b)
            {
                case '!':
                    SetRepeat(data, ref i);
                    break;
                case '"':
                    SetRasterAttributes(data, ref i);
                    break;
                case '#':
                    SelectOrDefineColor(data, ref i);
                    break;
                case '$':
                    _x = 0;
                    break;
                case '-':
                    _x = 0;
                    if (_y <= _options.MaxHeight)
                    {
                        _y += SixelHeight;
                    }

                    break;
            }
        }

        return Build();
    }

    private void SetMacroAspect(int p1)
    {
        // VT330/VT340 Programmer Reference, chapter 14 (vertical:horizontal).
        switch (p1)
        {
            case 2:
                _aspectVertical = 5;
                break;
            case 3:
            case 4:
                _aspectVertical = 3;
                break;
            case 7:
            case 8:
            case 9:
                _aspectVertical = 1;
                break;
            default:
                _aspectVertical = 2;
                break;
        }

        _aspectHorizontal = 1;
    }

    private void SetRepeat(byte[] data, ref int i)
    {
        int count = ReadNumbers(data, ref i);
        _repeat = count > 0 && _numbers[0] > 0 ? _numbers[0] : 1;
    }

    private void SetRasterAttributes(byte[] data, ref int i)
    {
        int count = ReadNumbers(data, ref i);
        if (count >= 2 && _numbers[0] > 0 && _numbers[1] > 0)
        {
            _aspectVertical = _numbers[0];
            _aspectHorizontal = _numbers[1];
        }

        if (count >= 4 && _numbers[2] > 0 && _numbers[3] > 0)
        {
            _declaredWidth = _numbers[2];
            _declaredHeight = _numbers[3];
            _canvas.Ensure(_declaredWidth, _declaredHeight);
        }
    }

    private void SelectOrDefineColor(byte[] data, ref int i)
    {
        int count = ReadNumbers(data, ref i);
        if (count == 0)
        {
            return;
        }

        int register = _numbers[0] % _registers.Length;
        _color = register;
        if (count < 5)
        {
            return;
        }

        switch (_numbers[1])
        {
            case 1:
                _registers[register] = ColorMath.HlsToRgb(_numbers[2], _numbers[3], _numbers[4]);
                break;
            case 2:
                _registers[register] = ColorMath.PercentRgb(_numbers[2], _numbers[3], _numbers[4]);
                break;
        }
    }

    /// <summary>Reads "n;n;n..." into <see cref="_numbers"/> and returns the number of fields (0 if none).</summary>
    private int ReadNumbers(byte[] data, ref int i)
    {
        Array.Clear(_numbers, 0, _numbers.Length);
        int field = 0;
        long current = 0;
        bool seen = false;
        while (i < data.Length)
        {
            byte b = data[i];
            if (b >= '0' && b <= '9')
            {
                current = Math.Min(current * 10 + (b - '0'), int.MaxValue);
                seen = true;
            }
            else if (b == ';')
            {
                Store(field++, current);
                current = 0;
                seen = true;
            }
            else if (!IsIgnorableControl(b))
            {
                break;
            }

            i++;
        }

        if (!seen)
        {
            return 0;
        }

        Store(field, current);
        return field + 1;
    }

    private static bool IsIgnorableControl(byte b) => b < 0x21 && b != Esc && b != Can && b != Sub;

    private void Store(int field, long value)
    {
        if (field < _numbers.Length)
        {
            _numbers[field] = (int)value;
        }
    }

    private void Paint(int bits)
    {
        int repeat = _repeat;
        _repeat = 1;

        long end = (long)_x + repeat;
        int bandBottom = _y + SixelHeight;
        if (end > _options.MaxWidth || bandBottom > _options.MaxHeight || bandBottom < 0)
        {
            throw new SixelException("The sixel image exceeds the configured size limits.");
        }

        if (bits != 0)
        {
            _canvas.Ensure((int)end, bandBottom);
            SixelColor color = _color < 0 ? Foreground : _registers[_color];
            for (int row = 0; row < SixelHeight; row++)
            {
                if ((bits >> row & 1) != 0)
                {
                    _canvas.Fill(_x, _y + row, repeat, color);
                }
            }
        }

        _x = (int)end;
        _maxX = Math.Max(_maxX, _x);
        _usedHeight = Math.Max(_usedHeight, bandBottom);
    }

    private SixelImage Build()
    {
        int width = Math.Max(_declaredWidth, _maxX);
        int height = ResolveHeight();
        if (width <= 0 || height <= 0)
        {
            throw new SixelException("The sixel data does not contain an image.");
        }

        if (width > _options.MaxWidth || height > _options.MaxHeight
            || (long)width * height > _options.MaxPixels)
        {
            throw new SixelException("The sixel image exceeds the configured size limits.");
        }

        byte[] pixels = _canvas.CropTo(width, height);
        if (!_transparentBackground)
        {
            ApplyBackground(pixels, _options.Background);
        }

        return new SixelImage(width, height, pixels)
        {
            PixelAspectVertical = _aspectVertical,
            PixelAspectHorizontal = _aspectHorizontal,
        };
    }

    private int ResolveHeight()
    {
        // The last band is always six rows tall; a declared height that only cuts padding rows wins.
        bool declaredCutsPadding = _declaredHeight > 0
            && _declaredHeight < _usedHeight
            && _usedHeight - _declaredHeight < SixelHeight;
        return declaredCutsPadding ? _declaredHeight : Math.Max(_declaredHeight, _usedHeight);
    }

    private static void ApplyBackground(byte[] pixels, SixelColor background)
    {
        for (int i = 0; i < pixels.Length; i += SixelImage.BytesPerPixel)
        {
            if (pixels[i + 3] == 0)
            {
                pixels[i] = background.R;
                pixels[i + 1] = background.G;
                pixels[i + 2] = background.B;
                pixels[i + 3] = background.A;
            }
        }
    }
}
