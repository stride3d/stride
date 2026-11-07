// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using Stride.Assets.Materials;
using Stride.Assets.Models;
using Stride.Assets.Textures;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Graphics;
using Stride.Importer.Common;
using Stride.Importer.ThreeD;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Xunit;

namespace Stride.Assets.Tests;

public sealed class TestGltfMaterialImport : IDisposable
{
    private static readonly string[] Images = { "basecolor", "orm", "normal", "emissive", "glass" };

    private const string OnePixelPng = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

    private readonly string directory = Path.Combine(Path.GetTempPath(), $"stride-gltf-materials-{Guid.NewGuid():N}");
    private readonly EntityInfo entity;

    public TestGltfMaterialImport()
    {
        Directory.CreateDirectory(directory);
        foreach (var image in Images)
            File.WriteAllBytes(Path.Combine(directory, image + ".png"), Convert.FromBase64String(OnePixelPng));

        var path = Path.Combine(directory, "materials.gltf");
        File.WriteAllText(path, JsonSerializer.Serialize(CreateGltf()));
        entity = new MeshConverter(null).ExtractEntity(path, path, extractTextureDependencies: true, deduplicateMaterials: false);
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);

    [Fact]
    public void OpaqueBaseColorIsTextureTimesFactorWithoutAlphaOrOcclusion()
    {
        var diffuse = Assert.IsType<MaterialDiffuseMapFeature>(Material("Painted").Diffuse);
        var product = AssertBinary(diffuse.DiffuseMap, BinaryOperator.Multiply);
        Assert.Equal("basecolor", TextureUrl(Assert.IsType<ComputeTextureColor>(product.LeftChild).Texture));
        var factor = Assert.IsType<ComputeColor>(product.RightChild);
        Assert.Equal(new Color4(0.5f, 0.5f, 0.5f, 1.0f).ToSRgb(), factor.Value);
        Assert.False(factor.PremultiplyAlpha);
        Assert.IsType<MaterialDiffuseLambertModelFeature>(Material("Painted").DiffuseModel);
    }

    [Fact]
    public void MetallicRoughnessReadsChannelsOfThePackedTexture()
    {
        var attributes = Material("Painted");

        var metalness = Assert.IsType<MaterialMetalnessMapFeature>(attributes.Specular);
        var metal = AssertBinary(metalness.MetalnessMap, BinaryOperator.Multiply);
        AssertScalarTexture(metal.LeftChild, "orm", ColorChannel.B);
        Assert.Equal(0.8f, Assert.IsType<ComputeFloat>(metal.RightChild).Value);
        Assert.IsType<MaterialSpecularMicrofacetModelFeature>(attributes.SpecularModel);

        var gloss = Assert.IsType<MaterialGlossinessMapFeature>(attributes.MicroSurface);
        Assert.True(gloss.Invert);
        var rough = AssertBinary(gloss.GlossinessMap, BinaryOperator.Multiply);
        AssertScalarTexture(rough.LeftChild, "orm", ColorChannel.G);
        Assert.Equal(0.6f, Assert.IsType<ComputeFloat>(rough.RightChild).Value);
    }

    [Fact]
    public void OcclusionReadsRedChannelWithStrength()
    {
        var occlusion = Assert.IsType<MaterialOcclusionMapFeature>(Material("Painted").Occlusion);
        var sum = AssertBinary(occlusion.AmbientOcclusionMap, BinaryOperator.Add);
        var scaled = AssertBinary(sum.LeftChild, BinaryOperator.Multiply);
        AssertScalarTexture(scaled.LeftChild, "orm", ColorChannel.R);
        Assert.Equal(0.25f, Assert.IsType<ComputeFloat>(scaled.RightChild).Value);
        Assert.Equal(0.75f, Assert.IsType<ComputeFloat>(sum.RightChild).Value);
    }

    [Fact]
    public void NormalEmissiveAndDoubleSidedAreMapped()
    {
        var attributes = Material("Painted");

        var normal = Assert.IsType<MaterialNormalMapFeature>(attributes.Surface);
        Assert.Equal("normal", TextureUrl(Assert.IsType<ComputeTextureColor>(normal.NormalMap).Texture));

        var emissive = Assert.IsType<MaterialEmissiveMapFeature>(attributes.Emissive);
        var product = AssertBinary(emissive.EmissiveMap, BinaryOperator.Multiply);
        Assert.Equal("emissive", TextureUrl(Assert.IsType<ComputeTextureColor>(product.LeftChild).Texture));
        Assert.Equal(new Color4(1.0f, 0.5f, 0.0f, 1.0f).ToSRgb(), Assert.IsType<ComputeColor>(product.RightChild).Value);

        Assert.Equal(CullMode.None, attributes.CullMode);
        Assert.Null(attributes.Transparency);
    }

    [Fact]
    public void BlendTakesAlphaFromBaseColorFactor()
    {
        var attributes = Material("Glass");

        var diffuse = Assert.IsType<MaterialDiffuseMapFeature>(attributes.Diffuse);
        var factor = Assert.IsType<ComputeColor>(diffuse.DiffuseMap);
        Assert.Equal(new Color4(0.8f, 0.9f, 1.0f, 0.2f).ToSRgb(), factor.Value);
        Assert.True(factor.PremultiplyAlpha);
        Assert.IsType<MaterialTransparencyBlendFeature>(attributes.Transparency);
        Assert.Equal(0.0f, Assert.IsType<ComputeFloat>(Assert.IsType<MaterialMetalnessMapFeature>(attributes.Specular).MetalnessMap).Value);
        Assert.Equal(0.05f, Assert.IsType<ComputeFloat>(Assert.IsType<MaterialGlossinessMapFeature>(attributes.MicroSurface).GlossinessMap).Value);
        Assert.Null(attributes.Emissive);
        Assert.Equal(CullMode.Back, attributes.CullMode);
    }

    [Fact]
    public void MaskUsesAlphaCutoff()
    {
        var attributes = Material("Cutout");

        var cutoff = Assert.IsType<MaterialTransparencyCutoffFeature>(attributes.Transparency);
        Assert.Equal(0.3f, Assert.IsType<ComputeFloat>(cutoff.Alpha).Value);
        var product = AssertBinary(Assert.IsType<MaterialDiffuseMapFeature>(attributes.Diffuse).DiffuseMap, BinaryOperator.Multiply);
        Assert.Equal("basecolor", TextureUrl(Assert.IsType<ComputeTextureColor>(product.LeftChild).Texture));
        var factor = Assert.IsType<ComputeColor>(product.RightChild);
        Assert.Equal(new Color4(1.0f, 1.0f, 1.0f, 0.9f).ToSRgb(), factor.Value);
        Assert.False(factor.PremultiplyAlpha);
    }

    [Fact]
    public void BlendedBaseColorTextureIsUsedDirectly()
    {
        var attributes = Material("TexturedGlass");

        Assert.Equal("glass", TextureUrl(Assert.IsType<ComputeTextureColor>(Assert.IsType<MaterialDiffuseMapFeature>(attributes.Diffuse).DiffuseMap).Texture));
        Assert.IsType<MaterialTransparencyBlendFeature>(attributes.Transparency);
    }

    [Fact]
    public void SharedBlendedImageGetsAPremultipliedCopy()
    {
        Assert.Equal("basecolor_premultiplied", TextureUrl(Assert.IsType<ComputeTextureColor>(Assert.IsType<MaterialDiffuseMapFeature>(Material("SharedGlass").Diffuse).DiffuseMap).Texture));
        var painted = Assert.IsType<MaterialDiffuseMapFeature>(Material("Painted").Diffuse).DiffuseMap;
        Assert.Equal("basecolor", TextureUrl(Assert.IsType<ComputeTextureColor>(AssertBinary(painted, BinaryOperator.Multiply).LeftChild).Texture));

        Assert.Equal(TextureRole.Color, Role("basecolor"));
        Assert.Equal("basecolor_premultiplied", Assert.Single(entity.PremultipliedTextures, x => x.Key == Path.Combine(directory, "basecolor.png")).Value);
        Assert.DoesNotContain(entity.PremultipliedTextures.Keys, path => path.EndsWith("glass.png"));
    }

    [Fact]
    public void PremultipliedCopyIsImportedNextToTheStraightTexture()
    {
        var assets = new List<AssetItem>();
        ModelAssetImporter.ImportTextures(entity.TextureDependencies, entity.TextureRoles, entity.PremultipliedTextures, assets, new LoggerResult());

        var straight = Assert.IsType<ColorTextureType>(Assert.IsType<TextureAsset>(Assert.Single(assets, x => x.Location.ToString() == "basecolor").Asset).Type);
        Assert.False(straight.PremultiplyAlpha);
        var copy = Assert.IsType<TextureAsset>(Assert.Single(assets, x => x.Location.ToString() == "basecolor_premultiplied").Asset);
        Assert.True(Assert.IsType<ColorTextureType>(copy.Type).PremultiplyAlpha);
        Assert.Equal(Path.Combine(directory, "basecolor.png"), copy.Source.ToOSPath());
    }

    [Fact]
    public void MaterialWithoutPropertiesUsesGltfDefaults()
    {
        var attributes = Material("Plain");

        Assert.Equal(Color4.White, Assert.IsType<ComputeColor>(Assert.IsType<MaterialDiffuseMapFeature>(attributes.Diffuse).DiffuseMap).Value);
        Assert.Equal(1.0f, Assert.IsType<ComputeFloat>(Assert.IsType<MaterialMetalnessMapFeature>(attributes.Specular).MetalnessMap).Value);
        Assert.Equal(1.0f, Assert.IsType<ComputeFloat>(Assert.IsType<MaterialGlossinessMapFeature>(attributes.MicroSurface).GlossinessMap).Value);
        Assert.Null(attributes.Transparency);
        Assert.Null(attributes.Occlusion);
        Assert.Null(attributes.Surface);
        Assert.Null(attributes.Emissive);
    }

    [Fact]
    public void TexturesGetTheRoleOfTheirMaterialSlot()
    {
        Assert.Equal(Images.Length, entity.TextureDependencies.Count);
        Assert.Equal(TextureRole.Color, Role("basecolor"));
        Assert.Equal(TextureRole.BlendedColor, Role("glass"));
        Assert.Equal(TextureRole.Data, Role("orm"));
        Assert.Equal(TextureRole.Normal, Role("normal"));
        Assert.Equal(TextureRole.Color, Role("emissive"));
    }

    [Fact]
    public void TextureRolesMapToTextureTypes()
    {
        var color = Assert.IsType<ColorTextureType>(ModelAssetImporter.GetTextureType(TextureRole.Color));
        Assert.True(color.UseSRgbSampling);
        Assert.False(color.PremultiplyAlpha);

        var blended = Assert.IsType<ColorTextureType>(ModelAssetImporter.GetTextureType(TextureRole.BlendedColor));
        Assert.True(blended.UseSRgbSampling);
        Assert.True(blended.PremultiplyAlpha);

        var data = Assert.IsType<ColorTextureType>(ModelAssetImporter.GetTextureType(TextureRole.Data));
        Assert.False(data.UseSRgbSampling);
        Assert.False(data.PremultiplyAlpha);

        Assert.True(Assert.IsType<NormapMapTextureType>(ModelAssetImporter.GetTextureType(TextureRole.Normal)).InvertY);
    }

    [Fact]
    public void EmbeddedImagesWithoutNamesGetTheirOwnFiles()
    {
        var path = Path.Combine(directory, "embedded.gltf");
        File.WriteAllText(path, JsonSerializer.Serialize(CreateGltf(embedImages: true)));

        var embedded = new MeshConverter(null).ExtractEntity(path, path, extractTextureDependencies: true, deduplicateMaterials: false);

        var expected = Enumerable.Range(0, Images.Length).Select(i => Path.Combine(directory, $"embedded_texture{i}.png")).ToList();
        Assert.Equal(expected.Order(), embedded.TextureDependencies.Order());
        Assert.All(expected, file => Assert.True(File.Exists(file), file));
        var metalness = Assert.IsType<MaterialMetalnessMapFeature>(embedded.Materials["Painted"].Attributes.Specular);
        var orm = TextureUrl(Assert.IsType<ComputeTextureScalar>(AssertBinary(metalness.MetalnessMap, BinaryOperator.Multiply).LeftChild).Texture);
        Assert.Equal(TextureRole.Data, embedded.TextureRoles[Path.Combine(directory, orm + ".png")]);
    }

    private MaterialAttributes Material(string name) => entity.Materials[name].Attributes;

    private TextureRole Role(string image) => entity.TextureRoles[Path.Combine(directory, image + ".png")];

    private static string TextureUrl(Texture texture) => AttachedReferenceManager.GetAttachedReference(texture).Url;

    private static ComputeBinaryBase<T> AssertBinary<T>(T node, BinaryOperator expected) where T : class, IComputeNode
    {
        var binary = Assert.IsAssignableFrom<ComputeBinaryBase<T>>(node);
        Assert.Equal(expected, binary.Operator);
        return binary;
    }

    private static void AssertScalarTexture(IComputeScalar node, string url, ColorChannel channel)
    {
        var texture = Assert.IsType<ComputeTextureScalar>(node);
        Assert.Equal(url, TextureUrl(texture.Texture));
        Assert.Equal(channel, texture.Channel);
    }

    private static object CreateGltf(bool embedImages = false)
    {
        var arrays = new[] { new float[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 }, new float[] { 0, 0, 1, 0, 0, 1 } };
        var bytes = MemoryMarshal.AsBytes(arrays.SelectMany(x => x).ToArray().AsSpan()).ToArray();

        return new
        {
            asset = new { version = "2.0" },
            scene = 0,
            scenes = new[] { new { nodes = new[] { 0 } } },
            nodes = new[] { new { mesh = 0 } },
            meshes = new[]
            {
                new
                {
                    primitives = Enumerable.Range(0, 7).Select(material => new { attributes = new { POSITION = 0, TEXCOORD_0 = 1 }, material, mode = 4 }).ToArray(),
                },
            },
            materials = new object[]
            {
                new
                {
                    name = "SharedImage",
                    pbrMetallicRoughness = new { baseColorTexture = new { index = 1 } },
                },
                new
                {
                    name = "Painted",
                    pbrMetallicRoughness = new
                    {
                        baseColorTexture = new { index = 0 },
                        baseColorFactor = new[] { 0.5f, 0.5f, 0.5f, 0.5f },
                        metallicRoughnessTexture = new { index = 1 },
                        metallicFactor = 0.8f,
                        roughnessFactor = 0.6f,
                    },
                    normalTexture = new { index = 2 },
                    occlusionTexture = new { index = 1, strength = 0.25f },
                    emissiveTexture = new { index = 3 },
                    emissiveFactor = new[] { 1.0f, 0.5f, 0.0f },
                    doubleSided = true,
                },
                new
                {
                    name = "Glass",
                    pbrMetallicRoughness = new { baseColorFactor = new[] { 0.8f, 0.9f, 1.0f, 0.2f }, metallicFactor = 0.0f, roughnessFactor = 0.05f },
                    alphaMode = "BLEND",
                },
                new
                {
                    name = "Cutout",
                    pbrMetallicRoughness = new { baseColorTexture = new { index = 0 }, baseColorFactor = new[] { 1.0f, 1.0f, 1.0f, 0.9f } },
                    alphaMode = "MASK",
                    alphaCutoff = 0.3f,
                },
                new
                {
                    name = "TexturedGlass",
                    pbrMetallicRoughness = new { baseColorTexture = new { index = 4 } },
                    alphaMode = "BLEND",
                },
                new { name = "Plain" },
                new
                {
                    name = "SharedGlass",
                    pbrMetallicRoughness = new { baseColorTexture = new { index = 0 } },
                    alphaMode = "BLEND",
                },
            },
            textures = Enumerable.Range(0, Images.Length).Select(source => new { source }).ToArray(),
            images = Images.Select(name => new { uri = embedImages ? "data:image/png;base64," + OnePixelPng : name + ".png" }).ToArray(),
            buffers = new[] { new { byteLength = bytes.Length, uri = "data:application/octet-stream;base64," + Convert.ToBase64String(bytes) } },
            bufferViews = new[]
            {
                new { buffer = 0, byteOffset = 0, byteLength = 36 },
                new { buffer = 0, byteOffset = 36, byteLength = 24 },
            },
            accessors = new object[]
            {
                new { bufferView = 0, componentType = 5126, count = 3, type = "VEC3", min = new[] { 0f, 0f, 0f }, max = new[] { 1f, 1f, 0f } },
                new { bufferView = 1, componentType = 5126, count = 3, type = "VEC2" },
            },
        };
    }
}
