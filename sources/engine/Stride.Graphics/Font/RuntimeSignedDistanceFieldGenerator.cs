using System;
using Stride.Core.Mathematics;
using Stride.Graphics.Font.RuntimeMsdf;

namespace Stride.Graphics.Font
{
    internal sealed partial class RuntimeSignedDistanceFieldSpriteFont
    {
        // Distance field configuration
        private readonly record struct DistanceEncodeParams(float Bias, float Scale);
        private readonly record struct DistanceFieldParams(int PixelRange, int Pad, DistanceEncodeParams Encode);

        private readonly record struct GlyphKey(char C, int PixelRange, int Pad);

        private static readonly DistanceEncodeParams DefaultEncode = new(Bias: 0.4f, Scale: 0.5f);

        private DistanceFieldParams GetDistanceFieldParams()
        {
            int pixelRange = Math.Max(1, PixelRange);
            int pad = ComputeTotalPad();
            return new DistanceFieldParams(pixelRange, pad, DefaultEncode);
        }

        private static GlyphKey MakeKey(char c, DistanceFieldParams p) => new(c, p.PixelRange, p.Pad);

        // Generator input: coverage today, outline/MSDF when available
        private abstract record GlyphInput;

        private sealed record CoverageInput(
            byte[] Buffer,
            int Length,
            int Width,
            int Rows,
            int Pitch) : GlyphInput;

        private sealed record OutlineInput(
            GlyphOutline Outline,
            int Width,
            int Height) : GlyphInput;

        private interface IDistanceFieldGenerator
        {
            CharacterBitmapRgba Generate(GlyphInput input, DistanceFieldParams p);
        }

        private sealed class SdfCoverageGenerator : IDistanceFieldGenerator
        {
            public CharacterBitmapRgba Generate(GlyphInput input, DistanceFieldParams p)
                => input switch
                {
                    CoverageInput c => BuildSdfRgbFromCoverage(c.Buffer, c.Width, c.Rows, c.Pitch, p.Pad, p.PixelRange, p.Encode),
                    _ => throw new ArgumentOutOfRangeException(nameof(input), "Unsupported input for SDF generator."),
                };
        }

        /// <summary>
        /// Composite generator: CoverageInput -> SDF, OutlineInput -> MSDF.
        /// Keeps the scheduling/upload pipeline unchanged while allowing generator swaps.
        /// </summary>
        private sealed class SdfOrMsdfGenerator(IGlyphMsdfRasterizer msdf, MsdfEncodeSettings msdfEncode) : IDistanceFieldGenerator
        {
            private readonly SdfCoverageGenerator sdf = new();
            private readonly IGlyphMsdfRasterizer msdf = msdf ?? throw new ArgumentNullException(nameof(msdf));
            private readonly MsdfEncodeSettings msdfEncode = msdfEncode;

            public CharacterBitmapRgba Generate(GlyphInput input, DistanceFieldParams p)
                => input switch
                {
                    CoverageInput => sdf.Generate(input, p),
                    OutlineInput o => msdf.RasterizeMsdf(
                        o.Outline,
                        new DistanceFieldSettings(
                            PixelRange: p.PixelRange,
                            Padding: p.Pad,
                            Width: o.Width,
                            Height: o.Height),
                        msdfEncode),
                    _ => throw new ArgumentOutOfRangeException(nameof(input)),
                };
        }

        // Swap MSDF backend here without touching the rest of the runtime font pipeline.
        private readonly IDistanceFieldGenerator generator =
            new SdfOrMsdfGenerator(new MsdfGenCoreRasterizer(), MsdfEncodeSettings.Default);
    }
}
