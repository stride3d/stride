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
    ///   Describes the output formats of the Render Targets and the Depth-Stencil Buffer.
    /// </summary>
    [DataContract]
    [StructLayout(LayoutKind.Sequential)] // Sequential so RenderTargetFormats are all contiguous
    public struct RenderOutputDescription : IEquatable<RenderOutputDescription>
    {
        #region Default values

        /// <summary>
        ///   Default value for <see cref="RenderTargetCount"/>.
        /// </summary>
        public const int DefaultRenderTargetCount = 0;
        /// <summary>
        ///   Default value for the Render Targets pixel formats (<see cref="RenderTargetFormat0"/> to <see cref="RenderTargetFormat7"/>).
        /// </summary>
        public const PixelFormat DefaultRenderTargetFormat = PixelFormat.None;
        /// <summary>
        ///   Default value for <see cref="DepthStencilFormat"/>.
        /// </summary>
        public const PixelFormat DefaultDepthStencilFormat = PixelFormat.None;
        /// <summary>
        ///   Default value for <see cref="MultisampleCount"/>.
        /// </summary>
        public const Graphics.MultisampleCount DefaultMultiSampleCount = Graphics.MultisampleCount.None;
        /// <summary>
        ///   Default value for <see cref="ScissorTestEnable"/>.
        /// </summary>
        public const bool DefaultScissorTestEnable = false;

        #endregion

        /// <summary>
        ///   The maximum number of Render Targets configurable by the graphics pipeline.
        /// </summary>
        public const int MaximumRenderTargetCount = 8;

        /// <summary>
        ///   The number of Render Targets.
        /// </summary>
        [DefaultValue(DefaultRenderTargetCount)]
        public int RenderTargetCount = DefaultRenderTargetCount;

        /// <summary>
        ///   Gets the pixel formats of the Render Targets.
        /// </summary>
        /// <remarks>
        ///   There is a maximum of eight Render Targets.
        ///   If a Render Target is set to <see cref="PixelFormat.None"/>, it is considered disabled.
        /// </remarks>
        public readonly Span<PixelFormat> RenderTargetFormats
            // Trickery so the compiler allows us to return a non-readonly Span<> from a readonly property
            => MemoryMarshal.CreateReadOnlySpan(in RenderTargetFormat0, MaximumRenderTargetCount).AsSpan();

        /// <summary>
        ///   The pixel format of the Render Target at index 0.
        /// </summary>
        [DefaultValue(DefaultRenderTargetFormat)]
        public PixelFormat RenderTargetFormat0 = DefaultRenderTargetFormat;
        /// <summary>
        ///   The pixel format of the Render Target at index 1.
        /// </summary>
        [DefaultValue(DefaultRenderTargetFormat)]
        public PixelFormat RenderTargetFormat1 = DefaultRenderTargetFormat;
        /// <summary>
        ///   The pixel format of the Render Target at index 2.
        /// </summary>
        [DefaultValue(DefaultRenderTargetFormat)]
        public PixelFormat RenderTargetFormat2 = DefaultRenderTargetFormat;
        /// <summary>
        ///   The pixel format of the Render Target at index 3.
        /// </summary>
        [DefaultValue(DefaultRenderTargetFormat)]
        public PixelFormat RenderTargetFormat3 = DefaultRenderTargetFormat;
        /// <summary>
        ///   The pixel format of the Render Target at index 4.
        /// </summary>
        [DefaultValue(DefaultRenderTargetFormat)]
        public PixelFormat RenderTargetFormat4 = DefaultRenderTargetFormat;
        /// <summary>
        ///   The pixel format of the Render Target at index 5.
        /// </summary>
        [DefaultValue(DefaultRenderTargetFormat)]
        public PixelFormat RenderTargetFormat5 = DefaultRenderTargetFormat;
        /// <summary>
        ///   The pixel format of the Render Target at index 6.
        /// </summary>
        [DefaultValue(DefaultRenderTargetFormat)]
        public PixelFormat RenderTargetFormat6 = DefaultRenderTargetFormat;
        /// <summary>
        ///   The pixel format of the Render Target at index 7.
        /// </summary>
        [DefaultValue(DefaultRenderTargetFormat)]
        public PixelFormat RenderTargetFormat7 = DefaultRenderTargetFormat;

        /// <summary>
        ///   The depth format of the Depth-Stencil Buffer.
        /// </summary>
        /// <remarks>
        ///   Specify <see cref="PixelFormat.None"/> to disable the Depth-Stencil Buffer.
        /// </remarks>
        [DefaultValue(DefaultDepthStencilFormat)]
        public PixelFormat DepthStencilFormat = DefaultDepthStencilFormat;

        /// <summary>
        ///   The number of samples to use when multi-sampling.
        /// </summary>
        /// <remarks>
        ///   Specify <see cref="MultisampleCount.None"/> to disable multi-sampling.
        /// </remarks>
        [DefaultValue(DefaultMultiSampleCount)]
        public MultisampleCount MultisampleCount = DefaultMultiSampleCount;

        /// <summary>
        ///   A value indicating whether to enable scissor-rectangle culling.
        ///   All pixels ouside an active scissor rectangle are culled.
        /// </summary>
        [DefaultValue(DefaultScissorTestEnable)]
        public bool ScissorTestEnable = DefaultScissorTestEnable;


        /// <summary>
        ///   Initializes a new instance of the <see cref="RenderOutputDescription"/> structure.
        /// </summary>
        /// <param name="renderTargetFormat">
        ///   The pixel format of the Render Target.
        ///   Specify <see cref="PixelFormat.None"/> to disable the Render Targets.
        /// </param>
        /// <param name="depthStencilFormat">
        ///   The depth format of the Depth-Stencil Buffer.
        ///   Specify <see cref="PixelFormat.None"/> to disable the Depth-Stencil Buffer.
        /// </param>
        /// <param name="multisampleCount">
        ///   The number of samples to use when multi-sampling.
        ///   Specify <see cref="MultisampleCount.None"/> to disable multi-sampling.
        /// </param>
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

        /// <summary>
        ///   Initializes a new instance of the <see cref="RenderOutputDescription"/> structure.
        /// </summary>
        /// <param name="renderTargetFormats">
        ///   The pixel formats for up to 8 Render Targets.
        ///   If a Render Target is set to <see cref="PixelFormat.None"/>, it is considered disabled.
        ///   Specify an empty span or all set to <see cref="PixelFormat.None"/> to disable the Render Targets.
        /// </param>
        /// <param name="depthStencilFormat">
        ///   The depth format of the Depth-Stencil Buffer.
        ///   Specify <see cref="PixelFormat.None"/> to disable the Depth-Stencil Buffer.
        /// </param>
        /// <param name="multisampleCount">
        ///   The number of samples to use when multi-sampling.
        ///   Specify <see cref="MultisampleCount.None"/> to disable multi-sampling.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   Cannot specify the format for more than 8 Render Targets in <paramref name="renderTargetFormats"/>.
        /// </exception>
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


        /// <summary>
        ///   Captures the description of the pipeline render output from a Command List.
        /// </summary>
        /// <param name="commandList">The Command List from which to capture the pipeline render output configuration.</param>
        public unsafe void CaptureState(CommandList commandList)
        {
            DepthStencilFormat = commandList.DepthStencilBuffer?.ViewFormat ?? PixelFormat.None;
            MultisampleCount = commandList.DepthStencilBuffer?.MultisampleCount ?? MultisampleCount.None;

            ScissorTestEnable = !commandList.Scissor.IsEmpty;

            RenderTargetCount = commandList.RenderTargetCount;

            for (int i = 0; i < RenderTargetCount; ++i)
            {
                RenderTargetFormats[i] = commandList.RenderTargets[i].ViewFormat;
                MultisampleCount = commandList.RenderTargets[i].MultisampleCount; // Multi-sampling should all be equal
            }
        }


        /// <inheritdoc/>
        public readonly bool Equals(RenderOutputDescription other)
        {
            return RenderTargetCount == other.RenderTargetCount
                && RenderTargetFormats.SequenceEqual(other.RenderTargetFormats)
                && DepthStencilFormat == other.DepthStencilFormat
                && ScissorTestEnable == other.ScissorTestEnable;
        }

        /// <inheritdoc/>
        public override readonly bool Equals(object obj)
        {
            return obj is RenderOutputDescription description && Equals(description);
        }

        /// <inheritdoc/>
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
