// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.TextureConverter
{
    /// <summary>
    /// Provides enumerations of the different available filter types.
    /// </summary>
    public class Filter
    {
        /// <summary>
        /// Available filters for mipmap generation
        /// </summary>
        public enum MipMapGeneration
        {
            Box,
            Cubic,
            Linear,
            Nearest,
        }

        /// <summary>
        /// Available filters for rescaling operation
        /// </summary>
        public enum Rescaling
        {
            Box = 0,
            Bicubic = 1,
            Bilinear = 2,
            BSpline = 3,
            CatmullRom = 4,
            Lanczos3 = 5,
            Nearest,
        }
    }
}
