// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using Stride.Core;
using Stride.Core.UnsafeExtensions;

namespace Stride.Graphics
{
    /// <summary>
    /// Describes render targets and depth stencil output formats.
    /// </summary>
    [DataContract]
    [StructLayout(LayoutKind.Sequential)] // Sequential so RenderTargetFormats are all contiguous
    public struct RenderOutputDescription : IEquatable<RenderOutputDescription>
    {
        #region Default values

        /// <summary>
        /// Enable scissor-rectangle culling. All pixels ouside an active scissor rectangle are culled.
        /// </summary>
        public const int DefaultRenderTargetCount = 0;
        public const PixelFormat DefaultRenderTargetFormat = PixelFormat.None;
        public const PixelFormat DefaultDepthStencilFormat = PixelFormat.None;
        public const Graphics.MultisampleCount DefaultMultiSampleCount = Graphics.MultisampleCount.None;
        public const bool DefaultScissorTestEnable = false;

        #endregion

        public const int MaximumRenderTargetCount = 8;

        [DefaultValue(DefaultRenderTargetCount)]
        public int RenderTargetCount = DefaultRenderTargetCount;

        public readonly Span<PixelFormat> RenderTargetFormats
            // Trickery so the compiler allows us to return a non-readonly Span<> from a readonly property
            => MemoryMarshal.CreateReadOnlySpan(in RenderTargetFormat0, MaximumRenderTargetCount).AsSpan();

        [DefaultValue(DefaultRenderTargetFormat)]
        public PixelFormat RenderTargetFormat0 = DefaultRenderTargetFormat;
        [DefaultValue(DefaultRenderTargetFormat)]
        public PixelFormat RenderTargetFormat1 = DefaultRenderTargetFormat;
        [DefaultValue(DefaultRenderTargetFormat)]
        public PixelFormat RenderTargetFormat2 = DefaultRenderTargetFormat;
        [DefaultValue(DefaultRenderTargetFormat)]
        public PixelFormat RenderTargetFormat3 = DefaultRenderTargetFormat;
        [DefaultValue(DefaultRenderTargetFormat)]
        public PixelFormat RenderTargetFormat4 = DefaultRenderTargetFormat;
        [DefaultValue(DefaultRenderTargetFormat)]
        public PixelFormat RenderTargetFormat5 = DefaultRenderTargetFormat;
        [DefaultValue(DefaultRenderTargetFormat)]
        public PixelFormat RenderTargetFormat6 = DefaultRenderTargetFormat;
        [DefaultValue(DefaultRenderTargetFormat)]
        public PixelFormat RenderTargetFormat7 = DefaultRenderTargetFormat;

        [DefaultValue(DefaultDepthStencilFormat)]
        public PixelFormat DepthStencilFormat = DefaultDepthStencilFormat;

        [DefaultValue(DefaultMultiSampleCount)]
        public MultisampleCount MultisampleCount = DefaultMultiSampleCount;

        [DefaultValue(DefaultScissorTestEnable)]
        public bool ScissorTestEnable = DefaultScissorTestEnable;


        public RenderOutputDescription(PixelFormat renderTargetFormat,
                                       PixelFormat depthStencilFormat = PixelFormat.None,
                                       MultisampleCount multisampleCount = MultisampleCount.None)
            : this()
        {
            RenderTargetCount = renderTargetFormat != PixelFormat.None ? 1 : 0;
            RenderTargetFormat0 = renderTargetFormat;
            DepthStencilFormat = depthStencilFormat;
            MultisampleCount = multisampleCount;
        }

        public RenderOutputDescription(ReadOnlySpan<PixelFormat> renderTargetFormats,
                                       PixelFormat depthStencilFormat = PixelFormat.None,
                                       MultisampleCount multisampleCount = MultisampleCount.None)
            : this()
        {
            if (renderTargetFormats.Length > 8)
                throw new ArgumentOutOfRangeException(nameof(renderTargetFormats), "Cannot specify the format for more than 8 Render Targets.");

            int lastSetRenderTarget = -1;
            for (int i = 0; i < MaximumRenderTargetCount; i++)
                if (renderTargetFormats[i] != PixelFormat.None)
                    lastSetRenderTarget = i;

            RenderTargetCount = lastSetRenderTarget + 1; // +1 so 0 if -1, 1 if 0, etc.
            renderTargetFormats.CopyTo(RenderTargetFormats);
            DepthStencilFormat = depthStencilFormat;
            MultisampleCount = multisampleCount;
        }


        public unsafe void CaptureState(CommandList commandList)
        {
            DepthStencilFormat = commandList.DepthStencilBuffer?.ViewFormat ?? PixelFormat.None;
            MultisampleCount = commandList.DepthStencilBuffer?.MultisampleCount ?? MultisampleCount.None;

            ScissorTestEnable = !commandList.Scissor.IsEmpty;

            RenderTargetCount = commandList.RenderTargetCount;

            for (int i = 0; i < MaximumRenderTargetCount; ++i)
            {
                RenderTargetFormats[i] = commandList.RenderTargets[i].ViewFormat;
                MultisampleCount = commandList.RenderTargets[i].MultisampleCount; // Multi-sampling should all be equal
            }
        }


        public readonly bool Equals(RenderOutputDescription other)
        {
            return RenderTargetCount == other.RenderTargetCount
                && RenderTargetFormats.SequenceEqual(other.RenderTargetFormats)
                && DepthStencilFormat == other.DepthStencilFormat
                && ScissorTestEnable == other.ScissorTestEnable;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is RenderOutputDescription description && Equals(description);
        }

        public override readonly int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(RenderTargetCount);
            for (int i = 0; i < MaximumRenderTargetCount; i++)
                hashCode.Add(RenderTargetFormats[i]);
            hashCode.Add(DepthStencilFormat);
            hashCode.Add(ScissorTestEnable);
            return hashCode.ToHashCode();
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
