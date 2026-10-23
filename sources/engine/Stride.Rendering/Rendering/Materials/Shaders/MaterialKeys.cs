// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    public class MaterialKeys
    {
        public static readonly PermutationParameterKey<ShaderSource> VertexStageSurfaceShaders = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(MaterialKeys), "Material.VertexStageSurfaceShaders");
        public static readonly PermutationParameterKey<ShaderSource> DomainStageSurfaceShaders = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(MaterialKeys), "Material.DomainStageSurfaceShaders");
        public static readonly PermutationParameterKey<ShaderSource> PixelStageSurfaceShaders = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(MaterialKeys), "Material.PixelStageSurfaceShaders");
        
        public static readonly PermutationParameterKey<ShaderSource> VertexStageStreamInitializer = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(MaterialKeys), "Material.VertexStageStreamInitializer");
        public static readonly PermutationParameterKey<ShaderSource> DomainStageStreamInitializer = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(MaterialKeys), "Material.DomainStageStreamInitializer");
        public static readonly PermutationParameterKey<ShaderSource> PixelStageStreamInitializer = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(MaterialKeys), "Material.PixelStageStreamInitializer");

        public static readonly PermutationParameterKey<ShaderSource> TessellationShader = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(MaterialKeys), "Material.TessellationShader");

        public static readonly PermutationParameterKey<ShaderSource> PixelStageSurfaceFilter = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(MaterialKeys), "Material.PixelStageSurfaceFilter");

        public static readonly ObjectParameterKey<Texture> BlendMap = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(MaterialKeys), "Material.BlendMap");
        public static readonly ValueParameterKey<float> BlendValue = (ValueParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewValue<float>(), typeof(MaterialKeys), "Material.BlendValue");

        public static readonly ObjectParameterKey<Texture> DisplacementMap = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(MaterialKeys), "Material.DisplacementMap");
        public static readonly ValueParameterKey<float> DisplacementValue = (ValueParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewValue<float>(), typeof(MaterialKeys), "Material.DisplacementValue");

        public static readonly ObjectParameterKey<Texture> DisplacementIntensityMap = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(MaterialKeys), "Material.DisplacementIntensityMap");
        public static readonly ValueParameterKey<float> DisplacementIntensityValue = (ValueParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewValue<float>(), typeof(MaterialKeys), "Material.DisplacementIntensityValue");

        public static readonly ObjectParameterKey<Texture> NormalMap = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(MaterialKeys), "Material.NormalMap");
        public static readonly ValueParameterKey<Vector3> NormalValue = (ValueParameterKey<Vector3>)ParameterKeys.Merge(ParameterKeys.NewValue<Vector3>(), typeof(MaterialKeys), "Material.NormalValue");

        public static readonly ObjectParameterKey<Texture> DiffuseMap = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(MaterialKeys), "Material.DiffuseMap");
        public static readonly ValueParameterKey<Color4> DiffuseValue = (ValueParameterKey<Color4>)ParameterKeys.Merge(ParameterKeys.NewValue<Color4>(), typeof(MaterialKeys), "Material.DiffuseValue");

        public static readonly ObjectParameterKey<Texture> DiffuseSpecularAlphaBlendMap = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(MaterialKeys), "Material.DiffuseSpecularAlphaBlendMap");
        public static readonly ValueParameterKey<float> DiffuseSpecularAlphaBlendValue = (ValueParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewValue<float>(), typeof(MaterialKeys), "Material.DiffuseSpecularAlphaBlendValue");

        public static readonly ObjectParameterKey<Texture> AlphaBlendColorMap = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(MaterialKeys), "Material.AlphaBlendColorMap");
        public static readonly ValueParameterKey<Color3> AlphaBlendColorValue = (ValueParameterKey<Color3>)ParameterKeys.Merge(ParameterKeys.NewValue<Color3>(), typeof(MaterialKeys), "Material.AlphaBlendColorValue");

        public static readonly ObjectParameterKey<Texture> AlphaDiscardMap = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(MaterialKeys), "Material.AlphaDiscardMap");
        public static readonly ValueParameterKey<float> AlphaDiscardValue = (ValueParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewValue<float>(), typeof(MaterialKeys), "Material.AlphaDiscardValue");

        public static readonly ObjectParameterKey<Texture> SpecularMap = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(MaterialKeys), "Material.SpecularMap");
        public static readonly ValueParameterKey<Color3> SpecularValue = (ValueParameterKey<Color3>)ParameterKeys.Merge(ParameterKeys.NewValue<Color3>(), typeof(MaterialKeys), "Material.SpecularValue");
        public static readonly ValueParameterKey<float> SpecularIntensityValue = (ValueParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewValue<float>(), typeof(MaterialKeys), "Material.SpecularIntensityValue");
        
        public static readonly ObjectParameterKey<Texture> GlossinessMap = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(MaterialKeys), "Material.GlossinessMap");
        public static readonly ValueParameterKey<float> GlossinessValue = (ValueParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewValue<float>(), typeof(MaterialKeys), "Material.GlossinessValue");

        public static readonly ObjectParameterKey<Texture> AmbientOcclusionMap = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(MaterialKeys), "Material.AmbientOcclusionMap");
        public static readonly ValueParameterKey<float> AmbientOcclusionValue = (ValueParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewValue<float>(), typeof(MaterialKeys), "Material.AmbientOcclusionValue");
        public static readonly ValueParameterKey<float> AmbientOcclusionDirectLightingFactorValue = (ValueParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewValue<float>(), typeof(MaterialKeys), "Material.AmbientOcclusionDirectLightingFactorValue");

        public static readonly ObjectParameterKey<Texture> CavityMap = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(MaterialKeys), "Material.CavityMap");
        public static readonly ValueParameterKey<float> CavityValue = (ValueParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewValue<float>(), typeof(MaterialKeys), "Material.CavityValue");

        public static readonly ValueParameterKey<float> CavityDiffuseValue = (ValueParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewValue<float>(), typeof(MaterialKeys), "Material.CavityDiffuseValue");
        public static readonly ValueParameterKey<float> CavitySpecularValue = (ValueParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewValue<float>(), typeof(MaterialKeys), "Material.CavitySpecularValue");

        public static readonly ObjectParameterKey<Texture> MetalnessMap = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(MaterialKeys), "Material.MetalnessMap");
        public static readonly ValueParameterKey<float> MetalnessValue = (ValueParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewValue<float>(), typeof(MaterialKeys), "Material.MetalnessValue");

        public static readonly ObjectParameterKey<Texture> EmissiveMap = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(MaterialKeys), "Material.EmissiveMap");
        public static readonly ValueParameterKey<Color4> EmissiveValue = (ValueParameterKey<Color4>)ParameterKeys.Merge(ParameterKeys.NewValue<Color4>(), typeof(MaterialKeys), "Material.EmissiveValue");

        public static readonly ObjectParameterKey<Texture> EmissiveIntensityMap = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(MaterialKeys), "Material.EmissiveIntensityMap");
        public static readonly ValueParameterKey<float> EmissiveIntensity = (ValueParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewValue<float>(), typeof(MaterialKeys), "Material.EmissiveIntensity");

        public static readonly ObjectParameterKey<Texture> ScatteringStrengthMap = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(MaterialKeys), "Material.ScatteringStrengthMap");
        public static readonly ValueParameterKey<float> ScatteringStrengthValue = (ValueParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewValue<float>(), typeof(MaterialKeys), "Material.ScatteringStrengthValue");

        /// <summary>
        /// Generic texture key used by a material
        /// </summary>
        public static readonly ObjectParameterKey<Texture> GenericTexture = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(MaterialKeys), "Material.GenericTexture");

        /// <summary>
        /// Generic texture key used by a material
        /// </summary>
        public static readonly ValueParameterKey<Color3> GenericValueColor3 = (ValueParameterKey<Color3>)ParameterKeys.Merge(ParameterKeys.NewValue<Color3>(), typeof(MaterialKeys), "Material.GenericValueColor3");

        /// <summary>
        /// Generic texture key used by a material
        /// </summary>
        public static readonly ValueParameterKey<Color4> GenericValueColor4 = (ValueParameterKey<Color4>)ParameterKeys.Merge(ParameterKeys.NewValue<Color4>(), typeof(MaterialKeys), "Material.GenericValueColor4");

        /// <summary>
        /// Generic texture key used by a material
        /// </summary>
        public static readonly ValueParameterKey<Vector4> GenericValueVector4 = (ValueParameterKey<Vector4>)ParameterKeys.Merge(ParameterKeys.NewValue<Vector4>(), typeof(MaterialKeys), "Material.GenericValueVector4");

        /// <summary>
        /// Texture UV scaling
        /// </summary>
        public static readonly ValueParameterKey<Vector2> TextureScale = (ValueParameterKey<Vector2>)ParameterKeys.Merge(ParameterKeys.NewValue<Vector2>(Vector2.One), typeof(MaterialKeys), "Material.TextureScale");

        /// <summary>
        /// Texture UV offset
        /// </summary>
        public static readonly ValueParameterKey<Vector2> TextureOffset = (ValueParameterKey<Vector2>)ParameterKeys.Merge(ParameterKeys.NewValue<Vector2>(), typeof(MaterialKeys), "Material.TextureOffset");

        /// <summary>
        /// Generic texture key used by a material
        /// </summary>
        public static readonly ValueParameterKey<float> GenericValueFloat = (ValueParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewValue<float>(), typeof(MaterialKeys), "Material.GenericValueFloat");

        /// <summary>
        /// Generic sampler key used by a material
        /// </summary>
        public static readonly ObjectParameterKey<SamplerState> Sampler = (ObjectParameterKey<SamplerState>)ParameterKeys.Merge(ParameterKeys.NewObject<SamplerState>(), typeof(MaterialKeys), "Material.Sampler");

        public static readonly PermutationParameterKey<bool> HasSkinningPosition = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation<bool>(), typeof(MaterialKeys), "Material.HasSkinningPosition");

        public static readonly PermutationParameterKey<bool> HasSkinningNormal = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation<bool>(), typeof(MaterialKeys), "Material.HasSkinningNormal");

        public static readonly PermutationParameterKey<bool> HasNormalMap = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation<bool>(), typeof(MaterialKeys), "Material.HasNormalMap");

        public static readonly PermutationParameterKey<bool> HasSkinningTangent = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation<bool>(), typeof(MaterialKeys), "Material.HasSkinningTangent");

        public static readonly PermutationParameterKey<int> SkinningMaxBones = (PermutationParameterKey<int>)ParameterKeys.Merge(ParameterKeys.NewPermutation<int>(56), typeof(MaterialKeys), "Material.SkinningMaxBones");
        
        public static readonly PermutationParameterKey<bool> UsePixelShaderWithDepthPass = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation<bool>(), typeof(MaterialKeys), "Material.UsePixelShaderWithDepthPass");
        
        public static readonly PermutationParameterKey<bool> UseDitheredShadows = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation<bool>(), typeof(MaterialKeys), "Material.UseDitheredShadows");

        static MaterialKeys()
        {
            //SpecularPowerScaled = ParameterKeys.NewDynamic(ParameterDynamicValue.New<float, float>(SpecularPower, ScaleSpecularPower));
        }

        private static void ScaleSpecularPower(ref float specularPower, ref float scaledSpecularPower)
        {
            scaledSpecularPower = MathF.Pow(2.0f, 1.0f + specularPower * 13.0f);
        }

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(MaterialKeys).TypeHandle);
        }
    }
}
