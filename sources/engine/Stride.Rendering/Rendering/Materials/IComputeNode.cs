// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Base interface for all <see cref="IComputeNode"/>
    /// </summary>
    public interface IComputeNode
    {
        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <param name="context">The context to get the children.</param>
        /// <returns>The list of children.</returns>
        IEnumerable<IComputeNode> GetChildren(object context = null);

        /// <summary>
        /// Generates the shader source equivalent for this node
        /// </summary>
        /// <returns>ShaderSource.</returns>
        ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys);
    }
}
