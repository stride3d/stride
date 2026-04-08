// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   A description of a <strong>Depth-Stencil State</strong>, which defines how depth and stencil testing
///   are performed during rasterization.
/// </summary>
/// <remarks>
///   This structure controls whether depth and stencil tests are enabled, how they are configured, and
///   how they affect pixel visibility.
///   <br/>
///   It allows fine-grained control over depth comparisons, stencil operations, and write masks,
///   enabling advanced rendering techniques such as shadow volumes, outlines, and complex masking.
/// </remarks>
/// <seealso cref="DepthStencilStates"/>
[DataContract]
[StructLayout(LayoutKind.Sequential)]
public struct DepthStencilStateDescription : IEquatable<DepthStencilStateDescription>
{
    #region Default values

    /// <summary>
    ///   The default value for <see cref="DepthBufferEnable"/>.
    /// </summary>
    public const bool DefaultDepthBufferEnable = true;
    /// <summary>
    ///   The default value for <see cref="DepthBufferWriteEnable"/>.
    /// </summary>
    public const bool DefaultDepthBufferWriteEnable = true;
    /// <summary>
    ///   The default value for <see cref="DepthBufferFunction"/>.
    /// </summary>
    public const CompareFunction DefaultDepthBufferFunction = CompareFunction.LessEqual;
    /// <summary>
    ///   The default value for <see cref="StencilEnable"/>.
    /// </summary>
    public const bool DefaultStencilEnable = false;

    /// <summary>
    ///   The default value for both <see cref="FrontFace"/> and <see cref="BackFace"/>.
    /// </summary>
    public static readonly DepthStencilStencilOpDescription DefaultDepthStencilOp = DepthStencilStencilOpDescription.Default;

    /// <summary>
    ///   The default value for <see cref="StencilMask"/>.
    /// </summary>
    public const byte DefaultStencilMask = byte.MaxValue;
    /// <summary>
    ///   The default value for <see cref="StencilWriteMask"/>.
    /// </summary>
    public const byte DefaultStencilWriteMask = byte.MaxValue;

    #endregion

    /// <summary>
    ///   Initializes a new instance of the <see cref="DepthStencilStateDescription"/> structure
    ///   with default values.
    /// </summary>
    /// <remarks><inheritdoc cref="Default" path="/remarks"/></remarks>
    public DepthStencilStateDescription()
    {
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="DepthStencilStateDescription"/> structure
    ///   with default values.
    /// </summary>
    /// <param name="depthEnable">A value indicating whether to enable Depth testing.</param>
    /// <param name="depthWriteEnable">A value indicating whether to enable writing to the Depth-Stencil Buffer.</param>
    /// <remarks><inheritdoc cref="Default" path="/remarks"/></remarks>
    public DepthStencilStateDescription(bool depthEnable, bool depthWriteEnable) : this()
    {
        DepthBufferEnable = depthEnable;
        DepthBufferWriteEnable = depthWriteEnable;
    }


    /// <summary>
    ///   Enables or disables depth testing during rasterization.
    /// </summary>
    /// <remarks>
    ///   When enabled, the depth test compares each pixel's depth value against the existing value in the Depth-Stencil Buffer,
    ///   using the function specified by <see cref="DepthBufferFunction"/>.
    ///   <br/>
    ///   If disabled, all pixels pass the depth test.
    /// </remarks>
    public bool DepthBufferEnable = DefaultDepthBufferEnable;

    /// <summary>
    ///   Specifies the comparison function used in the depth test.
    /// </summary>
    /// <remarks>
    ///   This function determines whether a pixel should be drawn based on its depth value.
    ///   For example, <c>CompareFunction.LessEqual</c> allows a pixel to pass if its depth is less than or equal to
    ///   the current Depth-Stencil Buffer value.
    /// </remarks>
    public CompareFunction DepthBufferFunction = DefaultDepthBufferFunction;

    /// <summary>
    ///   Enables or disables writing to the depth buffer.
    /// </summary>
    /// <remarks>
    ///   Disabling depth writes can be useful for rendering transparent objects or overlays that should not affect
    ///   depth testing of subsequent geometry.
    /// </remarks>
    public bool DepthBufferWriteEnable = DefaultDepthBufferWriteEnable;

    /// <summary>
    ///   Enables or disables stencil testing.
    /// </summary>
    /// <remarks>
    ///   When enabled, the stencil test is performed for each pixel using the configured stencil operations and masks.
    ///   This allows for advanced rendering techniques such as masking, outlining, and shadow volumes.
    /// </remarks>
    public bool StencilEnable = DefaultStencilEnable;

    /// <summary>
    ///   Bitmask applied to both the reference value and stencil buffer entry during stencil testing.
    ///   Default is <see cref="byte.MaxValue"/>.
    /// </summary>
    /// <remarks>
    ///   This mask controls which bits are considered significant in the stencil comparison.
    ///   For example, a mask of <c>0x0F</c> limits the test to the lower 4 bits of the stencil value.
    /// </remarks>
    public byte StencilMask = DefaultStencilMask;

    /// <summary>
    ///   Bitmask applied to values written into the stencil buffer.
    ///   Default is <see cref="byte.MaxValue"/>.
    /// </summary>
    /// <remarks>
    ///   This mask determines which bits can be modified during stencil write operations.
    ///   It allows selective updating of stencil buffer bits.
    /// </remarks>
    public byte StencilWriteMask = DefaultStencilWriteMask;

    /// <summary>
    ///   Describes stencil operations and comparison function for front-facing polygons.
    /// </summary>
    /// <remarks>
    ///   This includes the operations to perform when the stencil test fails, when the depth test fails,
    ///   and when both pass. It also defines the comparison function used for the stencil test.
    /// </remarks>
    public DepthStencilStencilOpDescription FrontFace = DefaultDepthStencilOp;

    /// <summary>
    ///   Describes stencil operations and comparison function for back-facing polygons.
    /// </summary>
    /// <remarks>
    ///   Typically used in conjunction with <see cref="FrontFace"/> to implement two-sided stencil operations,
    ///   such as shadow volume rendering or complex masking.
    /// </remarks>
    public DepthStencilStencilOpDescription BackFace = DefaultDepthStencilOp;


    /// <summary>
    ///   A Depth-Stencil State description with default values.
    /// </summary>
    /// <remarks>
    ///   The default values are:
    ///   <list type="bullet">
    ///     <item>Enables depth testing and depth writing.</item>
    ///     <item>Uses the depth comparison function <see cref="CompareFunction.LessEqual"/>.</item>
    ///     <item>Disables stencil testing.</item>
    ///     <item>Sets the stencil bitmasks to <see cref="byte.MaxValue"/> (all birs set to 1).</item>
    ///     <item>
    ///       The stencil operations and comparison functions for both front-facing and back-facing pixels are set to
    ///       <see cref="CompareFunction.Always"/> (they always succeed) and <see cref="StencilOperation.Keep"/>
    ///       (the value in the stencil buffer is not modified).</item>
    ///   </list>
    /// </remarks>
    public static readonly DepthStencilStateDescription Default = new();


    /// <summary>
    ///   Creates a new instance of <see cref="DepthStencilStateDescription"/> with the same values as this instance.
    /// </summary>
    /// <returns>A new Depth-Stencil State description that is a copy of this instance.</returns>
    public readonly DepthStencilStateDescription Clone()
    {
        return (DepthStencilStateDescription) MemberwiseClone();
    }

    /// <inheritdoc/>
    public readonly bool Equals(DepthStencilStateDescription other)
    {
        return DepthBufferEnable == other.DepthBufferEnable
            && DepthBufferFunction == other.DepthBufferFunction
            && DepthBufferWriteEnable == other.DepthBufferWriteEnable
            && StencilEnable == other.StencilEnable
            && StencilMask == other.StencilMask
            && StencilWriteMask == other.StencilWriteMask
            && FrontFace.Equals(other.FrontFace)
            && BackFace.Equals(other.BackFace);
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object obj)
    {
        return obj is DepthStencilStateDescription dssdesc && Equals(dssdesc);
    }

    public static bool operator ==(DepthStencilStateDescription left, DepthStencilStateDescription right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DepthStencilStateDescription left, DepthStencilStateDescription right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(DepthBufferEnable, DepthBufferFunction, DepthBufferWriteEnable, StencilEnable, StencilMask, StencilWriteMask, FrontFace, BackFace);
    }
}
