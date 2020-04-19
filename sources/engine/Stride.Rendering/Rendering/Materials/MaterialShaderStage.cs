// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Enumerates the different possible material shader stages.
    /// </summary>
    public enum MaterialShaderStage
    {
        /// <summary>
        /// The vertex shader
        /// </summary>
        Vertex,
        
        /// <summary>
        /// The domain shader
        /// </summary>
        Domain,

        /// <summary>
        /// The pixel shader
        /// </summary>
        Pixel,
    }
}
