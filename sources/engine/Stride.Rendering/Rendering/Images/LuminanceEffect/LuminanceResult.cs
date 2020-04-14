// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Graphics;

namespace Stride.Rendering.Images
{
    /// <summary>
    /// Struct LuminanceResult
    /// </summary>
    public struct LuminanceResult
    {
        public LuminanceResult(float averageLuminance, Texture localTexture)
            : this()
        {
            AverageLuminance = averageLuminance;
            LocalTexture = localTexture;
        }

        public float AverageLuminance { get; set; }

        public Texture LocalTexture { get; set; }
    }
}
