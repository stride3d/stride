// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;

namespace Xenko.Graphics
{
    /// <summary>
    /// Indicates triangles facing a particular direction are not drawn.
    /// </summary>
    /// <remarks>
    /// This enumeration is part of a rasterizer-state object description (see <see cref="RasterizerState"/>). 
    /// </remarks>
    [DataContract]
    public enum CullMode 
    {
        /// <summary>
        /// Always draw all triangles. 
        /// </summary>
        None = 1,

        /// <summary>
        /// Do not draw triangles that are front-facing. 
        /// </summary>
        Front = 2,

        /// <summary>
        /// Do not draw triangles that are back-facing. 
        /// </summary>
        Back = 3,
    }
}
