using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Shaders;
using Xenko.Rendering.Materials;

namespace Xenko.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Anisotropic (3 sided)")]
    public class VoxelLayoutAnisotropicPaired : IVoxelLayout
    {
        [NotNull]
        public IVoxelStorageMethod StorageMethod { get; set; } = new VoxelStorageMethodIndirect();



        IVoxelStorageTexture IsotropicTex;

        public int PrepareLocalStorage(VoxelStorageContext context, IVoxelStorage storage)
        {
            return StorageMethod.PrepareLocalStorage(context, storage, 4, 3);
        }
        public void PrepareOutputStorage(VoxelStorageContext context, IVoxelStorage storage)
        {
            storage.UpdateTexture(context, ref IsotropicTex, Graphics.PixelFormat.R16G16B16A16_Float, 3);
        }
        public void ClearOutputStorage()
        {
            IsotropicTex = null;
        }

        public void PostProcess(RenderDrawContext drawContext, string MipMapShader)
        {
            IsotropicTex.PostProcess(drawContext, MipMapShader);
        }




        ShaderClassSource writer = new ShaderClassSource("VoxelAnisotropicPairedWriter_Float4");
        ObjectParameterKey<Xenko.Graphics.Texture> DirectOutput;

        public ShaderSource GetVoxelizationShader(List<IVoxelModifierEmissionOpacity> modifiers)
        {
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(writer);
            StorageMethod.Apply(mixin);
            foreach (var attr in modifiers)
            {
                ShaderSource applier = attr.GetApplier("AnisotropicPaired");
                if (applier != null)
                    mixin.AddCompositionToArray("Modifiers", applier);
            }
            return mixin;
        }
        public void UpdateVoxelizationLayout(string compositionName, List<IVoxelModifierEmissionOpacity> modifier)
        {
            DirectOutput = VoxelAnisotropicPairedWriter_Float4Keys.DirectOutput.ComposeWith(compositionName);
        }
        public void ApplyVoxelizationParameters(ParameterCollection parameters, List<IVoxelModifierEmissionOpacity> modifiers)
        {
            IsotropicTex.ApplyVoxelizationParameters(DirectOutput, parameters);
        }




        ShaderClassSource sampler = new ShaderClassSource("VoxelAnisotropicPairedSampler");

        public ShaderSource GetSamplingShader()
        {
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(sampler);
            mixin.AddComposition("storage", IsotropicTex.GetSamplingShader());
            return mixin;
        }
        public void UpdateSamplingLayout(string compositionName)
        {
            IsotropicTex.UpdateSamplingLayout("storage."+compositionName);
        }
        public void ApplySamplingParameters(VoxelViewContext viewContext, ParameterCollection parameters)
        {
            IsotropicTex.ApplySamplingParameters(viewContext, parameters);
        }
    }
}
