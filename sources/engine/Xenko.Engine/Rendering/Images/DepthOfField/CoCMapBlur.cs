// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Mathematics;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// Blurs a Circle of Confusion map.
    /// </summary>
    /// <remarks>
    /// This is useful to avoid strong CoC changes leading to out-of-focus silhouette outline appearing in 
    /// front of another out-of-focus object.
    /// Internally it uses a special gaussian blur aware of the depth.
    /// </remarks>
    public class CoCMapBlur : ImageEffect
    {
        private ImageEffectShader cocBlurEffect;

        private float radius;

        private int tapCount;

        private Vector2[] tapWeights;

        private bool weightsDirty = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoCMapBlur"/> class.
        /// </summary>
        public CoCMapBlur()
            : base()
        {
            Radius = 5f;
        }

        /// <summary>
        /// Gets or sets the radius.
        /// </summary>
        /// <value>The radius.</value>
        public float Radius
        {
            get
            {
                return radius;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("Radius cannot be < 0");
                }

                weightsDirty = (radius != value);
                radius = value;
            }
        }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            cocBlurEffect = ToLoadAndUnload(new ImageEffectShader("CoCMapBlurEffect"));
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            // Updates the weight array if necessary
            if (weightsDirty || tapCount == 0)
            {
                weightsDirty = false;
                Vector2[] gaussianWeights = GaussianUtil.Calculate1D((int)radius, 2f, true);
                tapCount = gaussianWeights.Length;
                tapWeights = gaussianWeights;
            }

            var originalTexture = GetSafeInput(0);
            var outputTexture = GetSafeOutput(0);

            cocBlurEffect.Parameters.Set(DepthAwareDirectionalBlurKeys.Count, tapCount);
            cocBlurEffect.EffectInstance.UpdateEffect(context.GraphicsDevice);
            cocBlurEffect.Parameters.Set(CoCMapBlurShaderKeys.Radius, radius);
            cocBlurEffect.Parameters.Set(CoCMapBlurShaderKeys.OffsetsWeights, tapWeights);
            var tapNumber = 2 * tapCount - 1;

            // Blur in one direction
            var blurAngle = 0f;
            cocBlurEffect.Parameters.Set(CoCMapBlurShaderKeys.Direction, new Vector2((float)Math.Cos(blurAngle), (float)Math.Sin(blurAngle)));

            var firstBlurTexture = NewScopedRenderTarget2D(originalTexture.Description);
            cocBlurEffect.SetInput(0, originalTexture);
            cocBlurEffect.SetOutput(firstBlurTexture);
            cocBlurEffect.Draw(context, "CoCMapBlurPass1_tap{0}_radius{1}", tapNumber, (int)radius);

            // Second blur pass to ouput the final result
            blurAngle = MathUtil.PiOverTwo;
            cocBlurEffect.Parameters.Set(CoCMapBlurShaderKeys.Direction, new Vector2((float)Math.Cos(blurAngle), (float)Math.Sin(blurAngle)));

            cocBlurEffect.SetInput(0, firstBlurTexture);
            cocBlurEffect.SetOutput(outputTexture);
            cocBlurEffect.Draw(context, "CoCMapBlurPass2_tap{0}_radius{1}", tapNumber, (int)radius);
        }
    }
}
