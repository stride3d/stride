// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Stride.Core.Mathematics;

namespace Stride.Assets.SpriteFont.Compiler;

internal sealed class FontBitmap
{
    private readonly byte[] pixels;

    public FontBitmap(int width, int height) : this(width, height, new byte[checked(width * height * 4)])
    {
    }

    public FontBitmap(int width, int height, byte[] pixels)
    {
        if (width <= 0 || height <= 0 || pixels.Length != checked(width * height * 4))
            throw new ArgumentException("Invalid bitmap dimensions or pixel data.");
        Width = width;
        Height = height;
        this.pixels = pixels;
    }

    public int Width { get; }
    public int Height { get; }

    public Color this[int x, int y]
    {
        get
        {
            int offset = (y * Width + x) * 4;
            return new Color(pixels[offset], pixels[offset + 1], pixels[offset + 2], pixels[offset + 3]);
        }
        set
        {
            int offset = (y * Width + x) * 4;
            pixels[offset] = value.R;
            pixels[offset + 1] = value.G;
            pixels[offset + 2] = value.B;
            pixels[offset + 3] = value.A;
        }
    }

    public static FontBitmap Load(string path)
    {
        const int PixelSize = 4; // RGBA
        using var stream = File.OpenRead(path);
        using var image = Graphics.Image.Load(stream);
        var buffer = image.PixelBuffer[0];
        if (buffer.Format != Graphics.PixelFormat.R8G8B8A8_UNorm)
            throw new NotSupportedException("The font bitmap must decode to RGBA pixels.");
        var pixels = new byte[checked(buffer.Width * buffer.Height * PixelSize)];
        for (int y = 0; y < buffer.Height; y++)
            Marshal.Copy(buffer.DataPointer + y * buffer.RowStride, pixels, y * buffer.Width * PixelSize, buffer.Width * PixelSize);
        return new FontBitmap(buffer.Width, buffer.Height, pixels);
    }
}
