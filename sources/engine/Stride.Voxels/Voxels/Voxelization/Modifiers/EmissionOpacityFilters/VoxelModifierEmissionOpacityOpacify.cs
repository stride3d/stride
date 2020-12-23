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
    [Display("Opacify")]
    public class VoxelModifierEmissionOpacityOpacify : VoxelModifierEmissionOpacity
    {
        public float Amount = 2.0f;
        public override void CollectAttributes(List<AttributeStream> attributes, VoxelizationStage stage, bool output) { }

        public override ShaderSource GetApplier(string layout)
        {
            return new ShaderClassSource("VoxelModifierApplierOpacify" + layout);
        }

        ValueParameterKey<float> AmountKey;
        public override void UpdateVoxelizationLayout(string compositionName)
        {
            AmountKey = VoxelModifierApplierOpacifyIsotropicKeys.Amount.ComposeWith(compositionName);
        }

        public override void ApplyVoxelizationParameters(ParameterCollection parameters)
        {
            parameters.Set(AmountKey, Amount);
        }
    }
}
