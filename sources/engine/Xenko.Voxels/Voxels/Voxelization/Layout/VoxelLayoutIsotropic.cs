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
    [Display("Isotropic (single)")]
    public class VoxelLayoutIsotropic : IVoxelLayout
    {
        public enum StorageFormats
        {
            R10G10B10A2,
            RGBA8,
            RGBA16F,
        };

        [NotNull]
        public IVoxelStorageMethod StorageMethod { get; set; } = new VoxelStorageMethodIndirect();

        public StorageFormats StorageFormat { get; set; } = StorageFormats.RGBA16F;

        [Display("Max Brightness (non float format)")]
        public float maxBrightness = 10.0f;




        IVoxelStorageTexture IsotropicTex;

        public int PrepareLocalStorage(VoxelStorageContext context, IVoxelStorage storage)
        {
            return StorageMethod.PrepareLocalStorage(context, storage, 4, 1);
        }
        public void PrepareOutputStorage(VoxelStorageContext context, IVoxelStorage storage)
        {
            Graphics.PixelFormat format = Graphics.PixelFormat.R16G16B16A16_Float;
            switch (StorageFormat)
            {
                case StorageFormats.RGBA8:
                    format = Graphics.PixelFormat.R8G8B8A8_UNorm;
                    break;
                case StorageFormats.R10G10B10A2:
                    format = Graphics.PixelFormat.R10G10B10A2_UNorm;
                    break;
                case StorageFormats.RGBA16F:
                    format = Graphics.PixelFormat.R16G16B16A16_Float;
                    break;
            }
            storage.UpdateTexture(context, ref IsotropicTex, format, 1);
        }
        public void ClearOutputStorage()
        {
            IsotropicTex = null;
        }

        public void PostProcess(RenderDrawContext drawContext, string MipMapShader)
        {
            IsotropicTex.PostProcess(drawContext, MipMapShader);
        }




        ShaderClassSource writer = new ShaderClassSource("VoxelIsotropicWriter_Float4");
        ValueParameterKey<float> BrightnessInvKey;
        ObjectParameterKey<Xenko.Graphics.Texture> DirectOutput;

        public ShaderSource GetVoxelizationShader(List<IVoxelModifierEmissionOpacity> modifiers)
        {
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(writer);
            StorageMethod.Apply(mixin);
            foreach (var attr in modifiers)
            {
                ShaderSource applier = attr.GetApplier("Isotropic");
                if (applier != null)
                    mixin.AddCompositionToArray("Modifiers", applier);
            }
            return mixin;
        }
        public void UpdateVoxelizationLayout(string compositionName, List<IVoxelModifierEmissionOpacity> modifiers)
        {
            DirectOutput = VoxelIsotropicWriter_Float4Keys.DirectOutput.ComposeWith(compositionName);
            BrightnessInvKey = VoxelIsotropicWriter_Float4Keys.maxBrightnessInv.ComposeWith(compositionName);
        }
        public void ApplyVoxelizationParameters(ParameterCollection parameters, List<IVoxelModifierEmissionOpacity> modifiers)
        {
            if (StorageFormat != StorageFormats.RGBA16F)
                parameters.Set(BrightnessInvKey, 1.0f / maxBrightness);
            else
                parameters.Set(BrightnessInvKey, 1.0f);
            IsotropicTex.ApplyVoxelizationParameters(DirectOutput, parameters);
        }




        ValueParameterKey<float> BrightnessKey;
        ShaderClassSource sampler = new ShaderClassSource("VoxelIsotropicSampler");

        public ShaderSource GetSamplingShader()
        {
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(sampler);
            mixin.AddComposition("storage", IsotropicTex.GetSamplingShader());
            return mixin;
        }
        public void UpdateSamplingLayout(string compositionName)
        {
            BrightnessKey = VoxelIsotropicSamplerKeys.maxBrightness.ComposeWith(compositionName);
            IsotropicTex.UpdateSamplingLayout("storage."+compositionName);
        }
        public void ApplySamplingParameters(VoxelViewContext viewContext, ParameterCollection parameters)
        {
            if (StorageFormat != StorageFormats.RGBA16F)
                parameters.Set(BrightnessKey, maxBrightness);
            else
                parameters.Set(BrightnessKey, 1.0f);
            IsotropicTex.ApplySamplingParameters(viewContext, parameters);
        }
    }
}
