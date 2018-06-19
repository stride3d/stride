using System;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Graphics;

namespace Xenko.Rendering.Images
{
    [DataContract("TemporalAntiAliasEffect")]
    public class TemporalAntiAliasEffect : ImageEffectShader, IScreenSpaceAntiAliasingEffect
    {
        private int jitteringFrameCount;
        private ImageScaler textureScaler;
        private Texture previousTexture;

        private static readonly Vector2[] SampleOffsets = new[]
        {
            new Vector2(-1.0f, -1.0f ),
            new Vector2( 0.0f, -1.0f ),
            new Vector2( 1.0f, -1.0f ),
            new Vector2(-1.0f,  0.0f ),
            new Vector2( 1.0f,  0.0f ),
            new Vector2(-1.0f,  1.0f ),
            new Vector2( 0.0f,  1.0f ),
            new Vector2( 1.0f,  1.0f ),
            new Vector2( 0.0f,  0.0f )
        };

        const int JitterSamples = 16;
        private static readonly Int2[] JitterPositions =
        {
            new Int2( 3, 0 ), new Int2( 10, 9 ), new Int2( 13, 7 ), new Int2( 4, 14 ),    //  0, 1, 2, 3
            new Int2( 11, 1 ), new Int2( 2, 8 ), new Int2( 12, 15 ), new Int2( 5, 6 ),    //  4, 5, 6, 7
            new Int2( 15, 3 ), new Int2( 6, 10 ), new Int2( 8, 13 ), new Int2( 1, 4 ),    //  8, 9,10,11
            new Int2( 14, 11 ), new Int2( 7, 2 ), new Int2( 0, 12 ), new Int2( 9, 5 ),    // 12,13,14,15
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="TemporalAntiAliasEffect"/> class.
        /// </summary>
        public TemporalAntiAliasEffect() : base("TemporalAntiAliasShader")
        {
        }

        public bool NeedRangeDecompress => false;

        public bool RequiresVelocityBuffer => true;

        [DefaultValue(0.01f)]
        public float JitteringMagnitude { get; set; } = 0.01f;
        [DefaultValue(0.125f)]
        public float BlendWeightMin { get; set; } = 0.125f;
        [DefaultValue(0.5f)]

        public float BlendWeightMax { get; set; } = 0.5f;
        [DefaultValue(2.0f)]

        public float HistoryBlurAmp { get; set; } = 2.0f;
        [DefaultValue(128.0f)]

        public float LumaContrastFactor { get; set; } = 128.0f;
        [DefaultValue(0.5f)]

        public float VelocityDecay { get; set; } = 0.5f;

        protected override void InitializeCore()
        {
            base.InitializeCore();

            textureScaler = ToLoadAndUnload(new ImageScaler());
        }

        protected override void Destroy()
        {
            textureScaler.Dispose();

            base.Destroy();
        }

        protected override unsafe void UpdateParameters()
        {
            base.UpdateParameters();

            Vector2 jitterPixels;

            jitteringFrameCount++;
            jitteringFrameCount %= 16;

            // Compute jittering


            var jitteringOffset = SampleJitterNormalized16(jitteringFrameCount) * JitteringMagnitude;

            jitterPixels.X = jitteringOffset[0];
            jitterPixels.Y = -jitteringOffset[1];

            const float Sharpness = 1.0f + (-0.25f) * 0.5f;

            float totalWeight = 0.0f;
            float totalWeightLow = 0.0f;

            float weightCenter;
            float weightLowCenter;
            float* weights = stackalloc float[8];
            float* weightLows = stackalloc float[8];

            for (var i = 0; i < 8; ++i)
            {
                // Exponential fit to Blackman-Harris 3.3
                float PixelOffsetX = SampleOffsets[i][0] - jitterPixels[0];
                float PixelOffsetY = SampleOffsets[i][1] - jitterPixels[1];
                PixelOffsetX *= Sharpness;
                PixelOffsetY *= Sharpness;
                weights[i] = (float)Math.Exp(-2.29f * (PixelOffsetX * PixelOffsetX + PixelOffsetY * PixelOffsetY) );
                totalWeight += weights[i];

                // Lowpass.
                PixelOffsetX = SampleOffsets[i][0] - jitterPixels[0];
                PixelOffsetY = SampleOffsets[i][1] - jitterPixels[1];
                PixelOffsetX *= 0.25f;
                PixelOffsetY *= 0.25f;
                PixelOffsetX *= Sharpness;
                PixelOffsetY *= Sharpness;
                weightLows[i] = (float)Math.Exp(-2.29f * (PixelOffsetX * PixelOffsetX + PixelOffsetY * PixelOffsetY) );
                totalWeightLow += weightLows[i];
            }

            {
                float PixelOffsetX = SampleOffsets[8][0] - jitterPixels[0];
                float PixelOffsetY = SampleOffsets[8][1] - jitterPixels[1];
                PixelOffsetX *= Sharpness;
                PixelOffsetY *= Sharpness;
                weightCenter = (float)Math.Exp(-2.29f * (PixelOffsetX * PixelOffsetX + PixelOffsetY * PixelOffsetY) );
                totalWeight += weightCenter;
                weightCenter /= totalWeight;

                // Lowpass.
                PixelOffsetX = SampleOffsets[8][0] - jitterPixels[0];
                PixelOffsetY = SampleOffsets[8][1] - jitterPixels[1];
                PixelOffsetX *= 0.25f;
                PixelOffsetY *= 0.25f;
                PixelOffsetX *= Sharpness;
                PixelOffsetY *= Sharpness;
                weightLowCenter = (float)Math.Exp(-2.29f * (PixelOffsetX * PixelOffsetX + PixelOffsetY * PixelOffsetY) );
                totalWeightLow += weightLowCenter;
                weightLowCenter /= totalWeightLow;
            }

            for (var i = 0; i < 8; ++i)
            {
                weights[i] /= totalWeight;
                weightLows[i] /= totalWeightLow;
            }

            // Set cbuffer variables
            Parameters.Set(TemporalAntiAliasShaderKeys.u_BlendWeightMin, BlendWeightMin);
            Parameters.Set(TemporalAntiAliasShaderKeys.u_BlendWeightMax, BlendWeightMax);
            Parameters.Set(TemporalAntiAliasShaderKeys.u_HistoryBlurAmp, HistoryBlurAmp);
            Parameters.Set(TemporalAntiAliasShaderKeys.u_LumaContrastFactor, LumaContrastFactor);
            Parameters.Set(TemporalAntiAliasShaderKeys.u_VelocityDecay, VelocityDecay);

            Parameters.Set(TemporalAntiAliasShaderKeys.u_WeightCenter, weightCenter);
            Parameters.Set(TemporalAntiAliasShaderKeys.u_WeightLowCenter, weightLowCenter);
            Parameters.Set(TemporalAntiAliasShaderKeys.u_Weight1, *(Vector4*)&weights[0]);
            Parameters.Set(TemporalAntiAliasShaderKeys.u_Weight2, *(Vector4*)&weights[1]);
            Parameters.Set(TemporalAntiAliasShaderKeys.u_WeightLow1, *(Vector4*)&weights[0]);
            Parameters.Set(TemporalAntiAliasShaderKeys.u_WeightLow2, *(Vector4*)&weights[1]);
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            // Note: if no previous texture yet, skip rendering
            if (previousTexture != null)
            {
                SetInput(3, previousTexture);
                base.DrawCore(context);
            }

            // Copy current state for next frame
            {
                var input = GetInput(0);
                // Do we need to reallocate?
                if (previousTexture == null || previousTexture.Width != input.Width || previousTexture.Height != input.Height)
                {
                    if (previousTexture != null)
                        context.GraphicsContext.Allocator.ReleaseReference(previousTexture);

                    // Note: no need for multisample
                    var description = input.Description;
                    description.MultisampleCount = MultisampleCount.None;
                    previousTexture = context.GraphicsContext.Allocator.GetTemporaryTexture(description);
                }

                textureScaler.SetInput(input);
                textureScaler.SetOutput(previousTexture);
                textureScaler.Draw(context);
            }
        }


        Vector2 SampleJitterNormalized16(int nIndex)
        {
            ref var pPosition = ref SampleJitter16(nIndex);
            int x = pPosition[0];
            int y = pPosition[1];

            var afOutOffset = new Vector2(x, y);

            const float fOffset = (float)JitterSamples * 0.5f + 0.5f;
            const float fScale = 1.0f / (float)JitterSamples;

            return new Vector2(
                (afOutOffset[0] + fOffset) * fScale,
                (afOutOffset[1] + fOffset) * fScale);
        }

        ref Int2 SampleJitter16(int nIndex)
        {
            nIndex = nIndex % JitterSamples;
            return ref JitterPositions[nIndex];
        }
}
}