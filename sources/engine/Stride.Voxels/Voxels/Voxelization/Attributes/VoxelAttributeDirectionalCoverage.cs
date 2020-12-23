// Copyright (c) Stride contributors (https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Shaders;
using Stride.Rendering.Materials;
using Stride.Core.Mathematics;

namespace Stride.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Directional Coverage")]
    public class VoxelAttributeDirectionalCoverage : VoxelAttribute
    {
        IVoxelStorageTexture CoverageTex;

        public override void PrepareLocalStorage(VoxelStorageContext context, IVoxelStorage storage)
        {
            BufferOffset = storage.RequestTempStorage(32);
        }
        public override void PrepareOutputStorage(VoxelStorageContext context, IVoxelStorage storage)
        {
            storage.UpdateTexture(context, ref CoverageTex, Graphics.PixelFormat.R11G11B10_Float, 1);
        }
        public override void ClearOutputStorage()
        {
            CoverageTex = null;
        }




        public override void CollectVoxelizationPasses(VoxelizationPassList passList, IVoxelStorer storer, Matrix view, Vector3 resolution, VoxelizationStage stage, bool output)
        {
            passList.defaultVoxelizationMethod.CollectVoxelizationPasses(passList, storer, view, resolution, this, stage, output, false);
        }
        public override void CollectAttributes(List<AttributeStream> attributes, VoxelizationStage stage, bool output)
        {
            attributes.Add(new AttributeStream(this, VoxelizationStage.Post, output));
        }

        ShaderSource[] mipmapper = { new ShaderClassSource("Voxel2x2x2MipmapperSimple") };
        public override void PostProcess(RenderDrawContext drawContext)
        {
            CoverageTex.PostProcess(drawContext, mipmapper);
        }




        ShaderClassSource source = new ShaderClassSource("VoxelAttributeDirectionalCoverageShader");
        ObjectParameterKey<Stride.Graphics.Texture> DirectOutput;

        public override ShaderSource GetVoxelizationShader()
        {
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(source);
            return mixin;
        }
        public override void UpdateVoxelizationLayout(string compositionName)
        {
            DirectOutput = VoxelAttributeDirectionalCoverageShaderKeys.DirectOutput.ComposeWith(compositionName);
        }
        public override void ApplyVoxelizationParameters(ParameterCollection parameters)
        {
            CoverageTex?.ApplyVoxelizationParameters(DirectOutput, parameters);
        }




        ShaderClassSource sampler = new ShaderClassSource("VoxelAttributeDirectionalCoverageSampler");

        public override ShaderSource GetSamplingShader()
        {
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(sampler);
            if (CoverageTex!=null)
                mixin.AddComposition("storage", CoverageTex.GetSamplingShader());
            return mixin;
        }
        public override void UpdateSamplingLayout(string compositionName)
        {
            CoverageTex?.UpdateSamplingLayout("storage." + compositionName);
        }
        public override void ApplySamplingParameters(VoxelViewContext viewContext, ParameterCollection parameters)
        {
            CoverageTex?.ApplySamplingParameters(viewContext, parameters);
        }
    }
}
