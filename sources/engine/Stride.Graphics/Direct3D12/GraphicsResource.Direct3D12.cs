// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using Silk.NET.Direct3D12;

namespace Stride.Graphics
{
    public abstract partial class GraphicsResource
    {
        /// <summary>
        ///   A reference to the parent <see cref="GraphicsResource"/> that owns this resource, if any.
        /// </summary>
        internal GraphicsResource ParentResource;

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
        ///   The current Direct3D 12 Resource State of the Graphics Resource.
        /// </summary>
        internal ResourceStates NativeResourceState;

        /// <summary>
        ///   Gets a value indicating whether the Graphics Resource is in "Debug mode".
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the Graphics Resource is initialized in "Debug mode"; otherwise, <see langword="false"/>.
        /// </value>
        protected bool IsDebugMode => GraphicsDevice?.IsDebugMode == true;


        /// <summary>
        ///   Determines if the Graphics Resource needs to perform a state transition in order to reach the target state.
        /// </summary>
        /// <param name="targeState">The destination Graphics Resource state.</param>
        /// <returns>
        ///   <see langword="true"/> if a transition is needed to reach the target state;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        internal bool IsTransitionNeeded(ResourceStates targeState)
        {
            // If 'targeState' is a subset of 'before', then there's no need for a transition

            // NOTE: ResourceStates.Common is an oddball state that doesn't follow the ResourceStates
            //       pattern of having exactly one bit set so we need to special case these
            return NativeResourceState != targeState &&
                ((NativeResourceState | targeState) != NativeResourceState || targeState == ResourceStates.Common);
        }

        /// <inheritdoc/>
        internal override void SwapInternal(GraphicsResourceBase other)
        {
            var otherResource = (GraphicsResource)other;

            base.SwapInternal(other);

            (ParentResource, otherResource.ParentResource)                       = (otherResource.ParentResource, ParentResource);
            (CommandListFenceValue, otherResource.CommandListFenceValue)         = (otherResource.CommandListFenceValue, CommandListFenceValue);
            (UpdatingCommandList, otherResource.UpdatingCommandList)             = (otherResource.UpdatingCommandList, UpdatingCommandList);
            (NativeShaderResourceView, otherResource.NativeShaderResourceView)   = (otherResource.NativeShaderResourceView, NativeShaderResourceView);
            (NativeUnorderedAccessView, otherResource.NativeUnorderedAccessView) = (otherResource.NativeUnorderedAccessView, NativeUnorderedAccessView);
            (NativeResourceState, otherResource.NativeResourceState)             = (otherResource.NativeResourceState, NativeResourceState);
        }
    }
}

#endif
