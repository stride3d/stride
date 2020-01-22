using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Shaders;
using Xenko.Rendering.Materials;
using Xenko.Core.Mathematics;

namespace Xenko.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Directional Coverage")]
    public class VoxelAttributeDirectionalCoverage : VoxelAttributeBase, IVoxelAttribute
    {
        IVoxelStorageTexture CoverageTex;

        public void PrepareLocalStorage(VoxelStorageContext context, IVoxelStorage storage)
        {
            BufferOffset = storage.RequestTempStorage(32);
        }
        public void PrepareOutputStorage(VoxelStorageContext context, IVoxelStorage storage)
        {
            storage.UpdateTexture(context, ref CoverageTex, Graphics.PixelFormat.R11G11B10_Float, 1);
        }
        public void ClearOutputStorage()
        {
            CoverageTex = null;
        }


        

        public void CollectVoxelizationPasses(VoxelizationPassList passList, IVoxelStorer storer, Matrix view, Vector3 resolution, VoxelizationStage stage, bool output)
        {
            passList.defaultVoxelizationMethod.CollectVoxelizationPasses(passList, storer, view, resolution, this, stage, output, false);
        }
        public void CollectAttributes(List<AttributeStream> attributes, VoxelizationStage stage, bool output)
        {
            attributes.Add(new AttributeStream(this, VoxelizationStage.Post, output));
        }

        ShaderSource[] mipmapper = { new ShaderClassSource("Voxel2x2x2MipmapperSimple") };
        public void PostProcess(RenderDrawContext drawContext)
        {
            CoverageTex.PostProcess(drawContext, mipmapper);
        }




        ShaderClassSource source = new ShaderClassSource("VoxelAttributeDirectionalCoverageShader");
        ObjectParameterKey<Xenko.Graphics.Texture> DirectOutput;

        public ShaderSource GetVoxelizationShader()
        {
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(source);
            return mixin;
        }
        public void UpdateVoxelizationLayout(string compositionName)
        {
            DirectOutput = VoxelAttributeDirectionalCoverageShaderKeys.DirectOutput.ComposeWith(compositionName);
        }
        public void ApplyVoxelizationParameters(ParameterCollection parameters)
        {
            CoverageTex?.ApplyVoxelizationParameters(DirectOutput, parameters);
        }




        ShaderClassSource sampler = new ShaderClassSource("VoxelAttributeDirectionalCoverageSampler");

        public ShaderSource GetSamplingShader()
        {
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(sampler);
            if (CoverageTex!=null)
                mixin.AddComposition("storage", CoverageTex.GetSamplingShader());
            return mixin;
        }
        public void UpdateSamplingLayout(string compositionName)
        {
            CoverageTex?.UpdateSamplingLayout("storage." + compositionName);
        }
        public void ApplySamplingParameters(VoxelViewContext viewContext, ParameterCollection parameters)
        {
            CoverageTex?.ApplySamplingParameters(viewContext, parameters);
        }
    }
}
