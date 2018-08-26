// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using Xenko.Core;

namespace Xenko.Graphics
{
    /// <summary>
    /// Describes a depth stencil state.
    /// </summary>
    [DataContract]
    [StructLayout(LayoutKind.Sequential)]
    public struct DepthStencilStateDescription : IEquatable<DepthStencilStateDescription>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DepthStencilStateDescription"/> class.
        /// </summary>
        public DepthStencilStateDescription(bool depthEnable, bool depthWriteEnable) : this()
        {
            SetDefault();
            DepthBufferEnable = depthEnable;
            DepthBufferWriteEnable = depthWriteEnable;
        }

        /// <summary>
        /// Enables or disables depth buffering. The default is true.
        /// </summary>
        public bool DepthBufferEnable;

        /// <summary>
        /// Gets or sets the comparison function for the depth-buffer test. The default is CompareFunction.LessEqual
        /// </summary>
        public CompareFunction DepthBufferFunction;

        /// <summary>
        /// Enables or disables writing to the depth buffer. The default is true.
        /// </summary>
        public bool DepthBufferWriteEnable;

        /// <summary>
        /// Gets or sets stencil enabling. The default is false.
        /// </summary>
        public bool StencilEnable;

        /// <summary>
        /// Gets or sets the mask applied to the reference value and each stencil buffer entry to determine the significant bits for the stencil test. The default mask is byte.MaxValue.
        /// </summary>
        public byte StencilMask;

        /// <summary>
        /// Gets or sets the write mask applied to values written into the stencil buffer. The default mask is byte.MaxValue.
        /// </summary>
        public byte StencilWriteMask;

        /// <summary>
        /// Identify how to use the results of the depth test and the stencil test for pixels whose surface normal is facing towards the camera.
        /// </summary>
        public DepthStencilStencilOpDescription FrontFace;

        /// <summary>
        /// Identify how to use the results of the depth test and the stencil test for pixels whose surface normal is facing away the camera.
        /// </summary>
        public DepthStencilStencilOpDescription BackFace;

        /// <summary>
        /// Sets default values for this instance.
        /// </summary>
        public DepthStencilStateDescription SetDefault()
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
            return this;
        }

        /// <summary>
        /// Gets default values for this instance.
        /// </summary>
        public static DepthStencilStateDescription Default
        {
            get
            {
                var desc = new DepthStencilStateDescription();
                desc.SetDefault();
                return desc;
            }
        }

        public DepthStencilStateDescription Clone()
        {
            return (DepthStencilStateDescription)MemberwiseClone();
        }

        public bool Equals(DepthStencilStateDescription other)
        {
            return DepthBufferEnable == other.DepthBufferEnable && DepthBufferFunction == other.DepthBufferFunction && DepthBufferWriteEnable == other.DepthBufferWriteEnable && StencilEnable == other.StencilEnable && StencilMask == other.StencilMask && StencilWriteMask == other.StencilWriteMask && FrontFace.Equals(other.FrontFace) && BackFace.Equals(other.BackFace);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is DepthStencilStateDescription && Equals((DepthStencilStateDescription)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = DepthBufferEnable.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)DepthBufferFunction;
                hashCode = (hashCode * 397) ^ DepthBufferWriteEnable.GetHashCode();
                hashCode = (hashCode * 397) ^ StencilEnable.GetHashCode();
                hashCode = (hashCode * 397) ^ StencilMask.GetHashCode();
                hashCode = (hashCode * 397) ^ StencilWriteMask.GetHashCode();
                hashCode = (hashCode * 397) ^ FrontFace.GetHashCode();
                hashCode = (hashCode * 397) ^ BackFace.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(DepthStencilStateDescription left, DepthStencilStateDescription right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DepthStencilStateDescription left, DepthStencilStateDescription right)
        {
            return !left.Equals(right);
        }
    }
}
