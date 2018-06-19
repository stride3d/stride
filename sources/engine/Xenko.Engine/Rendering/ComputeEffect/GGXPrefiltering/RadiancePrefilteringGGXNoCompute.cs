// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Rendering.Images;

namespace Xenko.Rendering.ComputeEffect.GGXPrefiltering
{
    /// <summary>
    /// A class for radiance pre-filtering using the GGX distribution function.
    /// </summary>
    public class RadiancePrefilteringGGXNoCompute : DrawEffect
    {
        private int samplingsCount;

        private readonly ImageEffectShader shader;

        /// <summary>
        /// Gets or sets the boolean indicating if the highest level of mipmaps should be let as-is or pre-filtered.
        /// </summary>
        public bool DoNotFilterHighestLevel { get; set; }

        /// <summary>
        /// Gets or sets the input radiance map to pre-filter.
        /// </summary>
        public Texture RadianceMap { get; set; }

        /// <summary>
        /// Gets or sets the texture to use to store the result of the pre-filtering.
        /// </summary>
        public Texture PrefilteredRadiance { get; set; }

        /// <summary>
        /// Gets or sets the number of pre-filtered mipmap to generate.
        /// </summary>
        public int MipmapGenerationCount { get; set; }

        private ImageScaler scaler;

        /// <summary>
        /// Create a new instance of the class.
        /// </summary>
        /// <param name="context">the context</param>
        public RadiancePrefilteringGGXNoCompute(RenderContext context)
            : base(context, "RadiancePrefilteringGGX")
        {
            shader = new ImageEffectShader("RadiancePrefilteringGGXNoComputeEffect");
            DoNotFilterHighestLevel = true;
            samplingsCount = 1024;

            scaler = new ImageScaler(SamplingPattern.Expanded);
            scaler.Initialize(context);
        }

        /// <summary>
        /// Gets or sets the number of sampling used during the importance sampling
        /// </summary>
        /// <remarks>Should be a power of 2 and maximum value is 1024</remarks>
        public int SamplingsCount
        {
            get { return samplingsCount; }
            set
            {
                if (value > 1024)
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (!MathUtil.IsPow2(value))
                    throw new ArgumentException("The provided value should be a power of 2");

                samplingsCount = Math.Max(1, value);
            }
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            var output = PrefilteredRadiance;
            if (output == null || (output.ViewDimension != TextureDimension.Texture2D && output.ViewDimension != TextureDimension.TextureCube) || output.ArraySize != 6)
                throw new NotSupportedException("Only array of 2D textures are currently supported as output");

            if (!output.IsRenderTarget)
                throw new NotSupportedException("Only render targets are supported as output");

            var input = RadianceMap;
            if (input == null || input.ViewDimension != TextureDimension.TextureCube)
                throw new NotSupportedException("Only cubemaps are currently supported as input");

            var roughness = 0f;
            var faceCount = output.ArraySize;
            var levelSize = new Int2(output.Width, output.Height);
            var mipCount = MipmapGenerationCount == 0 ? output.MipLevels : MipmapGenerationCount;

            for (int mipLevel = 0; mipLevel < mipCount; mipLevel++)
            {
                for (int faceIndex = 0; faceIndex < faceCount; faceIndex++)
                {
                    using (var outputView = output.ToTextureView(ViewType.Single, faceIndex, mipLevel))
                    {
                        var inputLevel = MathUtil.Log2(input.Width / output.Width);
                        if (mipLevel == 0 && DoNotFilterHighestLevel)
                        {
                            if (input.Width >= output.Width && inputLevel < input.MipLevels && input.Format == output.Format)
                            {
                                // Optimization: make a simple copy of the texture when possible
                                var inputSubresource = inputLevel + faceIndex * input.MipLevels;
                                var outputSubresource = 0 + faceIndex * output.MipLevels;
                                context.CommandList.CopyRegion(input, inputSubresource, null, output, outputSubresource);
                            }
                            else // otherwise rescale the closest mipmap
                            {
                                var inputMipmapLevel = Math.Min(inputLevel, input.MipLevels - 1);
                                using (var inputView = input.ToTextureView(ViewType.Single, faceIndex, inputMipmapLevel))
                                {
                                    scaler.SetInput(inputView);
                                    scaler.SetOutput(outputView);
                                    scaler.Draw(context);
                                }
                            }
                        }
                        else
                        {
                            shader.Parameters.Set(RadiancePrefilteringGGXNoComputeShaderKeys.Face, faceIndex);
                            shader.Parameters.Set(RadiancePrefilteringGGXNoComputeShaderKeys.Roughness, roughness);
                            shader.Parameters.Set(RadiancePrefilteringGGXNoComputeShaderKeys.MipmapCount, input.MipLevels - 1);
                            shader.Parameters.Set(RadiancePrefilteringGGXNoComputeShaderKeys.RadianceMap, input);
                            shader.Parameters.Set(RadiancePrefilteringGGXNoComputeShaderKeys.RadianceMapSize, input.Width);
                            shader.Parameters.Set(RadiancePrefilteringGGXNoComputeParams.NbOfSamplings, SamplingsCount);
                            shader.SetOutput(outputView);
                            shader.Draw(context);
                        }
                    }
                }

                if (mipCount > 1)
                {
                    roughness += 1f / (mipCount - 1);
                    levelSize /= 2;
                }
            }
        }
    }
}
