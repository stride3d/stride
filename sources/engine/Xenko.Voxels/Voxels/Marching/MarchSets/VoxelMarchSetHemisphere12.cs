using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Hemisphere (12)")]
    public class VoxelMarchSetHemisphere12 : IVoxelMarchSet
    {
        public IVoxelMarchMethod Marcher { set; get; } = new VoxelMarchCone(9, 1.0f, 1.0f);
        public VoxelMarchSetHemisphere12()
        {

        }
        public VoxelMarchSetHemisphere12(IVoxelMarchMethod marcher)
        {
            Marcher = marcher;
        }
        public ShaderSource GetMarchingShader(int attrID)
        {
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(new ShaderClassSource("VoxelMarchSetHemisphere12"));
            mixin.AddComposition("Marcher", Marcher.GetMarchingShader(attrID));
            return mixin;
        }

        public void UpdateMarchingLayout(string compositionName)
        {
            Marcher.UpdateMarchingLayout("Marcher."+compositionName);
        }
        public void ApplyMarchingParameters(ParameterCollection parameters)
        {
            Marcher.ApplyMarchingParameters(parameters);
        }
    }
}
