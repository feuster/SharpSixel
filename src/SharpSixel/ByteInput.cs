using System;
using System.IO;

namespace SharpSixel;

internal static class ByteInput
{
    public static byte[] ReadAll(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        using (var buffer = new MemoryStream())
        {
            stream.CopyTo(buffer);
            return buffer.ToArray();
        }
    }

    /// <summary>Maps each char 0-255 to one byte (Latin-1); chars above 255 are dropped.</summary>
    public static byte[] FromLatin1(string text)
    {
        var bytes = new byte[text.Length];
        int count = 0;
        foreach (char c in text)
        {
            if (c <= 0xFF)
            {
                bytes[count++] = (byte)c;
            }
        }

        Array.Resize(ref bytes, count);
        return bytes;
    }
}
