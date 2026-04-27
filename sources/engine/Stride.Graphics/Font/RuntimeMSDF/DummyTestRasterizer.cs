using System;
using Stride.Core.Mathematics;
using Stride.Graphics.Font;

namespace Stride.Graphics.Font.RuntimeMsdf
{
    /// <summary>
    /// Dummy MSDF rasterizer that generates simple test patterns.
    /// Use this to isolate pipeline issues from MSDF generation issues.
    /// </summary>
    public sealed class DummyTestRasterizer : IGlyphMsdfRasterizer
    {
        private int glyphCounter = 0;

        CharacterBitmapRgba IGlyphMsdfRasterizer.RasterizeMsdf(
            GlyphOutline outline,
            DistanceFieldSettings df,
            MsdfEncodeSettings encode)
        {
            var totalWidth = df.TotalWidth;
            var totalHeight = df.TotalHeight;

            if (totalWidth <= 0 || totalHeight <= 0)
                return new CharacterBitmapRgba();

            var bmp = new CharacterBitmapRgba(totalWidth, totalHeight);

            // Increment counter for each glyph (helps identify unique glyphs)
            int currentGlyph = System.Threading.Interlocked.Increment(ref glyphCounter);

            unsafe
            {
                byte* buffer = (byte*)bmp.Buffer;
                int pitch = bmp.Pitch;

                // Choose pattern based on glyph number (mod 4)
                int patternType = currentGlyph % 4;

                for (int y = 0; y < totalHeight; y++)
                {
                    byte* row = buffer + y * pitch;
                    
                    for (int x = 0; x < totalWidth; x++)
                    {
                        byte r, g, b;

                        switch (patternType)
                        {
                            case 0: // Solid circle (SDF-like)
                                {
                                    float cx = totalWidth / 2f;
                                    float cy = totalHeight / 2f;
                                    float radius = Math.Min(totalWidth, totalHeight) * 0.35f;
                                    
                                    float dx = x - cx;
                                    float dy = y - cy;
                                    float dist = MathF.Sqrt(dx * dx + dy * dy);
                                    
                                    // SDF: inside = high value, outside = low value
                                    float sdf = dist < radius ? 1.0f : 0.0f;
                                    
                                    // Smooth transition
                                    float edge = 2f;
                                    float alpha = Math.Clamp((radius - dist) / edge + 0.5f, 0f, 1f);
                                    
                                    byte val = (byte)(alpha * 255);
                                    r = g = b = val;
                                    break;
                                }

                            case 1: // Gradient circle (test smooth rendering)
                                {
                                    float cx = totalWidth / 2f;
                                    float cy = totalHeight / 2f;
                                    float maxDist = MathF.Sqrt(cx * cx + cy * cy);
                                    
                                    float dx = x - cx;
                                    float dy = y - cy;
                                    float dist = MathF.Sqrt(dx * dx + dy * dy);
                                    
                                    float t = 1.0f - Math.Clamp(dist / maxDist, 0f, 1f);
                                    byte val = (byte)(t * 255);
                                    r = g = b = val;
                                    break;
                                }

                            case 2: // Checkerboard (test texture coordinates)
                                {
                                    int cellSize = 4;
                                    bool checker = ((x / cellSize) + (y / cellSize)) % 2 == 0;
                                    byte val = checker ? (byte)255 : (byte)64;
                                    r = g = b = val;
                                    break;
                                }

                            case 3: // Border box (test padding/bounds)
                                {
                                    bool isBorder = x < df.Padding || x >= totalWidth - df.Padding ||
                                                   y < df.Padding || y >= totalHeight - df.Padding;
                                    
                                    if (isBorder)
                                    {
                                        // Red border
                                        r = 255;
                                        g = 0;
                                        b = 0;
                                    }
                                    else
                                    {
                                        // White center
                                        r = g = b = 200;
                                    }
                                    break;
                                }

                            default:
                                r = g = b = 128;
                                break;
                        }

                        int offset = x * 4;
                        row[offset + 0] = r;
                        row[offset + 1] = g;
                        row[offset + 2] = b;
                        row[offset + 3] = 255; // Full opacity
                    }
                }
            }

            return bmp;
        }
    }
}
