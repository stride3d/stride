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
    [Display("Anti Aliasing")]
    public class VoxelModifierEmissionOpacityAntiAliasing : VoxelModifierEmissionOpacity
    {
        VoxelAttributeDirectionalCoverage directionalCoverage = new VoxelAttributeDirectionalCoverage();

        public override void CollectAttributes(List<AttributeStream> attributes, VoxelizationStage stage, bool output)
        {
            directionalCoverage.CollectAttributes(attributes, stage, output);
        }

        public override ShaderSource GetApplier(string layout)
        {
            return new ShaderClassSource("VoxelModifierApplierAntiAliasing" + layout, directionalCoverage.LocalSamplerID);
        }

        public override void UpdateVoxelizationLayout(string compositionName) { }
        public override void ApplyVoxelizationParameters(ParameterCollection parameters) { }
    }
}