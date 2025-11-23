// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Defines a set of built-in <see cref="RasterizerStateDescription"/>s for common rasterizer configurations.
/// </summary>
public static class RasterizerStates
{
    /// <summary>
    ///   A built-in Rasterizer State object with default settings.
    /// </summary>
    /// <inheritdoc cref="RasterizerStateDescription.Default" path="/remarks"/>
    public static readonly RasterizerStateDescription Default = RasterizerStateDescription.Default;

    /// <summary>
    ///   A built-in Rasterizer State object with settings for culling primitives with clockwise winding order.
    /// </summary>
    public static readonly RasterizerStateDescription CullFront = new(CullMode.Front);

    /// <summary>
    ///   A built-in Rasterizer State object with settings for culling primitives with counter-clockwise winding order.
    /// </summary>
    public static readonly RasterizerStateDescription CullBack = new(CullMode.Back);

    /// <summary>
    ///   A built-in Rasterizer State object with settings for not culling any primitives.
    /// </summary>
    public static readonly RasterizerStateDescription CullNone = new(CullMode.None);

    /// <summary>
    ///   A built-in Rasterizer State object for wireframe rendering with settings for culling primitives with clockwise winding order.
    /// </summary>
    public static readonly RasterizerStateDescription WireframeCullFront = new(CullMode.Front) { FillMode = FillMode.Wireframe };

    /// <summary>
    ///   A built-in Rasterizer State object for wireframe with settings for culling primitives with counter-clockwise winding order.
    /// </summary>
    public static readonly RasterizerStateDescription WireframeCullBack = new(CullMode.Back) { FillMode = FillMode.Wireframe };

    /// <summary>
    ///   A built-in Rasterizer State object for wireframe with settings for not culling any primitives.
    /// </summary>
    public static readonly RasterizerStateDescription Wireframe = new(CullMode.None) { FillMode = FillMode.Wireframe };
}
