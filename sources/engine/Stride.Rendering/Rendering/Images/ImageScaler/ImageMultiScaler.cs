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
    /// Allow to up-scale or down-scale a texture (input) multiple times while capturing intermediate mipmap results (outputs).
    /// </summary>
    /// <remarks>
    /// Based on the input texture and output texture, this class automatically downscale or upscale the input texture to the different output textures. 
    /// The requirement for the output textures are:
    /// <ul>
    /// <li>They must have a mipsize compatible with input texture (multiple of 2).</li>
    /// <li>They must share the same pixel format.</li>
    /// <li>They must be a different size from input texture.</li>
    /// <li>They must scale to a single direction (either down or up scale, but not both).</li>
    /// </ul>
    /// </remarks>
    public class ImageMultiScaler : ImageEffect
    {
        private readonly List<Texture> outputTextures = new List<Texture>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageMultiScaler"/> class.
        /// </summary>
        public ImageMultiScaler()
            : this(false)
        {
        }

        public ImageMultiScaler(bool useOverSampling)
            : base(null, useOverSampling)
        {
            // We are not using the default output for render targets, so don't setup them
            EnableSetRenderTargets = false;
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            var inputTexture = GetSafeInput(0);

            // Output pixel format
            PixelFormat outputPixelFormat;
            // Scaling direction (<0 downscale, >0 upscale)
            int scalingDirection;

            // Validate and Prepare scaling 
            if (!PrepareScaling(inputTexture, out scalingDirection, out outputPixelFormat))
            {
                return;
            }

            // Make sure that we are using a clean Scaler.
            Scaler.Reset();

            var nextSize = inputTexture.Size;
            nextSize.Depth = 1;

            var previousMipMap = inputTexture;
            int matchOutputCount = 0;
            while (matchOutputCount != outputTextures.Count)
            {
                nextSize = nextSize.Mip(scalingDirection);
                var mipmap = FindOutputMatchingSize(nextSize, scalingDirection);
                if (mipmap != null)
                {
                    matchOutputCount++;

                    // The size is now the intermediate texture
                    nextSize = mipmap.Size;
                }
                else
                {
                    mipmap = NewScopedRenderTarget2D(nextSize.Width, nextSize.Height, outputPixelFormat, 1);
                }

                // Down or UpScale
                Scaler.SetInput(previousMipMap);
                Scaler.SetOutput(mipmap);
                Scaler.Draw(context, name: scalingDirection < 0 ? "Down2" : "Up2");

                previousMipMap = mipmap;
            }

            // Cleanup output textures so that we don't hold a reference
            outputTextures.Clear();
        }

        /// <summary>
        /// Prepares the scaling.
        /// </summary>
        /// <param name="inputTexture">The input texture.</param>
        /// <param name="scalingDirection">The scaling direction.</param>
        /// <param name="outputPixelFormat">The output pixel format.</param>
        /// <returns><c>true</c> if we have some output to process; otherwise <c>false</c>.</returns>
        private bool PrepareScaling(Texture inputTexture, out int scalingDirection, out PixelFormat outputPixelFormat)
        {
            // TODO: support for intermediate output with non-matching size

            // Query all ouptut
            outputTextures.Clear();

            outputPixelFormat = PixelFormat.None;
            scalingDirection = 0;

            var maxSize = new Size3(1 << 15, 1 << 15, 1 << 15);
            var minSize = Size3.One;

            var inputSize = inputTexture.Size;
            for (int i = 0; i < OutputCount; i++)
            {
                var outputTexture = GetOutput(i);
                if (outputTexture != null)
                {
                    // Verify pixel format
                    if (outputPixelFormat != PixelFormat.None && outputPixelFormat != outputTexture.ViewFormat)
                    {
                        throw new InvalidOperationException("Output texture format [{0}] is not matching other output texture format [{1}]".ToFormat(outputTexture.ViewFormat, outputPixelFormat));
                    }
                    outputPixelFormat = outputTexture.ViewFormat;

                    var outputSize = outputTexture.Size;

                    // Verify pixel format
                    if (outputSize < minSize || outputSize > maxSize)
                    {
                        throw new InvalidOperationException("Unsupported texture size [{0}] out of limit [{1} - {2}]".ToFormat(outputTexture.Size, minSize, maxSize));
                    }

                    if (inputSize == outputSize)
                    {
                        throw new InvalidOperationException("Input and output texture cannot have same size [{0}]".ToFormat(inputSize));
                    }

                    int newScalingDirection = outputSize.CompareTo(inputSize);
                    if (scalingDirection != 0 && Math.Sign(scalingDirection) != Math.Sign(newScalingDirection))
                    {
                        throw new InvalidOperationException("Support only output scaling to the same direction");
                    }
                    scalingDirection = newScalingDirection;

                    // Check that we are scaling to different texture sizes
                    foreach (var existingOutput in outputTextures)
                    {
                        if (existingOutput.Size == outputTexture.Size)
                        {
                            throw new InvalidOperationException("A texture with size [{0}] already exist with the same output size".ToFormat(existingOutput.Size));
                        }
                    }

                    // If the textrue is valid use it
                    outputTextures.Add(outputTexture);
                }
            }

            return outputTextures.Count > 0;
        }

        private Texture FindOutputMatchingSize(Size3 targetSize, int scalingDirection)
        {
            for (int i = 0; i < outputTextures.Count; i++)
            {
                var outputTexture = outputTextures[i];
                if (outputTexture == null)
                {
                    continue;
                }

                if ((scalingDirection < 0 && outputTexture.Size >= targetSize) || (scalingDirection > 0 && outputTexture.Size <= targetSize))
                {
                    // Remove the texture from the pool
                    outputTextures[i] = null;
                    return outputTexture;
                }
            }
            return null;
        }
    }
}
