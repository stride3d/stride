// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D12
using System;
using SharpDX.Direct3D12;

namespace Stride.Graphics
{
    /// <summary>
    /// GraphicsResource class
    /// </summary>
    public abstract partial class GraphicsResource
    {
        internal GraphicsResource ParentResource;

        internal long? StagingFenceValue;
        internal CommandList StagingBuilder;
        internal CpuDescriptorHandle NativeShaderResourceView;
        internal CpuDescriptorHandle NativeUnorderedAccessView;
        internal ResourceStates NativeResourceState;

        protected bool IsDebugMode => GraphicsDevice != null && GraphicsDevice.IsDebugMode;

        /// <summary>
        /// Returns true if resource state transition is needed in order to use resource in given state
        /// </summary>
        /// <param name="targeState">Destination resource state</param>
        /// <returns>True if need to perform a transition, otherwsie false</returns>
        internal bool IsTransitionNeeded(ResourceStates targeState)
        {
            // If 'targeState' is a subset of 'before', then there's no need for a transition
            // Note: COMMON is an oddball state that doesn't follow the RESOURE_STATE pattern of 
            // having exactly one bit set so we need to special case these
            return NativeResourceState != targeState && ((NativeResourceState | targeState) != NativeResourceState || targeState == ResourceStates.Common);
        }
    }
}
 
#endif
