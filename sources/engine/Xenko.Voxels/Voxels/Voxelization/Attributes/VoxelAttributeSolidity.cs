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
    [Display("Solidity")]
    public class VoxelAttributeSolidity : VoxelAttributeBase, IVoxelAttribute
    {
        IVoxelStorageTexture SolidityTex;

        public void PrepareLocalStorage(VoxelStorageContext context, IVoxelStorage storage)
        {
            BufferOffset = storage.RequestTempStorage(64);
        }
        public void PrepareOutputStorage(VoxelStorageContext context, IVoxelStorage storage)
        {
            storage.UpdateTexture(context, ref SolidityTex, Graphics.PixelFormat.R8_UNorm, 1);
        }
        public void ClearOutputStorage()
        {
            SolidityTex = null;
        }




        VoxelizationMethodSingleAxis method = new VoxelizationMethodSingleAxis
        {
            MultisampleCount = Graphics.MultisampleCount.None,
            VoxelizationAxis = VoxelizationMethodSingleAxis.Axis.Y
        };
        public void CollectVoxelizationPasses(VoxelizationPassList passList, IVoxelStorer storer, Matrix view, Vector3 resolution, VoxelizationStage stage, bool output)
        {
            method.CollectVoxelizationPasses(passList, storer, view, resolution, this, stage, output, false);
        }
        public void CollectAttributes(List<AttributeStream> attributes, VoxelizationStage stage, bool output)
        {
            attributes.Add(new AttributeStream(this, VoxelizationStage.Post, output));
        }

        override public bool RequiresColumns()
        {
            return false;
        }
        ShaderSource[] mipmapper = { new ShaderClassSource("Voxel2x2x2MipmapperSimple") };
        
        public void PostProcess(RenderDrawContext drawContext)
        {
            SolidityTex.PostProcess(drawContext, mipmapper);
        }




        ShaderClassSource source = new ShaderClassSource("VoxelAttributeSolidityShader");
        ObjectParameterKey<Xenko.Graphics.Texture> DirectOutput;

        public ShaderSource GetVoxelizationShader()
        {
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(source);
            return mixin;
        }
        public void UpdateVoxelizationLayout(string compositionName)
        {
            DirectOutput = VoxelAttributeSolidityShaderKeys.DirectOutput.ComposeWith(compositionName);
        }
        public void ApplyVoxelizationParameters(ParameterCollection parameters)
        {
            SolidityTex?.ApplyVoxelizationParameters(DirectOutput, parameters);
        }




        ValueParameterKey<float> BrightnessKey;
        ShaderClassSource sampler = new ShaderClassSource("VoxelAttributeSoliditySampler");

        public ShaderSource GetSamplingShader()
        {
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(sampler);
            if (SolidityTex!=null)
            mixin.AddComposition("storage", SolidityTex.GetSamplingShader());
            return mixin;
        }
        public void UpdateSamplingLayout(string compositionName)
        {
            BrightnessKey = VoxelIsotropicSamplerKeys.maxBrightness.ComposeWith(compositionName);
            SolidityTex?.UpdateSamplingLayout("storage." + compositionName);
        }
        public void ApplySamplingParameters(VoxelViewContext viewContext, ParameterCollection parameters)
        {
            parameters.Set(BrightnessKey, 1.0f);
            SolidityTex?.ApplySamplingParameters(viewContext, parameters);
        }
    }
}
