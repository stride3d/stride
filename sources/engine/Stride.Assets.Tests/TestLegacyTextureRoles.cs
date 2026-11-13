// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using Stride.Assets.Models;
using Stride.Assets.Textures;
using Stride.Importer.Common;
using Stride.Importer.ThreeD;
using Xunit;

namespace Stride.Assets.Tests;

/// <summary>
/// Tests the texture roles of formats other than glTF, whose materials go through the legacy builder.
/// </summary>
public sealed class TestLegacyTextureRoles : IDisposable
{
    private const string OnePixelPng = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

    private readonly string directory = Path.Combine(Path.GetTempPath(), $"stride-legacy-roles-{Guid.NewGuid():N}");

    public TestLegacyTextureRoles()
    {
        Directory.CreateDirectory(directory);
        foreach (var image in new[] { "color", "normal" })
            File.WriteAllBytes(Path.Combine(directory, image + ".png"), Convert.FromBase64String(OnePixelPng));
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);

    [Fact]
    public void NormalMapsAreImportedAsNormalMaps()
    {
        var entity = Import("""
            newmtl Painted
            map_Kd color.png
            norm normal.png
            """);

        Assert.Equal(TextureRole.Normal, entity.TextureRoles[Path.Combine(directory, "normal.png")]);
        Assert.False(entity.TextureRoles.ContainsKey(Path.Combine(directory, "color.png")));
        Assert.IsType<NormapMapTextureType>(ModelAssetImporter.GetTextureType(entity.TextureRoles[Path.Combine(directory, "normal.png")]));
    }

    [Fact]
    public void AnImageUsedAsNormalMapAndColorIsImportedAsNormalMap()
    {
        var entity = Import("""
            newmtl Painted
            map_Kd color.png
            newmtl Bumpy
            map_Kd normal.png
            norm normal.png
            """);

        Assert.Equal(TextureRole.Normal, entity.TextureRoles[Path.Combine(directory, "normal.png")]);
    }

    private EntityInfo Import(string materials)
    {
        File.WriteAllText(Path.Combine(directory, "model.mtl"), materials);
        var obj = Path.Combine(directory, "model.obj");
        File.WriteAllText(obj, """
            mtllib model.mtl
            v 0 0 0
            v 1 0 0
            v 0 1 0
            vt 0 0
            vt 1 0
            vt 0 1
            vn 0 0 1
            usemtl Painted
            f 1/1/1 2/2/1 3/3/1
            usemtl Bumpy
            f 1/1/1 3/3/1 2/2/1
            """);
        return new MeshConverter(null).ExtractEntity(obj, obj, extractTextureDependencies: true, deduplicateMaterials: false);
    }
}
