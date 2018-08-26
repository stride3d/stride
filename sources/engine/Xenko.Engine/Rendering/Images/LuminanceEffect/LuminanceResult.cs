// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Graphics;

namespace Xenko.Rendering.Images
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
