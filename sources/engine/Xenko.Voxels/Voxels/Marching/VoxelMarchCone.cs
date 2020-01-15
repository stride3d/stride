using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Cone")]
    public class VoxelMarchCone : IVoxelMarchMethod
    {
        public bool Fast = false;
        public int Steps = 9;
        public float StepScale = 1.0f;
        public float ConeRatio = 1.0f;

        public VoxelMarchCone()
        {

        }
        public VoxelMarchCone(int steps, float stepScale, float ratio)
        {
            Steps = steps;
            StepScale = stepScale;
            ConeRatio = ratio;
        }
        public ShaderSource GetMarchingShader(int attrID)
        {
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(new ShaderClassSource(Fast? "VoxelMarchConeFast" : "VoxelMarchCone", Steps, StepScale, ConeRatio));
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
