using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Opacify")]
    public class VoxelModifierEmissionOpacityOpacify : VoxelModifierBase, IVoxelModifierEmissionOpacity
    {
        public float Amount = 2.0f;
        public void CollectAttributes(List<AttributeStream> attributes, VoxelizationStage stage, bool output) { }

        public ShaderSource GetApplier(string layout)
        {
            return new ShaderClassSource("VoxelModifierApplierOpacify" + layout);
        }

        ValueParameterKey<float> AmountKey;
        public void UpdateVoxelizationLayout(string compositionName)
        {
            AmountKey = VoxelModifierApplierOpacifyIsotropicKeys.Amount.ComposeWith(compositionName);
        }

        public void ApplyVoxelizationParameters(ParameterCollection parameters)
        {
            parameters.Set(AmountKey, Amount);
        }
    }
}
