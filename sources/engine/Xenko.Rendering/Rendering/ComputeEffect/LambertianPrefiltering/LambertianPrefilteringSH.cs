// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Rendering.Images;
using Buffer = Xenko.Graphics.Buffer;

namespace Xenko.Rendering.ComputeEffect.LambertianPrefiltering
{
    /// <summary>
    /// Performs Lambertian pre-filtering in the form of Spherical Harmonics.
    /// </summary>
    public class LambertianPrefilteringSH : DrawEffect
    {
        private int harmonicalOrder;

        private readonly ComputeEffectShader firstPassEffect;
        private readonly ComputeEffectShader secondPassEffect;

        private SphericalHarmonics prefilteredLambertianSH;

        /// <summary>
        /// Gets or sets the level of precision desired when calculating the spherical harmonics.
        /// </summary>
        public int HarmonicOrder
        {
            get { return harmonicalOrder; }
            set
            {
                harmonicalOrder = Math.Max(1, Math.Min(5, value));

                firstPassEffect.Parameters.Set(SphericalHarmonicsParameters.HarmonicsOrder, harmonicalOrder);
                secondPassEffect.Parameters.Set(SphericalHarmonicsParameters.HarmonicsOrder, harmonicalOrder);
            }
        }

        /// <summary>
        /// Gets the computed spherical harmonics corresponding to the pre-filtered lambertian.
        /// </summary>
        public SphericalHarmonics PrefilteredLambertianSH { get { return prefilteredLambertianSH; } }

        /// <summary>
        /// Gets or sets the input radiance map to pre-filter.
        /// </summary>
        public Texture RadianceMap { get; set; }

        public LambertianPrefilteringSH(RenderContext context)
            : base(context, "LambertianPrefilteringSH")
        {
            firstPassEffect = new ComputeEffectShader(context) { ShaderSourceName = "LambertianPrefilteringSHEffectPass1" };
            secondPassEffect = new ComputeEffectShader(context) { ShaderSourceName = "LambertianPrefilteringSHEffectPass2" };

            HarmonicOrder = 3;
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            var inputTexture = RadianceMap;
            if (inputTexture == null)
                return;

            const int FirstPassBlockSize = 4;
            const int FirstPassSumsCount = FirstPassBlockSize * FirstPassBlockSize;

            var faceCount = inputTexture.ViewDimension == TextureDimension.TextureCube ? 6 : 1;
            if (faceCount == 1)
            {
                throw new NotSupportedException("Only texture cube are currently supported as input of 'LambertianPrefilteringSH' effect.");
            }
            var inputSize = new Int2(inputTexture.Width, inputTexture.Height); // (Note: for cube maps width = height)
            var coefficientsCount = harmonicalOrder * harmonicalOrder;

            var sumsToPerfomRemaining = inputSize.X * inputSize.Y * faceCount / FirstPassSumsCount;
            var partialSumBuffer = NewScopedTypedBuffer(coefficientsCount * sumsToPerfomRemaining, PixelFormat.R32G32B32A32_Float, true);

            // Project the radiance on the SH basis and sum up the results along the 4x4 blocks
            firstPassEffect.ThreadNumbers = new Int3(FirstPassBlockSize, FirstPassBlockSize, 1);
            firstPassEffect.ThreadGroupCounts = new Int3(inputSize.X / FirstPassBlockSize, inputSize.Y / FirstPassBlockSize, faceCount);
            firstPassEffect.Parameters.Set(LambertianPrefilteringSHParameters.BlockSize, FirstPassBlockSize);
            firstPassEffect.Parameters.Set(SphericalHarmonicsParameters.HarmonicsOrder, harmonicalOrder);
            firstPassEffect.Parameters.Set(LambertianPrefilteringSHPass1Keys.RadianceMap, inputTexture);
            firstPassEffect.Parameters.Set(LambertianPrefilteringSHPass1Keys.OutputBuffer, partialSumBuffer);
            ((RendererBase)firstPassEffect).Draw(context);

            // Recursively applies the pass2 (sums the coefficients together) as long as needed. Swap input/output buffer at each iteration.
            var secondPassInputBuffer = partialSumBuffer;
            Buffer secondPassOutputBuffer = null;
            while (sumsToPerfomRemaining % 2 == 0)
            {
                // we are limited in the number of summing threads by the group-shared memory size.
                // determine the number of threads to use and update the number of sums remaining afterward.
                var sumsCount = 1;
                while (sumsCount < (1 << 10) && sumsToPerfomRemaining % 2 == 0) // shader can perform only an 2^x number of sums.
                {
                    sumsCount <<= 1;
                    sumsToPerfomRemaining >>= 1;
                }

                // determine the numbers of groups (limited to 65535 groups by dimensions)
                var groupCountX = 1;
                var groupCountY = sumsToPerfomRemaining;
                while (groupCountX >= short.MaxValue)
                {
                    groupCountX <<= 1;
                    groupCountY >>= 1;
                }

                // create the output buffer if not existing yet
                if (secondPassOutputBuffer == null)
                    secondPassOutputBuffer = NewScopedTypedBuffer(coefficientsCount * sumsToPerfomRemaining, PixelFormat.R32G32B32A32_Float, true);

                // draw pass 2
                secondPassEffect.ThreadNumbers = new Int3(sumsCount, 1, 1);
                secondPassEffect.ThreadGroupCounts = new Int3(groupCountX, groupCountY, coefficientsCount);
                secondPassEffect.Parameters.Set(LambertianPrefilteringSHParameters.BlockSize, sumsCount);
                secondPassEffect.Parameters.Set(SphericalHarmonicsParameters.HarmonicsOrder, harmonicalOrder);
                secondPassEffect.Parameters.Set(LambertianPrefilteringSHPass2Keys.InputBuffer, secondPassInputBuffer);
                secondPassEffect.Parameters.Set(LambertianPrefilteringSHPass2Keys.OutputBuffer, secondPassOutputBuffer);
                ((RendererBase)secondPassEffect).Draw(context);

                // swap second pass input/output buffers.
                var swapTemp = secondPassOutputBuffer;
                secondPassOutputBuffer = secondPassInputBuffer;
                secondPassInputBuffer = swapTemp;
            }

            // create and initialize result SH
            prefilteredLambertianSH = new SphericalHarmonics(HarmonicOrder);

            // Get the data out of the final buffer
            var sizeResult = coefficientsCount * sumsToPerfomRemaining * PixelFormat.R32G32B32A32_Float.SizeInBytes();
            var stagedBuffer = NewScopedBuffer(new BufferDescription(sizeResult, BufferFlags.None, GraphicsResourceUsage.Staging));
            context.CommandList.CopyRegion(secondPassInputBuffer, 0, new ResourceRegion(0, 0, 0, sizeResult, 1, 1), stagedBuffer, 0);
            var finalsValues = stagedBuffer.GetData<Vector4>(context.CommandList);    
            
            // performs last possible additions, normalize the result and store it in the SH
            for (var c = 0; c < coefficientsCount; c++)
            {
                var coeff = Vector4.Zero;
                for (var f = 0; f < sumsToPerfomRemaining; ++f)
                {
                    coeff += finalsValues[coefficientsCount * f + c];
                }
                prefilteredLambertianSH.Coefficients[c] = 4 * MathUtil.Pi / coeff.W * new Color3(coeff.X, coeff.Y, coeff.Z);
            }
        }
    }
}
