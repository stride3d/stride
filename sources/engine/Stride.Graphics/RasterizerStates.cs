// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

public static class RasterizerStates
{
    /// <summary>
    /// Known values for <see cref="RasterizerStateDescription"/>.
    /// </summary>
    static RasterizerStates()
    {
        /// <summary>
        /// Built-in rasterizer state object with settings for culling primitives with clockwise winding order.
        /// </summary>
        /// <summary>
        /// Built-in rasterizer state object with settings for culling primitives with counter-clockwise winding order.
        /// </summary>
        /// <summary>
        /// Built-in rasterizer state object with settings for not culling any primitives.
        /// </summary>
        /// <summary>
        /// Built-in rasterizer state object for wireframe rendering with settings for culling primitives with clockwise winding order.
        /// </summary>
        /// <summary>
        /// Built-in rasterizer state object for wireframe with settings for culling primitives with counter-clockwise winding order.
        /// </summary>
        /// <summary>
        /// Built-in rasterizer state object for wireframe with settings for not culling any primitives.
        /// </summary>
        var defaultState = new RasterizerStateDescription();
        defaultState.SetDefaults();
        Default = defaultState;
    }


    public static readonly RasterizerStateDescription Default;

    public static readonly RasterizerStateDescription CullFront = new(CullMode.Front);

    public static readonly RasterizerStateDescription CullBack = new(CullMode.Back);

    public static readonly RasterizerStateDescription CullNone = new(CullMode.None);

    public static readonly RasterizerStateDescription WireframeCullFront = new(CullMode.Front) { FillMode = FillMode.Wireframe };

    public static readonly RasterizerStateDescription WireframeCullBack = new(CullMode.Back) { FillMode = FillMode.Wireframe };

    public static readonly RasterizerStateDescription Wireframe = new(CullMode.None) { FillMode = FillMode.Wireframe };
}
