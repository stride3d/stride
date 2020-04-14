// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Xenko.Graphics
{
    /// <summary>
    /// The base interface for all the vertex data structure.
    /// </summary>
    public interface IVertex
    {
        /// <summary>
        /// Gets the layout of the vertex.
        /// </summary>
        /// <returns></returns>
        VertexDeclaration GetLayout();

        /// <summary>
        /// Flip the vertex winding.
        /// </summary>
        void FlipWinding();
    }
}
