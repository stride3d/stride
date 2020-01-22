using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Hemisphere (6)")]
    public class VoxelMarchSetHemisphere6 : VoxelMarchSetBase, IVoxelMarchSet
    {
        public VoxelMarchSetHemisphere6()
        {

        }
        public VoxelMarchSetHemisphere6(IVoxelMarchMethod marcher)
        {
            Marcher = marcher;
        }

        public ShaderSource GetMarchingShader(int attrID)
        {
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(new ShaderClassSource("VoxelMarchSetHemisphere6"));
            mixin.AddComposition("Marcher", Marcher.GetMarchingShader(attrID));
            return mixin;
        }

        public override void UpdateMarchingLayout(string compositionName)
        {
            base.UpdateMarchingLayout(compositionName);
            OffsetKey = VoxelMarchSetHemisphere6Keys.offset.ComposeWith(compositionName);
        }
    }
}
