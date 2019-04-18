// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Graphics;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// Provides a gaussian blur effect.
    /// </summary>
    /// <remarks>
    /// To improve performance of this gaussian blur is using:
    /// - a separable 1D horizontal and vertical blur
    /// - linear filtering to reduce the number of taps
    /// </remarks>
    [DataContract("GaussianBlur")]
    [Display("Gaussian Blur")]
    public sealed class GaussianBlur : ImageEffect, IImageEffectRenderer // SceneEffectRenderer as GaussianBlur is a simple input/output effect.
    {
        private List<GaussianBlurShader> shaders = new List<GaussianBlurShader>();

        private int radius;

        private float sigmaRatio;

        /// <summary>
        /// Initializes a new instance of the <see cref="GaussianBlur"/> class.
        /// </summary>
        public GaussianBlur()
        {
            Radius = 4;
            SigmaRatio = 3.0f;
        }

        /// <summary>
        /// Gets or sets the radius.
        /// </summary>
        /// <value>The radius.</value>
        /// <userdoc>The radius of the Gaussian in pixels</userdoc>
        [DataMember(10)]
        public int Radius
        {
            get
            {
                return radius;
            }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("Radius cannot be < 1");
                }

                radius = value;
            }
        }

        /// <summary>
        /// Gets or sets the sigma ratio. The sigma ratio is used to calculate the sigma based on the radius: The actual
        /// formula is <c>sigma = radius / SigmaRatio</c>. The default value is 2.0f.
        /// </summary>
        /// <value>The sigma ratio.</value>
        /// <userdoc>The sigma ratio of the Gaussian. The sigma ratio is used to calculate the sigma of the Gaussian. 
        /// The actual formula is <c>sigma = radius / SigmaRatio</c></userdoc>
        [DataMember(20)]
        public float SigmaRatio
        {
            get
            {
                return sigmaRatio;
            }
            set
            {
                if (value < 0.0f)
                {
                    throw new ArgumentOutOfRangeException("SigmaRatio cannot be < 0.0f");
                }

                sigmaRatio = value;
            }
        }

        public void Collect(RenderContext context)
        {
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            // Find gaussian blur shader (if same radius has already been used)
            GaussianBlurShader matchingGaussianBlurShader = null;
            foreach (var gaussianBlurShader in shaders)
            {
                if (gaussianBlurShader.Radius == Radius)
                {
                    matchingGaussianBlurShader = gaussianBlurShader;
                    break;
                }
            }

            // Not found, create it
            if (matchingGaussianBlurShader == null)
            {
                matchingGaussianBlurShader = new GaussianBlurShader(this, Radius);
                shaders.Add(matchingGaussianBlurShader);
            }

            // Perform the gaussian blur
            matchingGaussianBlurShader.Draw(context, SigmaRatio, GetSafeInput(0), GetSafeOutput(0));
        }

        /// <summary>
        /// Store a Gaussian Blur shader pair. If we didn't do so, it might trigger a LoadEffect every time the radius changes.
        /// </summary>
        private class GaussianBlurShader
        {
            public readonly int Radius;

            private readonly GaussianBlur gaussianBlur;
            private readonly ImageEffectShader blurH;
            private readonly ImageEffectShader blurV;
            private readonly string nameGaussianBlurH;
            private readonly string nameGaussianBlurV;

            private float sigmaRatio;
            private Vector2[] offsetsWeights;

            public GaussianBlurShader(GaussianBlur gaussianBlur, int radius)
            {
                Radius = radius;
                this.gaussianBlur = gaussianBlur;

                // Craete ImageEffectShader
                blurH = gaussianBlur.ToLoadAndUnload(new ImageEffectShader("GaussianBlurEffect", true));
                blurV = gaussianBlur.ToLoadAndUnload(new ImageEffectShader("GaussianBlurEffect", true));
                blurH.Initialize(gaussianBlur.Context);
                blurV.Initialize(gaussianBlur.Context);

                // Setup Horizontal parameters
                blurH.Parameters.Set(GaussianBlurKeys.VerticalBlur, false);
                blurV.Parameters.Set(GaussianBlurKeys.VerticalBlur, true);

                var size = radius * 2 + 1;
                nameGaussianBlurH = string.Format("GaussianBlurH{0}x{0}", size);
                nameGaussianBlurV = string.Format("GaussianBlurV{0}x{0}", size);

                // TODO: cache if necessary
                offsetsWeights = GaussianUtil.Calculate1D(this.Radius, gaussianBlur.SigmaRatio);

                // Update permutation parameters
                blurH.Parameters.Set(GaussianBlurKeys.Count, offsetsWeights.Length);
                blurV.Parameters.Set(GaussianBlurKeys.Count, offsetsWeights.Length);
                blurH.EffectInstance.UpdateEffect(gaussianBlur.Context.GraphicsDevice);
                blurV.EffectInstance.UpdateEffect(gaussianBlur.Context.GraphicsDevice);

                // Update parameters
                blurH.Parameters.Set(GaussianBlurShaderKeys.OffsetsWeights, offsetsWeights);
                blurV.Parameters.Set(GaussianBlurShaderKeys.OffsetsWeights, offsetsWeights);
            }

            public void Draw(RenderDrawContext context, float sigmaRatio, Texture inputTexture, Texture outputTexture)
            {
                // Check if we need to regenerate offsetsWeights
                if (offsetsWeights == null || this.sigmaRatio != sigmaRatio)
                {
                    offsetsWeights = GaussianUtil.Calculate1D(Radius, sigmaRatio);

                    // Update parameters
                    blurH.Parameters.Set(GaussianBlurShaderKeys.OffsetsWeights, offsetsWeights);
                    blurV.Parameters.Set(GaussianBlurShaderKeys.OffsetsWeights, offsetsWeights);

                    this.sigmaRatio = sigmaRatio;
                }

                // Get a temporary texture for the intermediate pass
                // This texture will be allocated only in the scope of this draw and returned to the pool at the exit of this method
                var desc = inputTexture.Description;
                desc.MultisampleCount = MultisampleCount.None; // TODO we should have a method to get a non-multisampled RT
                var outputTextureH = gaussianBlur.NewScopedRenderTarget2D(desc);

                // Horizontal pass
                blurH.SetInput(inputTexture);
                blurH.SetOutput(outputTextureH);
                blurH.Draw(context, nameGaussianBlurH);

                // Vertical pass
                blurV.SetInput(outputTextureH);
                blurV.SetOutput(outputTexture);
                blurV.Draw(context, nameGaussianBlurV);
            }
        }
    }
}
