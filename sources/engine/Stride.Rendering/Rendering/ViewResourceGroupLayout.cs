// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering
{
    /// <summary>
    /// Implementation of <see cref="ResourceGroupLayout"/> specifically for PerView cbuffer of <see cref="RenderSystem"/>.
    /// </summary>
    public class ViewResourceGroupLayout : RenderSystemResourceGroupLayout
    {
        public ResourceGroupEntry[] Entries;
    }
}
