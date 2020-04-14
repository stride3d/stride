// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering
{
    /// <summary>
    /// Implementation of <see cref="ResourceGroupLayout"/> specifically for PerFrame cbuffer of <see cref="RenderSystem"/>.
    /// </summary>
    public class FrameResourceGroupLayout : RenderSystemResourceGroupLayout
    {
        public ResourceGroupEntry Entry;
    }
}
