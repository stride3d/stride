// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// Defines default values.
    /// </summary>
    internal partial class ImageScalerShaderKeys
    {
        static ImageScalerShaderKeys()
        {
            // Default value of 1.0f
            Color = ParameterKeys.NewValue(Color4.White);
        }
    }
}
