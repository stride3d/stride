// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics;

[DataContract]
public enum FillMode : int
{
    /// <summary>
    /// <p>Determines the fill mode to use when rendering triangles.</p>
    /// </summary>
    /// <remarks>
    /// <p>This enumeration is part of a rasterizer-state object description (see <strong><see cref="RasterizerStateDescription"/></strong>).</p>
    /// </remarks>
        /// <summary>
        /// <dd> <p>Draw lines connecting the vertices. Adjacent vertices are not drawn.</p> </dd>
        /// </summary>
    Wireframe = 2,

        /// <summary>
        /// <dd> <p>Fill the triangles formed by the vertices. Adjacent vertices are not drawn.</p> </dd>
        /// </summary>
    Solid = 3
}
