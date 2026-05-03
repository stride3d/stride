// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   A description of a <strong>Rasterizer State</strong>, which defines how primitives are rasterized to the Render Targets.
/// </summary>
/// <remarks>
///   This structure controls fill mode, primitive culling, multisampling, depth bias, and clipping.
/// </remarks>
/// <seealso cref="RasterizerStates"/>
[DataContract]
[StructLayout(LayoutKind.Sequential)]
public struct RasterizerStateDescription : IEquatable<RasterizerStateDescription>
{
    #region Default values

    /// <summary>
    ///   The default value for <see cref="FillMode"/>.
    /// </summary>
    public const FillMode DefaultFillMode = FillMode.Solid;
    /// <summary>
    ///   The default value for <see cref="CullMode"/>.
    /// </summary>
    public const CullMode DefaultCullMode = CullMode.Back;
    /// <summary>
    ///   The default value for <see cref="FrontFaceCounterClockwise"/>.
    /// </summary>
    public const bool DefaultFrontFaceCounterClockwise = false;
    /// <summary>
    ///   The default value for <see cref="DepthClipEnable"/>.
    /// </summary>
    public const bool DefaultDepthClipEnable = true;
    /// <summary>
    ///   The default value for <see cref="ScissorTestEnable"/>.
    /// </summary>
    public const bool DefaultScissorTestEnable = false;
    /// <summary>
    ///   The default value for <see cref="MultisampleCount"/>.
    /// </summary>
    public const MultisampleCount DefaultMultisampleCount = MultisampleCount.None;
    /// <summary>
    ///   The default value for <see cref="MultisampleAntiAliasLine"/>.
    /// </summary>
    public const bool DefaultMultisampleAntiAliasLine = false;
    /// <summary>
    ///   The default value for <see cref="DepthBias"/>.
    /// </summary>
    public const int DefaultDepthBias = 0;
    /// <summary>
    ///   The default value for <see cref="DepthBiasClamp"/>.
    /// </summary>
    public const float DefaultDepthBiasClamp = 0;
    /// <summary>
    ///   The default value for <see cref="SlopeScaleDepthBias"/>.
    /// </summary>
    public const float DefaultSlopeScaleDepthBias = 0;

    #endregion

    /// <summary>
    ///   Initializes a new instance of the <see cref="RasterizerStateDescription"/> structure
    ///   with default values.
    /// </summary>
    /// <remarks><inheritdoc cref="Default" path="/remarks"/></remarks>
    public RasterizerStateDescription()
    {
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="RasterizerStateDescription"/> structure
    ///   with default values, and with the specified culling mode.
    /// </summary>
    /// <param name="cullMode">The cull mode.</param>
    /// <remarks><inheritdoc cref="Default" path="/remarks"/></remarks>
    public RasterizerStateDescription(CullMode cullMode) : this()
    {
        CullMode = cullMode;
    }


    /// <summary>
    ///   Specifies how primitives are filled during rasterization (e.g., <strong>solid</strong> or <strong>wireframe</strong>).
    /// </summary>
    /// <remarks>
    ///   Common values include <see cref="FillMode.Solid"/> for standard rendering and <see cref="FillMode.Wireframe"/> for debugging geometry.
    ///   Wireframe mode is especially useful for visualizing mesh topology or detecting overdraw.
    /// </remarks>
    public FillMode FillMode = DefaultFillMode;

    /// <summary>
    ///   Specifies which triangle facing direction <strong>should be culled (not rendered)</strong> during rasterization.
    ///   The facing direction is determined by the <see cref="FrontFaceCounterClockwise"/> setting.
    /// </summary>
    /// <remarks>
    ///   This property determines whether front-facing or back-facing triangles are culled.
    ///   A triangle's facing is defined by the winding order of its vertices and the value of <see cref="FrontFaceCounterClockwise"/>.
    ///   For example, if <c>FrontFaceCounterClockwise</c> is <see langword="false"/> (clockwise is front-facing),
    ///   and <c>CullMode</c> is set to <see cref="CullMode.Back"/>, then counter-clockwise triangles will be culled.
    /// </remarks>
    public CullMode CullMode = DefaultCullMode;

    /// <summary>
    ///   Determines the winding order used to identify front-facing triangles.
    ///   <list type="bullet">
    ///     <item>If <see langword="true"/>, triangles with vertices ordered counter-clockwise on the render target are considered front-facing.</item>
    ///     <item>If <see langword="false"/>, triangles with clockwise winding are considered front-facing.</item>
    ///   </list>
    ///   This setting affects how <see cref="CullMode"/> determines which triangles to cull.
    /// </summary>
    /// <remarks>
    ///   This setting defines the convention for front-facing triangles. Combined with the <see cref="CullMode"/> value,
    ///   it determines whether front-facing or back-facing triangles are culled during rasterization.
    ///   For example, if <c>FrontFaceCounterClockwise</c> is <see langword="false"/> (the default in Direct3D),
    ///   and <c>CullMode</c> is set to <see cref="CullMode.Front"/>, then triangles with clockwise winding will be culled.
    /// </remarks>
    public bool FrontFaceCounterClockwise = DefaultFrontFaceCounterClockwise;

    /// <summary>
    ///   Constant depth bias added to each pixel's depth value.
    /// </summary>
    /// <remarks>
    ///   This value is added to the depth of each pixel and is typically used to resolve Z-fighting,
    ///   such as when rendering decals or wireframe overlays on top of solid geometry.
    ///   The actual depth offset depends on the Depth Buffer format and the slope of the primitive.
    /// </remarks>
    public int DepthBias = DefaultDepthBias;

    /// <summary>
    ///   Maximum depth bias that can be applied to a pixel.
    /// </summary>
    /// <remarks>
    ///   Clamps the total depth bias applied to a pixel, after combining <see cref="DepthBias"/> and <see cref="SlopeScaleDepthBias"/>.
    ///   This is useful to prevent excessive biasing on steep slopes or when using large bias values.
    /// </remarks>
    public float DepthBiasClamp = DefaultDepthBiasClamp;

    /// <summary>
    ///   Scalar applied to a primitive's slope to compute a variable depth bias.
    ///   Helps offset depth values based on surface angle.
    /// </summary>
    /// <remarks>
    ///   This value is multiplied by the maximum slope of the primitive to compute a variable depth bias.
    ///   It helps reduce Z-fighting on surfaces that are nearly parallel to the view direction.
    ///   Often used in conjunction with <see cref="DepthBias"/> for shadow mapping or coplanar geometry.
    /// </remarks>
    public float SlopeScaleDepthBias = DefaultSlopeScaleDepthBias;

    /// <summary>
    ///   Enables or disables clipping of geometry based on the depth (Z) value.
    ///   When enabled, primitives outside the near and far clip planes are discarded.
    /// </summary>
    /// <remarks>
    ///   When enabled, geometry outside the near and far clip planes is discarded.
    ///   Disabling this can be useful for special effects like infinite projection or stencil shadows,
    ///   but may lead to incorrect depth ordering if not handled carefully.
    /// </remarks>
    public bool DepthClipEnable = DefaultDepthClipEnable;

    // TODO: D3D12: In Direct3D 12, Scissor rectangles are set through the Command List dynamically, not through immutable Render States

    /// <summary>
    ///   Enables scissor testing. Pixels outside the active scissor rectangle are culled.
    /// </summary>
    /// <remarks>
    ///   When enabled, only pixels inside the active scissor rectangle are rendered.
    ///   This is commonly used for UI rendering, partial redraws, or performance optimization.
    /// </remarks>
    public bool ScissorTestEnable = DefaultScissorTestEnable;

    /// <summary>
    ///   Specifies the number of samples used for multisample anti-aliasing (MSAA).
    /// </summary>
    /// <remarks>
    ///   Higher sample counts improve edge smoothness but increase memory and processing cost.
    /// </remarks>
    public MultisampleCount MultisampleCount = DefaultMultisampleCount;

    /// <summary>
    ///   Enables antialiasing for lines when MSAA is disabled. Only affects line rendering.
    /// </summary>
    /// <remarks>
    ///   This only affects line primitives, and has no effect when <see cref="MultisampleCount"/> is greater than 1.
    /// </remarks>
    public bool MultisampleAntiAliasLine = DefaultMultisampleAntiAliasLine;


    /// <summary>
    ///   A Rasterizer State description with default values.
    /// </summary>
    /// <remarks>
    ///   The default values are:
    ///   <list type="bullet">
    ///     <item><see cref="FillMode"/>: Rasterize filled triangles (<see cref="FillMode.Solid"/>).</item>
    ///     <item><see cref="CullMode"/>: Cull back-facing primitives (<see cref="CullMode.Back"/>).</item>
    ///     <item><see cref="FrontFaceCounterClockwise"/>: Consider front-facing the primitives whose vertices are ordered clockwise (<see langword="false"/>).</item>
    ///     <item><see cref="DepthClipEnable"/>: Clip primitives outside the near and far clipping planes (<see langword="true"/>).</item>
    ///     <item>Scissor testing disabled.</item>
    ///     <item>No multisampling (<see cref="MultisampleCount.None"/>) and no antialiased lines.</item>
    ///     <item>No <see cref="DepthBias"/>, <see cref="DepthBiasClamp"/>, or <see cref="SlopeScaleDepthBias"/>.</item>
    ///   </list>
    /// </remarks>
    public static readonly RasterizerStateDescription Default = new();


    /// <inheritdoc/>
    public readonly bool Equals(RasterizerStateDescription other)
    {
        return FillMode == other.FillMode
            && CullMode == other.CullMode
            && FrontFaceCounterClockwise == other.FrontFaceCounterClockwise
            && DepthBias == other.DepthBias
            && DepthBiasClamp.Equals(other.DepthBiasClamp)
            && SlopeScaleDepthBias.Equals(other.SlopeScaleDepthBias)
            && DepthClipEnable == other.DepthClipEnable
            && ScissorTestEnable == other.ScissorTestEnable
            && MultisampleCount == other.MultisampleCount
            && MultisampleAntiAliasLine == other.MultisampleAntiAliasLine;
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object obj)
    {
        return obj is RasterizerStateDescription rsdesc && Equals(rsdesc);
    }

    public static bool operator ==(RasterizerStateDescription left, RasterizerStateDescription right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(RasterizerStateDescription left, RasterizerStateDescription right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(FillMode);
        hash.Add(CullMode);
        hash.Add(FrontFaceCounterClockwise);
        hash.Add(DepthBias);
        hash.Add(DepthBiasClamp);
        hash.Add(SlopeScaleDepthBias);
        hash.Add(DepthClipEnable);
        hash.Add(ScissorTestEnable);
        hash.Add(MultisampleCount);
        hash.Add(MultisampleAntiAliasLine);
        return hash.ToHashCode();
    }
}
