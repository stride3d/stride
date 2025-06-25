// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using Stride.Core;

namespace Stride.Graphics;

[DataContract]
[StructLayout(LayoutKind.Sequential)]
public struct DepthStencilStencilOpDescription : IEquatable<DepthStencilStencilOpDescription>
{
        /// <summary>
        /// Gets or sets the stencil operation to perform if the stencil test fails. The default is StencilOperation.Keep.
        /// </summary>
    public StencilOperation StencilFail;

        /// <summary>
        /// Gets or sets the stencil operation to perform if the stencil test passes and the depth-test fails. The default is StencilOperation.Keep.
        /// </summary>
        /// <summary>
        /// Gets or sets the stencil operation to perform if the stencil test passes. The default is StencilOperation.Keep.
        /// </summary>
        /// <summary>
        /// Gets or sets the comparison function for the stencil test. The default is CompareFunction.Always.
        /// </summary>
    public StencilOperation StencilDepthBufferFail;

    public StencilOperation StencilPass;

    public CompareFunction StencilFunction;


    public readonly bool Equals(DepthStencilStencilOpDescription other)
    {
        return StencilFail == other.StencilFail
            && StencilDepthBufferFail == other.StencilDepthBufferFail
            && StencilPass == other.StencilPass
            && StencilFunction == other.StencilFunction;
    }

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

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(StencilFail, StencilDepthBufferFail, StencilPass, StencilFunction);
    }
}
