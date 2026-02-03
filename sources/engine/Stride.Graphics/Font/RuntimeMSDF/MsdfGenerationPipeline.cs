using System;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Graphics.Font.RuntimeMsdf
{
    /// <summary>
    /// Settings that are common across SDF/MSDF generators.
    /// Keep this independent from any particular font class so we can reuse it
    /// for pipeline-time atlas gen or runtime glyph gen.
    /// </summary>
    public readonly record struct DistanceFieldSettings(
        int PixelRange,
        int Padding,
        int Width,
        int Height)
    {
        public int TotalWidth => Width + Padding * 2;
        public int TotalHeight => Height + Padding * 2;
    }

    /// <summary>
    /// MSDF output encoding choices.
    /// Most MSDF implementations output float RGB, then you pack to RGBA8.
    /// </summary>
    public readonly record struct MsdfEncodeSettings(float Bias, float Scale)
    {
        public static readonly MsdfEncodeSettings Default = new(Bias: 0.5f, Scale: 0.5f);
    }

    /// <summary>
    /// Library-agnostic MSDF rasterizer interface.
    /// Implementations can wrap Remora.MSDFGen today, and be swapped later.
    /// </summary>
    public interface IGlyphMsdfRasterizer
    {
        /// <summary>
        /// Rasterizes an MSDF (RGB packed into RGBA8) for the provided glyph outline.
        /// The output bitmap is expected to be (Width+2*Padding) x (Height+2*Padding).
        /// </summary>
        internal CharacterBitmapRgba RasterizeMsdf(
            GlyphOutline outline,
            DistanceFieldSettings df,
            MsdfEncodeSettings encode);
    }

    /// <summary>
    /// Placeholder implementation.
    ///
    /// NOTE: This compiles without taking a hard dependency on Remora.MSDFGen.
    /// When you're ready, create a second file that references the NuGet package
    /// directly (e.g. RemoraMsdfRasterizer.cs) and plug it in through your generator seam.
    /// </summary>
/*    public sealed class NotImplementedMsdfRasterizer : IGlyphMsdfRasterizer
    {
        internal CharacterBitmapRgba RasterizeMsdf(GlyphOutline outline, DistanceFieldSettings df, MsdfEncodeSettings encode)
        {
            IGlyphMsdfRasterizer rasterizer = new RemoraMsdfRasterizer();
        }
    }*/
}
