// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_VULKAN

namespace Stride.Graphics;

public static partial class GraphicsBackend
{
    /// <inheritdoc/>
    public static partial bool Implements(GraphicsCapabilityKind kind) => kind switch
    {
        // Texture.Vulkan.cs and PipelineState.Vulkan.cs hardcode VkSampleCountFlags.Count1, and no
        // MultisampleCount conversion exists, so this backend cannot render multisampled.
        GraphicsCapabilityKind.Multisampling => false,

        _ => true
    };
}

#endif
