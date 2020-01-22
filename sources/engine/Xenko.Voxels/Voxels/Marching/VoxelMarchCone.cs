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
        [DataMember(0)]
        public bool EditMode = true;
        [DataMember(10)]
        public bool Fast = false;
        [DataMember(20)]
        public int Steps = 9;
        [DataMember(30)]
        public float StepScale = 1.0f;
        [DataMember(40)]
        public float ConeRatio = 1.0f;
        [DataMember(50)]
        public float StartOffset = 1.0f;

        public VoxelMarchCone()
        {

        }
        public VoxelMarchCone(int steps, float stepScale, float ratio)
        {
            Steps = steps;
            StepScale = stepScale;
            ConeRatio = ratio;
            EditMode = false;
        }
        public ShaderSource GetMarchingShader(int attrID)
        {
            var mixin = new ShaderMixinSource();
            if (EditMode)
            {
                mixin.Mixins.Add(new ShaderClassSource("VoxelMarchConeEditMode"));
            }
            else
            {
                mixin.Mixins.Add(new ShaderClassSource("VoxelMarchCone", Steps, StepScale, ConeRatio, StartOffset));
                mixin.Macros.Add(new ShaderMacro("sampleFunction", Fast ? "SampleNearestMip" : "Sample"));
            }
            mixin.Macros.Add(new ShaderMacro("AttributeID", attrID));

            return mixin;
        }

        ValueParameterKey<int> StepsKey;
        ValueParameterKey<float> StepScaleKey;
        ValueParameterKey<float> ConeRatioKey;
        ValueParameterKey<int> FastKey;
        ValueParameterKey<float> OffsetKey;
        public void UpdateMarchingLayout(string compositionName)
        {
            if (EditMode)
            {
                StepsKey = VoxelMarchConeEditModeKeys.steps.ComposeWith(compositionName);
                StepScaleKey = VoxelMarchConeEditModeKeys.stepScale.ComposeWith(compositionName);
                ConeRatioKey = VoxelMarchConeEditModeKeys.coneRatio.ComposeWith(compositionName);
                FastKey = VoxelMarchConeEditModeKeys.fast.ComposeWith(compositionName);
                OffsetKey = VoxelMarchConeEditModeKeys.offset.ComposeWith(compositionName);
            }
        }
        public void ApplyMarchingParameters(ParameterCollection parameters)
        {
            if (EditMode)
            {
                parameters.Set(StepsKey, Steps);
                parameters.Set(StepScaleKey, StepScale);
                parameters.Set(ConeRatioKey, ConeRatio);
                parameters.Set(FastKey, Fast ? 1 : 0);
                parameters.Set(OffsetKey, StartOffset);
            }
        }
    }
}
