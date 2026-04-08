// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   Describes the stencil operations and comparison function used during stencil testing
///   for a given face orientation (front or back).
/// </summary>
/// <remarks>
///   This structure defines how the stencil buffer is updated based on the outcome of the stencil and depth tests.
///   It is used in <see cref="DepthStencilStateDescription"/> to configure separate behavior for front-facing and back-facing polygons.
/// </remarks>
[DataContract]
[StructLayout(LayoutKind.Sequential)]
public struct DepthStencilStencilOpDescription : IEquatable<DepthStencilStencilOpDescription>
{
    #region Default values

    /// <summary>
    ///   Default value for <see cref="StencilFunction"/>.
    /// </summary>
    public const CompareFunction DefaultStencilFunction = CompareFunction.Always;
    /// <summary>
    ///   Default value for <see cref="StencilPass"/>.
    /// </summary>
    public const StencilOperation DefaultStencilPass = StencilOperation.Keep;
    /// <summary>
    ///   Default value for <see cref="StencilFail"/>.
    /// </summary>
    public const StencilOperation DefaultStencilFail = StencilOperation.Keep;
    /// <summary>
    ///   Default value for <see cref="StencilDepthBufferFail"/>.
    /// </summary>
    public const StencilOperation DefaultStencilDepthBufferFail = StencilOperation.Keep;

    #endregion

    /// <summary>
    ///   Specifies the stencil operation to perform <strong>when the stencil test fails</strong>.
    /// </summary>
    /// <remarks>
    ///   This operation is applied <strong>regardless of the result of the depth test</strong>.
    ///   Common values include <see cref="StencilOperation.Keep"/> (no change) and <see cref="StencilOperation.Increment"/>/<see cref="StencilOperation.Decrement"/>
    ///   for masking or outlining effects.
    /// </remarks>
    public StencilOperation StencilFail = DefaultStencilFail;

    /// <summary>
    ///   Specifies the stencil operation to perform <strong>when the stencil test passes but the depth test fails</strong>.
    /// </summary>
    /// <remarks>
    ///   This is useful for effects like shadow volumes, where depth failure indicates occlusion.
    /// </remarks>
    public StencilOperation StencilDepthBufferFail = DefaultStencilDepthBufferFail;

    /// <summary>
    ///   Specifies the stencil operation to perform <strong>when both the stencil and depth tests pass</strong>.
    /// </summary>
    /// <remarks>
    ///   This is the most common path for visible pixels.
    ///   The operation typically updates the stencil buffer to mark the pixel as processed.
    /// </remarks>
    public StencilOperation StencilPass = DefaultStencilPass;

    /// <summary>
    ///   Specifies the comparison function used to evaluate the stencil test.
    /// </summary>
    /// <remarks>
    ///   The test compares the stencil buffer value with a reference value using this function.
    ///   For example, <see cref="CompareFunction.Equal"/> passes only if the values match.
    /// </remarks>
    public CompareFunction StencilFunction = DefaultStencilFunction;


    /// <summary>
    ///   Initializes a new instance of the <see cref="DepthStencilStencilOpDescription"/> structure
    ///   with default values.
    /// </summary>
    /// <remarks><inheritdoc cref="Default" path="/remarks"/></remarks>
    public DepthStencilStencilOpDescription()
    {
    }

    /// <summary>
    ///   A Depth-Stencil Operation description with default values.
    /// </summary>
    /// <remarks>
    ///   The default values are:
    ///   <list type="bullet">
    ///     <item>
    ///       Always keep the current value (<see cref="StencilOperation.Keep"/>) in all cases
    ///       (i.e., <see cref="StencilFail"/>, <see cref="StencilDepthBufferFail"/>, and <see cref="StencilPass"/>).
    ///     </item>
    ///     <item>A comparison function that always passes (<see cref="CompareFunction.Always"/>).</item>
    ///   </list>
    /// </remarks>
    public static readonly DepthStencilStencilOpDescription Default = new();


    /// <inheritdoc/>
    public readonly bool Equals(DepthStencilStencilOpDescription other)
    {
        return StencilFail == other.StencilFail
            && StencilDepthBufferFail == other.StencilDepthBufferFail
            && StencilPass == other.StencilPass
            && StencilFunction == other.StencilFunction;
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object obj)
    {
        return obj is DepthStencilStencilOpDescription dssOp && Equals(dssOp);
    }

    public static bool operator ==(DepthStencilStencilOpDescription left, DepthStencilStencilOpDescription right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DepthStencilStencilOpDescription left, DepthStencilStencilOpDescription right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(StencilFail, StencilDepthBufferFail, StencilPass, StencilFunction);
    }
}
