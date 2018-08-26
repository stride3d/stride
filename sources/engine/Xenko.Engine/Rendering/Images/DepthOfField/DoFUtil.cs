// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// Some util function relevant to the depth-of-field effect.
    /// </summary>
    internal class DoFUtil
    {
        /// <summary>
        /// Creates an array with uniform weight along one direction of the blur. 
        /// </summary>
        /// <param name="count">Number of taps from the center (included) along one direction.</param>
        /// <returns>The array with uniform weights.</returns>
        public static float[] GetUniformWeightBlurArray(int count)
        {
            // Total number of taps
            var tapNumber = 2 * count - 1;
            var uniformWeight = 1f / tapNumber;
            float[] result = new float[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = uniformWeight;
            }
            return result;
        }
    }
}
