// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Xenko.Core;
using Xenko.Core.Mathematics;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// Applies a McIntosh blur to a texture. (Hexagonal bokeh)
    /// </summary>
    /// <remarks>
    /// This is a 3-pass (+1 final gathering) technique based on the paper of McIntosh from the Simon Fraser University. (2012)
    /// http://ivizlab.sfu.ca/papers/cgf2012.pdf
    /// </remarks>
    public class McIntoshBokeh : BokehBlur
    {
        private ImageEffectShader directionalBlurEffect;
        private ImageEffectShader finalCombineEffect;
        private ImageEffectShader optimizedEffect;

        // Number of tap required along one direction. (Not the total number of tap.)
        private int tapCount;

        // Weight of each tap
        private float[] tapWeights;

        // Simple flag to switch between the debug version or the optimized version
        private bool useOptimizedPath = true;

        /// <summary>
        /// Phase of the bokeh effect. (rotation angle in radian)
        /// </summary>
        public float Phase { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="McIntoshBokeh"/> class.
        /// </summary>
        public McIntoshBokeh()
        {
            Phase = 0f;
        }

        public override float Radius
        {
            get
            {
                return base.Radius;
            }

            set
            {
                //Special case for McIntosh blur: we need to apply a radius double of the final result radius.
                base.Radius = value * 2.0f;

                // Our actual total number of tap
                tapCount = (int)Radius + 1;
            }
        }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            directionalBlurEffect = ToLoadAndUnload(new ImageEffectShader("DepthAwareDirectionalBlurEffect"));
            finalCombineEffect = ToLoadAndUnload(new ImageEffectShader("McIntoshCombineShader"));
            optimizedEffect = ToLoadAndUnload(new ImageEffectShader("McIntoshOptimizedEffect"));
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            // Make sure we keep our uniform weights in synchronization with the number of taps
            if (tapWeights == null || tapWeights.Length != tapCount)
            {
                tapWeights = DoFUtil.GetUniformWeightBlurArray(tapCount);
            }

            if (!useOptimizedPath)
            {
                DrawCoreNaive(context);
            }
            else
            {
                DrawCoreOptimized(context);
            }
        }

        // Naive approach: 4 passes. (Reference version)
        private void DrawCoreNaive(RenderDrawContext context)
        {
            var originalTexture = GetSafeInput(0);
            var outputTexture = GetSafeOutput(0);

            var tapNumber = 2 * tapCount - 1;
            directionalBlurEffect.Parameters.Set(DepthAwareDirectionalBlurKeys.Count, tapCount);
            directionalBlurEffect.Parameters.Set(DepthAwareDirectionalBlurKeys.TotalTap, tapNumber);
            directionalBlurEffect.EffectInstance.UpdateEffect(context.GraphicsDevice);
            directionalBlurEffect.Parameters.Set(DepthAwareDirectionalBlurUtilKeys.Radius, Radius);
            directionalBlurEffect.Parameters.Set(DepthAwareDirectionalBlurUtilKeys.TapWeights, tapWeights);

            // Blur in one direction
            var blurAngle = Phase;
            directionalBlurEffect.Parameters.Set(DepthAwareDirectionalBlurUtilKeys.Direction, new Vector2((float)Math.Cos(blurAngle), (float)Math.Sin(blurAngle)));

            var firstBlurTexture = NewScopedRenderTarget2D(originalTexture.Description);
            directionalBlurEffect.SetInput(0, originalTexture);
            directionalBlurEffect.SetOutput(firstBlurTexture);
            directionalBlurEffect.Draw((RenderDrawContext)context, "McIntoshBokehPass1_tap{0}_radius{1}", tapNumber, (int)Radius);

            // Diagonal blur A
            blurAngle = MathUtil.Pi / 3f + Phase;
            directionalBlurEffect.Parameters.Set(DepthAwareDirectionalBlurUtilKeys.Direction, new Vector2((float)Math.Cos(blurAngle), (float)Math.Sin(blurAngle)));

            var diagonalBlurA = NewScopedRenderTarget2D(originalTexture.Description);
            directionalBlurEffect.SetInput(0, firstBlurTexture);
            directionalBlurEffect.SetOutput(diagonalBlurA);
            directionalBlurEffect.Draw((RenderDrawContext)context, "McIntoshBokehPass2A_tap{0}_radius{1}", tapNumber, (int)Radius);

            // Diagonal blur B
            blurAngle = -MathUtil.Pi / 3f + Phase;
            directionalBlurEffect.Parameters.Set(DepthAwareDirectionalBlurUtilKeys.Direction, new Vector2((float)Math.Cos(blurAngle), (float)Math.Sin(blurAngle)));

            var diagonalBlurB = NewScopedRenderTarget2D(originalTexture.Description);
            directionalBlurEffect.SetInput(0, firstBlurTexture);
            directionalBlurEffect.SetOutput(diagonalBlurB);
            directionalBlurEffect.Draw((RenderDrawContext)context, "McIntoshBokehPass2B_tap{0}_radius{1}", tapNumber, (int)Radius);

            // Final pass outputting the min of A and B
            finalCombineEffect.SetInput(0, diagonalBlurA);
            finalCombineEffect.SetInput(1, diagonalBlurB);
            finalCombineEffect.SetOutput(outputTexture);
            finalCombineEffect.Draw(context, name: "McIntoshBokehPassCombine");
        }

        // Optimized approach: 2 passes.
        private void DrawCoreOptimized(RenderDrawContext context)
        {
            var originalTexture = GetSafeInput(0);

            var outputTexture = GetSafeOutput(0);

            var tapNumber = 2 * tapCount - 1;
            directionalBlurEffect.Parameters.Set(DepthAwareDirectionalBlurKeys.Count, tapCount);
            directionalBlurEffect.Parameters.Set(DepthAwareDirectionalBlurKeys.TotalTap, tapNumber);
            directionalBlurEffect.EffectInstance.UpdateEffect(context.GraphicsDevice);
            directionalBlurEffect.Parameters.Set(DepthAwareDirectionalBlurUtilKeys.Radius, Radius);
            directionalBlurEffect.Parameters.Set(DepthAwareDirectionalBlurUtilKeys.TapWeights, tapWeights);

            // Blur in one direction
            var blurAngle = Phase;
            directionalBlurEffect.Parameters.Set(DepthAwareDirectionalBlurUtilKeys.Direction, new Vector2((float)Math.Cos(blurAngle), (float)Math.Sin(blurAngle)));

            var firstBlurTexture = NewScopedRenderTarget2D(originalTexture.Description);
            directionalBlurEffect.SetInput(0, originalTexture);
            directionalBlurEffect.SetOutput(firstBlurTexture);
            directionalBlurEffect.Draw((RenderDrawContext)context, "McIntoshBokehPass1_tap{0}_radius{1}", tapNumber, (int)Radius);

            // Calculates the 2 diagonal blurs and keep the min of them
            var diagonalBlurAngleA = +MathUtil.Pi / 3f + Phase;
            var diagonalBlurAngleB = -MathUtil.Pi / 3f + Phase;
            optimizedEffect.SetInput(0, firstBlurTexture);
            optimizedEffect.SetOutput(outputTexture);
            optimizedEffect.Parameters.Set(DepthAwareDirectionalBlurKeys.Count, tapCount);
            optimizedEffect.Parameters.Set(DepthAwareDirectionalBlurKeys.TotalTap, tapNumber);
            optimizedEffect.EffectInstance.UpdateEffect(context.GraphicsDevice);
            optimizedEffect.Parameters.Set(DepthAwareDirectionalBlurUtilKeys.Radius.ComposeWith("directionalBlurA"), Radius);
            optimizedEffect.Parameters.Set(DepthAwareDirectionalBlurUtilKeys.Direction.ComposeWith("directionalBlurA"), new Vector2((float)Math.Cos(diagonalBlurAngleA), (float)Math.Sin(diagonalBlurAngleA)));
            optimizedEffect.Parameters.Set(DepthAwareDirectionalBlurUtilKeys.TapWeights.ComposeWith("directionalBlurA"), tapWeights);
            optimizedEffect.Parameters.Set(DepthAwareDirectionalBlurUtilKeys.Radius.ComposeWith("directionalBlurB"), Radius);
            optimizedEffect.Parameters.Set(DepthAwareDirectionalBlurUtilKeys.Direction.ComposeWith("directionalBlurB"), new Vector2((float)Math.Cos(diagonalBlurAngleB), (float)Math.Sin(diagonalBlurAngleB)));
            optimizedEffect.Parameters.Set(DepthAwareDirectionalBlurUtilKeys.TapWeights.ComposeWith("directionalBlurB"), tapWeights);
            optimizedEffect.Draw((RenderDrawContext)context, "McIntoshBokehPass2_BlurABCombine_tap{0}_radius{1}", tapNumber, (int)Radius);
        }
    }
}
