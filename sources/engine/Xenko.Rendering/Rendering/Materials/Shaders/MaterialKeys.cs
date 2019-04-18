// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Shaders;

namespace Xenko.Rendering.Materials
{
    public class MaterialKeys
    {
        public static readonly PermutationParameterKey<ShaderSource> VertexStageSurfaceShaders = ParameterKeys.NewPermutation<ShaderSource>();
        public static readonly PermutationParameterKey<ShaderSource> DomainStageSurfaceShaders = ParameterKeys.NewPermutation<ShaderSource>();
        public static readonly PermutationParameterKey<ShaderSource> PixelStageSurfaceShaders = ParameterKeys.NewPermutation<ShaderSource>();
        
        public static readonly PermutationParameterKey<ShaderSource> VertexStageStreamInitializer = ParameterKeys.NewPermutation<ShaderSource>();
        public static readonly PermutationParameterKey<ShaderSource> DomainStageStreamInitializer = ParameterKeys.NewPermutation<ShaderSource>();
        public static readonly PermutationParameterKey<ShaderSource> PixelStageStreamInitializer = ParameterKeys.NewPermutation<ShaderSource>();

        public static readonly PermutationParameterKey<ShaderSource> TessellationShader = ParameterKeys.NewPermutation<ShaderSource>();

        public static readonly PermutationParameterKey<ShaderSource> PixelStageSurfaceFilter = ParameterKeys.NewPermutation<ShaderSource>();

        public static readonly ObjectParameterKey<Texture> BlendMap = ParameterKeys.NewObject<Texture>();
        public static readonly ValueParameterKey<float> BlendValue = ParameterKeys.NewValue<float>();

        public static readonly ObjectParameterKey<Texture> DisplacementMap = ParameterKeys.NewObject<Texture>();
        public static readonly ValueParameterKey<float> DisplacementValue = ParameterKeys.NewValue<float>();

        public static readonly ObjectParameterKey<Texture> DisplacementIntensityMap = ParameterKeys.NewObject<Texture>();
        public static readonly ValueParameterKey<float> DisplacementIntensityValue = ParameterKeys.NewValue<float>();

        public static readonly ObjectParameterKey<Texture> NormalMap = ParameterKeys.NewObject<Texture>();
        public static readonly ValueParameterKey<Vector3> NormalValue = ParameterKeys.NewValue<Vector3>();

        public static readonly ObjectParameterKey<Texture> DiffuseMap = ParameterKeys.NewObject<Texture>();
        public static readonly ValueParameterKey<Color4> DiffuseValue = ParameterKeys.NewValue<Color4>();

        public static readonly ObjectParameterKey<Texture> DiffuseSpecularAlphaBlendMap = ParameterKeys.NewObject<Texture>();
        public static readonly ValueParameterKey<float> DiffuseSpecularAlphaBlendValue = ParameterKeys.NewValue<float>();

        public static readonly ObjectParameterKey<Texture> AlphaBlendColorMap = ParameterKeys.NewObject<Texture>();
        public static readonly ValueParameterKey<Color3> AlphaBlendColorValue = ParameterKeys.NewValue<Color3>();

        public static readonly ObjectParameterKey<Texture> AlphaDiscardMap = ParameterKeys.NewObject<Texture>();
        public static readonly ValueParameterKey<float> AlphaDiscardValue = ParameterKeys.NewValue<float>();

        public static readonly ObjectParameterKey<Texture> SpecularMap = ParameterKeys.NewObject<Texture>();
        public static readonly ValueParameterKey<Color3> SpecularValue = ParameterKeys.NewValue<Color3>();
        public static readonly ValueParameterKey<float> SpecularIntensityValue = ParameterKeys.NewValue<float>();
        
        public static readonly ObjectParameterKey<Texture> GlossinessMap = ParameterKeys.NewObject<Texture>();
        public static readonly ValueParameterKey<float> GlossinessValue = ParameterKeys.NewValue<float>();

        public static readonly ObjectParameterKey<Texture> AmbientOcclusionMap = ParameterKeys.NewObject<Texture>();
        public static readonly ValueParameterKey<float> AmbientOcclusionValue = ParameterKeys.NewValue<float>();
        public static readonly ValueParameterKey<float> AmbientOcclusionDirectLightingFactorValue = ParameterKeys.NewValue<float>();

        public static readonly ObjectParameterKey<Texture> CavityMap = ParameterKeys.NewObject<Texture>();
        public static readonly ValueParameterKey<float> CavityValue = ParameterKeys.NewValue<float>();

        public static readonly ValueParameterKey<float> CavityDiffuseValue = ParameterKeys.NewValue<float>();
        public static readonly ValueParameterKey<float> CavitySpecularValue = ParameterKeys.NewValue<float>();

        public static readonly ObjectParameterKey<Texture> MetalnessMap = ParameterKeys.NewObject<Texture>();
        public static readonly ValueParameterKey<float> MetalnessValue = ParameterKeys.NewValue<float>();

        public static readonly ObjectParameterKey<Texture> EmissiveMap = ParameterKeys.NewObject<Texture>();
        public static readonly ValueParameterKey<Color4> EmissiveValue = ParameterKeys.NewValue<Color4>();

        public static readonly ObjectParameterKey<Texture> EmissiveIntensityMap = ParameterKeys.NewObject<Texture>();
        public static readonly ValueParameterKey<float> EmissiveIntensity = ParameterKeys.NewValue<float>();

        public static readonly ObjectParameterKey<Texture> ScatteringStrengthMap = ParameterKeys.NewObject<Texture>();
        public static readonly ValueParameterKey<float> ScatteringStrengthValue = ParameterKeys.NewValue<float>();

        /// <summary>
        /// Generic texture key used by a material
        /// </summary>
        public static readonly ObjectParameterKey<Texture> GenericTexture = ParameterKeys.NewObject<Texture>();

        /// <summary>
        /// Generic texture key used by a material
        /// </summary>
        public static readonly ValueParameterKey<Color3> GenericValueColor3 = ParameterKeys.NewValue<Color3>();

        /// <summary>
        /// Generic texture key used by a material
        /// </summary>
        public static readonly ValueParameterKey<Color4> GenericValueColor4 = ParameterKeys.NewValue<Color4>();

        /// <summary>
        /// Generic texture key used by a material
        /// </summary>
        public static readonly ValueParameterKey<Vector4> GenericValueVector4 = ParameterKeys.NewValue<Vector4>();

        /// <summary>
        /// Texture UV scaling
        /// </summary>
        public static readonly ValueParameterKey<Vector2> TextureScale = ParameterKeys.NewValue<Vector2>(Vector2.One);

        /// <summary>
        /// Texture UV offset
        /// </summary>
        public static readonly ValueParameterKey<Vector2> TextureOffset = ParameterKeys.NewValue<Vector2>();

        /// <summary>
        /// Generic texture key used by a material
        /// </summary>
        public static readonly ValueParameterKey<float> GenericValueFloat = ParameterKeys.NewValue<float>();

        /// <summary>
        /// Generic sampler key used by a material
        /// </summary>
        public static readonly ObjectParameterKey<SamplerState> Sampler = ParameterKeys.NewObject<SamplerState>();

        public static readonly PermutationParameterKey<bool> HasSkinningPosition = ParameterKeys.NewPermutation<bool>();

        public static readonly PermutationParameterKey<bool> HasSkinningNormal = ParameterKeys.NewPermutation<bool>();

        public static readonly PermutationParameterKey<bool> HasNormalMap = ParameterKeys.NewPermutation<bool>();

        public static readonly PermutationParameterKey<bool> HasSkinningTangent = ParameterKeys.NewPermutation<bool>();

        public static readonly PermutationParameterKey<int> SkinningMaxBones = ParameterKeys.NewPermutation<int>(56);
        
        public static readonly PermutationParameterKey<bool> UsePixelShaderWithDepthPass = ParameterKeys.NewPermutation<bool>();

        static MaterialKeys()
        {
            //SpecularPowerScaled = ParameterKeys.NewDynamic(ParameterDynamicValue.New<float, float>(SpecularPower, ScaleSpecularPower));
        }

        private static void ScaleSpecularPower(ref float specularPower, ref float scaledSpecularPower)
        {
            scaledSpecularPower = (float)Math.Pow(2.0f, 1.0f + specularPower * 13.0f);
        }
    }
}
