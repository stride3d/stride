// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Xenko.Core;

namespace Xenko.Graphics
{
    /// <summary>
    /// Describes render targets and depth stencil output formats.
    /// </summary>
    [DataContract]
    public struct RenderOutputDescription : IEquatable<RenderOutputDescription>
    {
        // Render targets
        [DefaultValue(0)]
        public int RenderTargetCount;
        [DefaultValue(PixelFormat.None)]
        public PixelFormat RenderTargetFormat0;
        [DefaultValue(PixelFormat.None)]
        public PixelFormat RenderTargetFormat1;
        [DefaultValue(PixelFormat.None)]
        public PixelFormat RenderTargetFormat2;
        [DefaultValue(PixelFormat.None)]
        public PixelFormat RenderTargetFormat3;
        [DefaultValue(PixelFormat.None)]
        public PixelFormat RenderTargetFormat4;
        [DefaultValue(PixelFormat.None)]
        public PixelFormat RenderTargetFormat5;
        [DefaultValue(PixelFormat.None)]
        public PixelFormat RenderTargetFormat6;
        [DefaultValue(PixelFormat.None)]
        public PixelFormat RenderTargetFormat7;

        [DefaultValue(PixelFormat.None)]
        public PixelFormat DepthStencilFormat;

        [DefaultValue(MultisampleCount.None)]
        public MultisampleCount MultisampleCount;

        /// <summary>
        /// Enable scissor-rectangle culling. All pixels ouside an active scissor rectangle are culled.
        /// </summary>
        [DefaultValue(false)]
        public bool ScissorTestEnable;

        public RenderOutputDescription(PixelFormat renderTargetFormat, PixelFormat depthStencilFormat = PixelFormat.None, MultisampleCount multisampleCount = MultisampleCount.None) : this()
        {
            RenderTargetCount = renderTargetFormat != PixelFormat.None ? 1 : 0;
            RenderTargetFormat0 = renderTargetFormat;
            DepthStencilFormat = depthStencilFormat;
            MultisampleCount = multisampleCount;
        }

        public unsafe void CaptureState(CommandList commandList)
        {
            DepthStencilFormat = commandList.DepthStencilBuffer != null ? commandList.DepthStencilBuffer.ViewFormat : PixelFormat.None;
            MultisampleCount = commandList.DepthStencilBuffer != null ? commandList.DepthStencilBuffer.MultisampleCount : MultisampleCount.None;

            ScissorTestEnable = !commandList.Scissor.IsEmpty;

            RenderTargetCount = commandList.RenderTargetCount;
            fixed (PixelFormat* renderTargetFormat0 = &RenderTargetFormat0)
            {
                var renderTargetFormat = renderTargetFormat0;
                for (int i = 0; i < RenderTargetCount; ++i)
                {
                    *renderTargetFormat++ = commandList.RenderTargets[i].ViewFormat;
                    MultisampleCount = commandList.RenderTargets[i].MultisampleCount; // multisample should all be equal
                }
            }
        }

        public bool Equals(RenderOutputDescription other)
        {
            return RenderTargetCount == other.RenderTargetCount
                   && RenderTargetFormat0 == other.RenderTargetFormat0
                   && RenderTargetFormat1 == other.RenderTargetFormat1
                   && RenderTargetFormat2 == other.RenderTargetFormat2
                   && RenderTargetFormat3 == other.RenderTargetFormat3
                   && RenderTargetFormat4 == other.RenderTargetFormat4
                   && RenderTargetFormat5 == other.RenderTargetFormat5
                   && RenderTargetFormat6 == other.RenderTargetFormat6
                   && RenderTargetFormat7 == other.RenderTargetFormat7
                   && DepthStencilFormat == other.DepthStencilFormat
                   && ScissorTestEnable == other.ScissorTestEnable;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is RenderOutputDescription && Equals((RenderOutputDescription)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = RenderTargetCount;
                hashCode = (hashCode * 397) ^ (int)RenderTargetFormat0;
                hashCode = (hashCode * 397) ^ (int)RenderTargetFormat1;
                hashCode = (hashCode * 397) ^ (int)RenderTargetFormat2;
                hashCode = (hashCode * 397) ^ (int)RenderTargetFormat3;
                hashCode = (hashCode * 397) ^ (int)RenderTargetFormat4;
                hashCode = (hashCode * 397) ^ (int)RenderTargetFormat5;
                hashCode = (hashCode * 397) ^ (int)RenderTargetFormat6;
                hashCode = (hashCode * 397) ^ (int)RenderTargetFormat7;
                hashCode = (hashCode * 397) ^ (int)DepthStencilFormat;
                hashCode = (hashCode * 397) ^ (ScissorTestEnable ? 1 : 0);
                return hashCode;
            }
        }

        public static bool operator ==(RenderOutputDescription left, RenderOutputDescription right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RenderOutputDescription left, RenderOutputDescription right)
        {
            return !left.Equals(right);
        }
    }
}
