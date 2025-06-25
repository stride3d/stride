// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using Stride.Core;

namespace Stride.Graphics;

[DataContract]
[StructLayout(LayoutKind.Sequential)]
public struct DepthStencilStateDescription : IEquatable<DepthStencilStateDescription>
{
    /// <summary>
    /// Describes a depth stencil state.
    /// </summary>
    public DepthStencilStateDescription(bool depthEnable, bool depthWriteEnable) : this()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DepthStencilStateDescription"/> class.
        /// </summary>
        /// <summary>
        /// Enables or disables depth buffering. The default is true.
        /// </summary>
        /// <summary>
        /// Gets or sets the comparison function for the depth-buffer test. The default is CompareFunction.LessEqual
        /// </summary>
        /// <summary>
        /// Enables or disables writing to the depth buffer. The default is true.
        /// </summary>
        /// <summary>
        /// Gets or sets stencil enabling. The default is false.
        /// </summary>
        /// <summary>
        /// Gets or sets the mask applied to the reference value and each stencil buffer entry to determine the significant bits for the stencil test. The default mask is byte.MaxValue.
        /// </summary>
        /// <summary>
        /// Gets or sets the write mask applied to values written into the stencil buffer. The default mask is byte.MaxValue.
        /// </summary>
        /// <summary>
        /// Identify how to use the results of the depth test and the stencil test for pixels whose surface normal is facing towards the camera.
        /// </summary>
        /// <summary>
        /// Identify how to use the results of the depth test and the stencil test for pixels whose surface normal is facing away the camera.
        /// </summary>
        /// <summary>
        /// Sets default values for this instance.
        /// </summary>
        /// <summary>
        /// Gets default values for this instance.
        /// </summary>
        SetDefaults();
        DepthBufferEnable = depthEnable;
        DepthBufferWriteEnable = depthWriteEnable;
    }


    public bool DepthBufferEnable;

    public CompareFunction DepthBufferFunction;

    public bool DepthBufferWriteEnable;

    public bool StencilEnable;

    public byte StencilMask;

    public byte StencilWriteMask;

    public DepthStencilStencilOpDescription FrontFace;

    public DepthStencilStencilOpDescription BackFace;


    public void SetDefaults()
    {
        DepthBufferEnable = true;
        DepthBufferWriteEnable = true;
        DepthBufferFunction = CompareFunction.LessEqual;
        StencilEnable = false;

        FrontFace.StencilFunction = CompareFunction.Always;
        FrontFace.StencilPass = StencilOperation.Keep;
        FrontFace.StencilFail = StencilOperation.Keep;
        FrontFace.StencilDepthBufferFail = StencilOperation.Keep;

        BackFace.StencilFunction = CompareFunction.Always;
        BackFace.StencilPass = StencilOperation.Keep;
        BackFace.StencilFail = StencilOperation.Keep;
        BackFace.StencilDepthBufferFail = StencilOperation.Keep;

        StencilMask = byte.MaxValue;
        StencilWriteMask = byte.MaxValue;
    }


    public readonly DepthStencilStateDescription Clone()
    {
        return (DepthStencilStateDescription) MemberwiseClone();
    }

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

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(DepthBufferEnable, DepthBufferFunction, DepthBufferWriteEnable, StencilEnable, StencilMask, StencilWriteMask, FrontFace, BackFace);
    }
}
