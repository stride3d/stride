// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Stride.Assets.Models;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Importer.ThreeD;
using Xunit;

namespace Stride.Assets.Tests;

/// <summary>
/// Tests finding the files a model reads besides itself, and importing a model whose textures are missing.
/// </summary>
public sealed class TestModelTextureImport : IDisposable
{
    private const string OnePixelPng = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

    private readonly string directory = Path.Combine(Path.GetTempPath(), $"stride-model-files-{Guid.NewGuid():N}");

    public TestModelTextureImport()
    {
        Directory.CreateDirectory(directory);
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);

    [Fact]
    public void ObjFindsItsMaterialLibraryAndTextures()
    {
        WriteImage("textures/color.png");
        WriteImage("normal.png");
        File.WriteAllText(Path.Combine(directory, "model.mtl"), "newmtl Painted\nmap_Kd textures/color.png\nnorm normal.png\n");
        var model = Path.Combine(directory, "model.obj");
        File.WriteAllText(model, "mtllib model.mtl\nv 0 0 0\nv 1 0 0\nv 0 1 0\nvt 0 0\nvt 1 0\nvt 0 1\nusemtl Painted\nf 1/1 2/2 3/3\n");

        var files = new MeshConverter(null).FindSourceFiles(model);

        Assert.Contains((Path.Combine(directory, "model.mtl"), "model.mtl"), files);
        // The importer reads textures next to the model, wherever the model references them.
        Assert.Contains((Path.Combine(directory, "textures", "color.png"), "color.png"), files);
        Assert.Contains((Path.Combine(directory, "normal.png"), "normal.png"), files);
        Assert.DoesNotContain(files, file => file.Source == model);
    }

    [Fact]
    public void TextureNextToTheModelComesBeforeTheReferencedPath()
    {
        WriteImage("textures/color.png");
        WriteImage("color.png");
        File.WriteAllText(Path.Combine(directory, "model.mtl"), "newmtl Painted\nmap_Kd textures/color.png\n");
        var model = Path.Combine(directory, "model.obj");
        File.WriteAllText(model, "mtllib model.mtl\nv 0 0 0\nv 1 0 0\nv 0 1 0\nvt 0 0\nvt 1 0\nvt 0 1\nusemtl Painted\nf 1/1 2/2 3/3\n");

        var files = new MeshConverter(null).FindSourceFiles(model);

        Assert.Contains((Path.Combine(directory, "color.png"), "color.png"), files);
        Assert.DoesNotContain((Path.Combine(directory, "textures", "color.png"), "color.png"), files);
    }

    [Fact]
    public void GltfFindsItsBufferAndImagesWithoutWritingFiles()
    {
        WriteImage("images/color.png");
        var positions = new float[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 };
        File.WriteAllBytes(Path.Combine(directory, "geometry.bin"), positions.SelectMany(BitConverter.GetBytes).ToArray());
        var gltf = new
        {
            asset = new { version = "2.0" },
            scene = 0,
            scenes = new[] { new { nodes = new[] { 0 } } },
            nodes = new[] { new { mesh = 0 } },
            meshes = new[] { new { primitives = new[] { new { attributes = new { POSITION = 0 }, material = 0, mode = 4 } } } },
            materials = new object[]
            {
                new { pbrMetallicRoughness = new { baseColorTexture = new { index = 0 } } },
            },
            textures = new[] { new { source = 0 }, new { source = 1 } },
            images = new[] { new { uri = "images/color.png" }, new { uri = "data:image/png;base64," + OnePixelPng } },
            buffers = new[] { new { byteLength = positions.Length * 4, uri = "geometry.bin" } },
            bufferViews = new[] { new { buffer = 0, byteOffset = 0, byteLength = positions.Length * 4 } },
            accessors = new object[] { new { bufferView = 0, componentType = 5126, count = 3, type = "VEC3", min = new[] { 0f, 0f, 0f }, max = new[] { 1f, 1f, 0f } } },
        };
        var model = Path.Combine(directory, "model.gltf");
        File.WriteAllText(model, JsonSerializer.Serialize(gltf));
        var before = Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Order().ToList();

        var files = new MeshConverter(null).FindSourceFiles(model);

        Assert.Contains((Path.Combine(directory, "geometry.bin"), "geometry.bin"), files);
        Assert.Contains((Path.Combine(directory, "images", "color.png"), "color.png"), files);
        Assert.Equal(before, Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Order().ToList());
    }

    [Fact]
    public void MissingTextureIsAWarning()
    {
        var logger = new LoggerResult();
        var assets = new List<AssetItem>();

        ModelAssetImporter.ImportTextures([Path.Combine(directory, "missing.png")], null, null, assets, logger);

        Assert.Empty(assets);
        Assert.False(logger.HasErrors);
        Assert.Contains(logger.Messages, message => message.Type == LogMessageType.Warning && message.Text.Contains("missing.png"));
    }

    private void WriteImage(string relativePath)
    {
        var path = Path.Combine(directory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, Convert.FromBase64String(OnePixelPng));
    }
}
