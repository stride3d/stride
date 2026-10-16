// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN

namespace Stride.Graphics
{
    /// <summary>
    ///   The layouts one command buffer gave the subresources (mip, array layer) of a texture: the one it assumed on first
    ///   touch and the one it left. Submission corrects the assumed layouts from what earlier submissions left.
    /// </summary>
    internal sealed class CommandBufferImageLayouts
    {
        public readonly BarrierLayout?[] Assumed;
        public readonly BarrierLayout?[] Current;

        public CommandBufferImageLayouts(int subresourceCount)
        {
            Assumed = new BarrierLayout?[subresourceCount];
            Current = new BarrierLayout?[subresourceCount];
        }
    }
}
#endif
