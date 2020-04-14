// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    /// Known values for <see cref="RasterizerStateDescription"/>.
    /// </summary>
    public static class RasterizerStates
    {
        /// <summary>
        /// Built-in rasterizer state object with settings for culling primitives with clockwise winding order.
        /// </summary>
        public static readonly RasterizerStateDescription CullFront = new RasterizerStateDescription(CullMode.Front);

        /// <summary>
        /// Built-in rasterizer state object with settings for culling primitives with counter-clockwise winding order.
        /// </summary>
        public static readonly RasterizerStateDescription CullBack = new RasterizerStateDescription(CullMode.Back);

        /// <summary>
        /// Built-in rasterizer state object with settings for not culling any primitives.
        /// </summary>
        public static readonly RasterizerStateDescription CullNone = new RasterizerStateDescription(CullMode.None);

        /// <summary>
        /// Built-in rasterizer state object for wireframe rendering with settings for culling primitives with clockwise winding order.
        /// </summary>
        public static readonly RasterizerStateDescription WireframeCullFront = new RasterizerStateDescription(CullMode.Front) { FillMode = FillMode.Wireframe };

        /// <summary>
        /// Built-in rasterizer state object for wireframe with settings for culling primitives with counter-clockwise winding order.
        /// </summary>
        public static readonly RasterizerStateDescription WireframeCullBack = new RasterizerStateDescription(CullMode.Back) { FillMode = FillMode.Wireframe };

        /// <summary>
        /// Built-in rasterizer state object for wireframe with settings for not culling any primitives.
        /// </summary>
        public static readonly RasterizerStateDescription Wireframe = new RasterizerStateDescription(CullMode.None) { FillMode = FillMode.Wireframe };
    }
}
