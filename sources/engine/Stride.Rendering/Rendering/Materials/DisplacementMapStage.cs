// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Enumerates the different shader stages in which a displacement map can be applied.
    /// </summary>
    public enum DisplacementMapStage
    {
        /// <summary>
        /// The vertex shader
        /// </summary>
        Vertex = MaterialShaderStage.Vertex,

        /// <summary>
        /// The domain shader
        /// </summary>
        Domain = MaterialShaderStage.Domain,
    }
}
