// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xenko.Core.Mathematics;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// Utility class to calculate 1D Gaussian filter used for separable 2D Gaussian filters.
    /// </summary>
    internal class GaussianUtil
    {
        /// <summary>
        /// Calculate a 1D gaussian filter.
        /// </summary>
        /// <param name="radius">The radius.</param>
        /// <param name="sigmaRatio">The sigma ratio. Default is ratio is 2.0f. Sigma = radius / SigmaRatio</param>
        /// <param name="disableBilinear">if set to <c>true</c> to disable bilinear offsets/weights.</param>
        /// <returns>An array of offsets (<see cref="Vector2.X" />) and weights (<see cref="Vector2.Y" />).</returns>
        public static Vector2[] Calculate1D(int radius, float sigmaRatio, bool disableBilinear = false)
        {
            if (radius < 1)
                radius = 1;

            var sigma = radius / sigmaRatio;

            // Precalculate a default sigma if not specified
            if (MathUtil.IsZero(sigma))
            {
                sigma = radius / 2.0f;
            }

            var sigma2 = sigma * sigma;
            var sigmaDivInner = 1.0 / (2.0 * sigma2);

            double total = 1.0;

            Vector2[] offsetsWeights;
            double[] localWeights;

            if (disableBilinear)
            {
                int count = radius + 1;
                offsetsWeights = new Vector2[count];
                localWeights = new double[count];

                for (int i = 1; i <= radius; i++)
                {
                    var weight = Math.Exp(-Math.Pow(i, 2) * sigmaDivInner);
                    offsetsWeights[i].X = i;
                    localWeights[i] = weight;
                    total += 2.0 * weight;  // weight*2 because offsets/weights are mirrored
                }
            }
            else
            {
                // Calculate offsets and weights with LinearSampling
                int count = (radius + 1) / 2 + 1;
                offsetsWeights = new Vector2[count];
                localWeights = new double[count];
                int index = 1;

                for (int i = 1; i <= radius; i += 2)
                {
                    double w0 = Math.Exp(-Math.Pow(i, 2) * sigmaDivInner);
                    double offset;
                    double weight;

                    if (i == radius)
                    {
                        // edge case
                        offset = i;
                        weight = w0;
                    }
                    else
                    {
                        // Calculate offsets & weights with bilinear filtering
                        double w1 = Math.Exp(-Math.Pow(i + 1, 2) * sigmaDivInner);
                        offset = (i + w1 / (w0 + w1));
                        weight = (w0 + w1);
                    }
                    offsetsWeights[index].X = (float)offset;
                    localWeights[index] = weight;
                    total += 2.0 * weight;  // weight*2 because offsets/weights are mirrored
                    index++;
                }
            }

            // Mormalize weights so that sum is 1.0 (normal distribution, energy conservative)
            localWeights[0] = 1.0;
            for (int i = 0; i < localWeights.Length; i++)
                offsetsWeights[i].Y = (float)(localWeights[i] / total);

            return offsetsWeights;
        }
    }
}
