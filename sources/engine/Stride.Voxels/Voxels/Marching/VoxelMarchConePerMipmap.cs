// Copyright (c) Stride contributors (https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core;
using Stride.Shaders;

namespace Stride.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Cone (Mipmap Exact)")]
    public class VoxelMarchConePerMipmap : IVoxelMarchMethod
    {
        [DataMember(0)]
        public int Steps { get; set; } = 7;

        [DataMember(10)]
        public float ConeRatio { get; set; } = 1f;

        [DataMember(20)]
        public float StartOffset { get; set; } = 0.5f;

        public VoxelMarchConePerMipmap()
        {

        }
        public VoxelMarchConePerMipmap(float ratio, int steps)
        {
            ConeRatio = ratio;
            Steps = steps;
        }
        public ShaderSource GetMarchingShader(int attrID)
        {
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(new ShaderClassSource("VoxelMarchConePerMipmap", Steps));
            mixin.Macros.Add(new ShaderMacro("AttributeID", attrID));
            return mixin;
        }
        ValueParameterKey<float> OffsetKey;
        ValueParameterKey<float> ConeRatioInvKey;
        public void UpdateMarchingLayout(string compositionName)
        {
            OffsetKey = VoxelMarchConePerMipmapKeys.offset.ComposeWith(compositionName);
            ConeRatioInvKey = VoxelMarchConePerMipmapKeys.coneRatioInv.ComposeWith(compositionName);
        }
        public void ApplyMarchingParameters(ParameterCollection parameters)
        {
            parameters.Set(OffsetKey, StartOffset);
            parameters.Set(ConeRatioInvKey, 1.0f/ConeRatio);
        }
    }
}
