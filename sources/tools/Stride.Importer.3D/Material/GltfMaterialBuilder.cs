// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Silk.NET.Assimp;
using Stride.Assets.Materials;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Importer.Common;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using AssimpMaterial = Silk.NET.Assimp.Material;

namespace Stride.Importer.ThreeD.Material
{
    /// <summary>
    /// Builds Stride materials from the glTF metallic-roughness data that Assimp's glTF importer exposes.
    /// </summary>
    internal sealed unsafe class GltfMaterialBuilder
    {
        private const string AlphaModeKey = "$mat.gltf.alphaMode";
        private const string AlphaCutoffKey = "$mat.gltf.alphaCutoff";
        private const string TextureStrengthKey = "$tex.strength";
        private const string TextureScaleKey = "$tex.scale";

        /// <summary>
        /// The texture slots read from glTF materials and the role of each texture.
        /// </summary>
        private static readonly (TextureType Type, TextureRole Role)[] TextureSlots =
        [
            (TextureType.BaseColor, TextureRole.Color),
            (TextureType.Diffuse, TextureRole.Color),
            (TextureType.Emissive, TextureRole.Color),
            (TextureType.EmissionColor, TextureRole.Color),
            (TextureType.Normals, TextureRole.Normal),
            (TextureType.GltfMetallicRoughness, TextureRole.Data),
            (TextureType.Metalness, TextureRole.Data),
            (TextureType.DiffuseRoughness, TextureRole.Data),
            (TextureType.Lightmap, TextureRole.Data),
            (TextureType.AmbientOcclusion, TextureRole.Data),
        ];

        private readonly Assimp assimp;
        private readonly Scene* scene;
        private readonly Logger logger;
        private HashSet<string> sharedBlendedImages;

        public GltfMaterialBuilder(Assimp assimp, Scene* scene, Logger logger)
        {
            this.assimp = assimp;
            this.scene = scene;
            this.logger = logger;
        }

        /// <summary>
        /// Returns <c>true</c> when the file is a glTF file, whose materials use the metallic-roughness model.
        /// </summary>
        public static bool IsGltfFile(string fileName)
        {
            var extension = System.IO.Path.GetExtension(fileName);
            return extension.Equals(".gltf", StringComparison.OrdinalIgnoreCase) || extension.Equals(".glb", StringComparison.OrdinalIgnoreCase);
        }

        public MaterialAsset Build(AssimpMaterial* material, string materialName)
        {
            var asset = new MaterialAsset();
            var attributes = asset.Attributes;

            var alphaMode = GetAlphaMode(material);

            // Stride blends with premultiplied alpha, so only blended materials premultiply their base color.
            var baseColor = GetColor(material, Assimp.MatkeyBaseColor) ?? GetColor(material, Assimp.MaterialColorDiffuseBase) ?? Vector4.One;
            baseColor.W = alphaMode == AlphaMode.Opaque ? 1.0f : GetFloat(material, Assimp.MatkeyOpacity) ?? 1.0f;
            var baseColorFactor = new ComputeColor(new Color4(baseColor).ToSRgb()) { PremultiplyAlpha = alphaMode == AlphaMode.Blend };
            var baseColorTexture = GetBaseColorTexture(material);
            if (alphaMode == AlphaMode.Blend && baseColorTexture is { } blended && GetSharedBlendedImages().Contains(blended.FileName))
                baseColorTexture = blended with { FileName = GetPremultipliedTextureName(blended.FileName) + System.IO.Path.GetExtension(blended.FileName) };
            attributes.Diffuse = new MaterialDiffuseMapFeature(Multiply(CreateColorNode(baseColorTexture), baseColorFactor));
            attributes.DiffuseModel = new MaterialDiffuseLambertModelFeature();

            var metallicRoughnessTexture = GetTexture(material, TextureType.GltfMetallicRoughness);
            var metallic = GetFloat(material, Assimp.MatkeyMetallicFactor) ?? 1.0f;
            var roughness = GetFloat(material, Assimp.MatkeyRoughnessFactor) ?? 1.0f;
            attributes.Specular = new MaterialMetalnessMapFeature(Multiply(CreateScalarNode(metallicRoughnessTexture ?? GetTexture(material, TextureType.Metalness), ColorChannel.B), metallic));
            attributes.SpecularModel = new MaterialSpecularMicrofacetModelFeature();
            attributes.MicroSurface = new MaterialGlossinessMapFeature(Multiply(CreateScalarNode(metallicRoughnessTexture ?? GetTexture(material, TextureType.DiffuseRoughness), ColorChannel.G), roughness))
            {
                Invert = true,
            };

            var occlusionTexture = GetTexture(material, TextureType.Lightmap) ?? GetTexture(material, TextureType.AmbientOcclusion);
            if (occlusionTexture is { } occlusion)
            {
                attributes.Occlusion = new MaterialOcclusionMapFeature
                {
                    AmbientOcclusionMap = ApplyStrength(CreateScalarNode(occlusion, ColorChannel.R), occlusion.Strength),
                };
            }

            if (GetTexture(material, TextureType.Normals) is { } normal)
            {
                attributes.Surface = new MaterialNormalMapFeature(CreateColorNode(normal));
                if (normal.Scale != 1.0f)
                    logger.Info($"Material '{materialName}': the normal texture scale {normal.Scale} is not supported and is ignored.");
            }

            var emissiveColor = GetColor(material, Assimp.MaterialColorEmissiveBase) ?? Vector4.Zero;
            if (emissiveColor.X > 0.0f || emissiveColor.Y > 0.0f || emissiveColor.Z > 0.0f)
            {
                var emissiveTexture = GetTexture(material, TextureType.Emissive) ?? GetTexture(material, TextureType.EmissionColor);
                var emissiveFactor = new ComputeColor(new Color4(emissiveColor.X, emissiveColor.Y, emissiveColor.Z, 1.0f).ToSRgb());
                attributes.Emissive = new MaterialEmissiveMapFeature(Multiply(CreateColorNode(emissiveTexture), emissiveFactor))
                {
                    Intensity = new ComputeFloat(GetFloat(material, Assimp.MatkeyEmissiveIntensity) ?? 1.0f),
                };
            }

            attributes.Transparency = alphaMode switch
            {
                AlphaMode.Blend => new MaterialTransparencyBlendFeature(),
                AlphaMode.Mask => new MaterialTransparencyCutoffFeature { Alpha = new ComputeFloat(GetFloat(material, AlphaCutoffKey) ?? 0.5f) },
                _ => null,
            };

            if (GetBool(material, Assimp.MatkeyTwosided))
                attributes.CullMode = CullMode.None;

            return asset;
        }

        /// <summary>
        /// Collects the textures referenced by glTF materials and the role of each one.
        /// </summary>
        public Dictionary<string, TextureRole> CollectTextureRoles(Func<string, string> resolvePath)
        {
            var roles = new Dictionary<string, TextureRole>();
            for (uint i = 0; i < scene->MNumMaterials; i++)
            {
                var material = scene->MMaterials[i];
                var baseColorTexture = GetBaseColorTexture(material);
                foreach (var (type, slotRole) in TextureSlots)
                {
                    if (GetTexture(material, type) is not { } texture)
                        continue;

                    var role = slotRole == TextureRole.Color && texture.FileName == baseColorTexture?.FileName && GetAlphaMode(material) == AlphaMode.Blend
                        ? TextureRole.BlendedColor
                        : slotRole;
                    // An image shared with other uses keeps their role; blended materials use its premultiplied copy instead.
                    if (role == TextureRole.BlendedColor && GetSharedBlendedImages().Contains(texture.FileName))
                        continue;

                    var path = resolvePath(texture.FileName);
                    if (!roles.TryGetValue(path, out var existing))
                    {
                        roles.Add(path, role);
                    }
                    else if (existing != role)
                    {
                        var chosen = RolePriority(role) > RolePriority(existing) ? role : existing;
                        roles[path] = chosen;
                        logger.Info($"Texture '{texture.FileName}' is used both as {existing} and {role}; it is imported as {chosen}.");
                    }
                }
            }
            return roles;
        }

        /// <summary>
        /// Collects the images that blended materials use as base color while other materials or slots use them too, by path,
        /// with the name of the premultiplied copy the blended materials reference.
        /// </summary>
        public Dictionary<string, string> CollectPremultipliedTextures(Func<string, string> resolvePath)
        {
            var textures = new Dictionary<string, string>();
            foreach (var fileName in GetSharedBlendedImages())
            {
                var name = GetPremultipliedTextureName(fileName);
                textures[resolvePath(fileName)] = name;
                logger.Info($"Texture '{fileName}' is the base color of a blended material and is also used elsewhere; blended materials use a premultiplied copy, '{name}'.");
            }
            return textures;
        }

        private static string GetPremultipliedTextureName(string fileName) => System.IO.Path.GetFileNameWithoutExtension(fileName) + "_premultiplied";

        /// <summary>
        /// Finds the images used as base color by a blended material and also by another material or slot. Stride blends with
        /// premultiplied alpha, so the blended materials need a premultiplied texture while the other uses need the straight one.
        /// </summary>
        private HashSet<string> GetSharedBlendedImages()
        {
            if (sharedBlendedImages != null)
                return sharedBlendedImages;

            var blended = new HashSet<string>();
            var other = new HashSet<string>();
            for (uint i = 0; i < scene->MNumMaterials; i++)
            {
                var material = scene->MMaterials[i];
                var isBlend = GetAlphaMode(material) == AlphaMode.Blend;
                var baseColorTexture = GetBaseColorTexture(material)?.FileName;
                foreach (var (type, slotRole) in TextureSlots)
                {
                    if (GetTexture(material, type) is not { } texture)
                        continue;
                    if (isBlend && slotRole == TextureRole.Color && texture.FileName == baseColorTexture)
                        blended.Add(texture.FileName);
                    else
                        other.Add(texture.FileName);
                }
            }

            blended.IntersectWith(other);
            return sharedBlendedImages = blended;
        }

        /// <summary>
        /// Decides the role of an image used in several roles: data types win over color, so channel values stay linear.
        /// </summary>
        private static int RolePriority(TextureRole role)
        {
            return role switch
            {
                TextureRole.Normal => 3,
                TextureRole.Data => 2,
                _ => 0,
            };
        }

        private GltfTexture? GetBaseColorTexture(AssimpMaterial* material)
        {
            return GetTexture(material, TextureType.BaseColor) ?? GetTexture(material, TextureType.Diffuse);
        }

        private AlphaMode GetAlphaMode(AssimpMaterial* material)
        {
            return GetString(material, AlphaModeKey) switch
            {
                "BLEND" => AlphaMode.Blend,
                "MASK" => AlphaMode.Mask,
                _ => AlphaMode.Opaque,
            };
        }

        private static IComputeColor Multiply(IComputeColor texture, ComputeColor factor)
        {
            if (texture is null)
                return factor;
            if (factor.Value == Color4.White)
                return texture;
            return new ComputeBinaryColor(texture, factor, BinaryOperator.Multiply);
        }

        private static IComputeScalar Multiply(IComputeScalar texture, float factor)
        {
            if (texture is null)
                return new ComputeFloat(factor);
            if (factor == 1.0f)
                return texture;
            return new ComputeBinaryScalar(texture, new ComputeFloat(factor), BinaryOperator.Multiply);
        }

        /// <summary>
        /// Applies the glTF occlusion strength: <c>1 + strength * (occlusion - 1)</c>.
        /// </summary>
        private static IComputeScalar ApplyStrength(IComputeScalar occlusion, float strength)
        {
            if (strength >= 1.0f)
                return occlusion;
            var scaled = new ComputeBinaryScalar(occlusion, new ComputeFloat(strength), BinaryOperator.Multiply);
            return new ComputeBinaryScalar(scaled, new ComputeFloat(1.0f - strength), BinaryOperator.Add);
        }

        private static ComputeTextureColor CreateColorNode(GltfTexture? texture)
        {
            if (texture is not { } t)
                return null;
            return TextureLayerGenerator.GenerateMaterialTextureNode(null, t.FileName, t.UVIndex, Vector2.One, t.AddressModeU, t.AddressModeV, null);
        }

        private static ComputeTextureScalar CreateScalarNode(GltfTexture? texture, ColorChannel channel)
        {
            if (texture is not { } t)
                return null;
            return TextureLayerGenerator.GenerateMaterialTextureScalarNode(t.FileName, t.UVIndex, channel, t.AddressModeU, t.AddressModeV);
        }

        private GltfTexture? GetTexture(AssimpMaterial* material, TextureType type)
        {
            if (assimp.GetMaterialTextureCount(material, type) == 0)
                return null;

            var path = new AssimpString();
            if (assimp.GetMaterialString(material, Assimp.MaterialTextureBase, (uint)type, 0, ref path) != Return.Success)
                return null;

            if (!Materials.TryGetTextureFileName(path.AsString, scene, out var fileName, out var errorMessage))
            {
                logger.Error(errorMessage);
                return null;
            }

            return new GltfTexture(
                fileName,
                (uint)(GetInt(material, Assimp.MaterialUvwsrcBase, type) ?? 0),
                ToAddressMode(GetInt(material, Assimp.MaterialMappingmodeUBase, type)),
                ToAddressMode(GetInt(material, Assimp.MaterialMappingmodeVBase, type)),
                GetFloat(material, TextureStrengthKey, type) ?? 1.0f,
                GetFloat(material, TextureScaleKey, type) ?? 1.0f);
        }

        private static TextureAddressMode ToAddressMode(int? mode)
        {
            return (TextureMapMode?)mode switch
            {
                TextureMapMode.Clamp => TextureAddressMode.Clamp,
                TextureMapMode.Mirror => TextureAddressMode.Mirror,
                TextureMapMode.Decal => TextureAddressMode.Border,
                _ => TextureAddressMode.Wrap,
            };
        }

        private Vector4? GetColor(AssimpMaterial* material, string key)
        {
            var color = System.Numerics.Vector4.Zero;
            if (assimp.GetMaterialColor(material, key, 0, 0, ref color) != Return.Success)
                return null;
            return new Vector4(color.X, color.Y, color.Z, color.W);
        }

        private float? GetFloat(AssimpMaterial* material, string key, TextureType type = TextureType.None)
        {
            var value = 0.0f;
            uint count = 1;
            if (assimp.GetMaterialFloatArray(material, key, (uint)type, 0, ref value, ref count) != Return.Success || count == 0)
                return null;
            return value;
        }

        private int? GetInt(AssimpMaterial* material, string key, TextureType type)
        {
            var value = 0;
            uint count = 1;
            if (assimp.GetMaterialIntegerArray(material, key, (uint)type, 0, ref value, ref count) != Return.Success || count == 0)
                return null;
            return value;
        }

        private string GetString(AssimpMaterial* material, string key)
        {
            var value = new AssimpString();
            return assimp.GetMaterialString(material, key, 0, 0, ref value) == Return.Success ? value.AsString : null;
        }

        private bool GetBool(AssimpMaterial* material, string key)
        {
            MaterialProperty* property = null;
            if (assimp.GetMaterialProperty(material, key, 0, 0, ref property) != Return.Success || property == null || property->MDataLength == 0)
                return false;
            return property->MData[0] != 0;
        }

        private enum AlphaMode
        {
            Opaque,
            Mask,
            Blend,
        }

        private readonly record struct GltfTexture(string FileName, uint UVIndex, TextureAddressMode AddressModeU, TextureAddressMode AddressModeV, float Strength, float Scale);
    }
}
