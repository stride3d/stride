// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using Silk.NET.Direct3D12;

namespace Stride.Graphics
{
    public abstract partial class GraphicsResource
    {
        /// <summary>
        /// Fence value used with <see cref="GraphicsDevice.CopyFence"/> during resource initialization. Need to be waited on for CPU access.
        /// </summary>
        internal ulong? CopyFenceValue;
        /// <summary>
        /// Fence value used with <see cref="GraphicsDevice.CommandListFence"/> when resource is being written by a command list (i.e. <see cref="CommandList.Copy(GraphicsResource, GraphicsResource)"/>). Need to be waited on for CPU access.
        /// </summary>
        internal ulong? CommandListFenceValue;
        /// <summary>
        /// Command list which updated the resource (i.e. <see cref="CommandList.Copy(GraphicsResource, GraphicsResource)"/>) before it has been submitted. Will become <see cref="CommandListFenceValue"/> when command list is submitted.
        /// </summary>
        internal CommandList UpdatingCommandList;

        /// <summary>
        ///   A handle to the CPU-accessible Shader Resource View (SRV) Descriptor.
        /// </summary>
        internal CpuDescriptorHandle NativeShaderResourceView;
        /// <summary>
        ///   A handle to the CPU-accessible Unordered Access View (UAV) Descriptor.
        /// </summary>
        internal CpuDescriptorHandle NativeUnorderedAccessView;

        /// <summary>
        ///   Gets a value indicating whether the Graphics Resource is in "Debug mode".
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the Graphics Resource is initialized in "Debug mode"; otherwise, <see langword="false"/>.
        /// </value>
        protected bool IsDebugMode => GraphicsDevice?.IsDebugMode == true;

        /// <inheritdoc/>
        internal override void SwapInternal(GraphicsResourceBase other)
        {
            var otherResource = (GraphicsResource)other;

            base.SwapInternal(other);

            (CommandListFenceValue, otherResource.CommandListFenceValue)         = (otherResource.CommandListFenceValue, CommandListFenceValue);
            (UpdatingCommandList, otherResource.UpdatingCommandList)             = (otherResource.UpdatingCommandList, UpdatingCommandList);
            (NativeShaderResourceView, otherResource.NativeShaderResourceView)   = (otherResource.NativeShaderResourceView, NativeShaderResourceView);
            (NativeUnorderedAccessView, otherResource.NativeUnorderedAccessView) = (otherResource.NativeUnorderedAccessView, NativeUnorderedAccessView);
            (IsHostVisibleHeap, otherResource.IsHostVisibleHeap)                 = (otherResource.IsHostVisibleHeap, IsHostVisibleHeap);
            (LayoutTracker, otherResource.LayoutTracker)                         = (otherResource.LayoutTracker, LayoutTracker);
        }
    }
}

#endif
