// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xenko.Core.Storage;
using Xenko.Graphics;
using Xenko.Shaders;

namespace Xenko.Rendering
{
    /// <summary>
    /// Describes an effect as used by a <see cref="RenderNode"/>.
    /// </summary>
    public class RenderEffectReflection
    {
        public static readonly RenderEffectReflection Empty = new RenderEffectReflection();

        public RootSignature RootSignature;

        public FrameResourceGroupLayout PerFrameLayout;
        public ViewResourceGroupLayout PerViewLayout;
        public RenderSystemResourceGroupLayout PerDrawLayout;

        // PerFrame
        public ResourceGroup PerFrameResources;

        public ResourceGroupBufferUploader BufferUploader;

        public EffectDescriptorSetReflection DescriptorReflection;
        public ResourceGroupDescription[] ResourceGroupDescriptions;

        // Used only for fallback effect
        public EffectParameterUpdaterLayout FallbackUpdaterLayout;
        public int[] FallbackResourceGroupMapping;
    }
}
