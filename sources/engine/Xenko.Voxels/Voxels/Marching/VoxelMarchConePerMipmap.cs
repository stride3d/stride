using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Cone (Mipmap Exact)")]
    public class VoxelMarchConePerMipmap : IVoxelMarchMethod
    {
        public int Steps = 7;

        public VoxelMarchConePerMipmap()
        {

        }
        public VoxelMarchConePerMipmap(int steps)
        {
            Steps = steps;
        }
        public ShaderSource GetMarchingShader(int attrID)
        {
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(new ShaderClassSource("VoxelMarchConePerMipmap", Steps));
            mixin.Macros.Add(new ShaderMacro("AttributeID", attrID));
            return mixin;
        }
        public void UpdateMarchingLayout(string compositionName)
        {
        }
        public void ApplyMarchingParameters(ParameterCollection parameters)
        {
        }
    }
}
