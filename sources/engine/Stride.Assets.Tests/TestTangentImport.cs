// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Data;
using Stride.Importer.ThreeD;
using Xunit;

namespace Stride.Assets.Tests;

public class TestTangentImport
{
    [Fact]
    public void TangentsKeepTheirHandednessInW()
    {
        var vertices = ImportTwoTriangles();
        var middle = (vertices.Min(v => v.Position.X) + vertices.Max(v => v.Position.X)) / 2;

        Assert.Equal(6, vertices.Count);
        Assert.All(vertices, v => Assert.Equal(1.0f, new Vector3(v.Tangent.X, v.Tangent.Y, v.Tangent.Z).Length(), 3));
        // W follows Stride's own tangent generator (VertexHelper), so it matches normal maps imported with Invert Y.
        Assert.All(vertices.Where(v => v.Position.X < middle), v => Assert.Equal(-1.0f, v.Tangent.W));
        Assert.All(vertices.Where(v => v.Position.X > middle), v => Assert.Equal(1.0f, v.Tangent.W));
    }

    private static List<(Vector3 Position, Vector4 Tangent)> ImportTwoTriangles()
    {
        // Two triangles facing +Z with the image's up along +Y (glTF V points down the image), the second mirrored in U,
        // laid out like the Normal and U Mirror columns of Khronos' NormalTangentMirrorTest.
        var positions = new float[] { 0, 0, 0, 1, 0, 0, 0, 1, 0, 2, 0, 0, 3, 0, 0, 2, 1, 0 };
        var normals = Enumerable.Repeat(new float[] { 0, 0, 1 }, 6).SelectMany(x => x).ToArray();
        var uvs = new float[] { 0, 1, 1, 1, 0, 0, 1, 1, 0, 1, 1, 0 };
        var tangents = new float[] { 1, 0, 0, 1, 1, 0, 0, 1, 1, 0, 0, 1, -1, 0, 0, -1, -1, 0, 0, -1, -1, 0, 0, -1 };
        var arrays = new[] { positions, normals, uvs, tangents };
        var bytes = MemoryMarshal.AsBytes(arrays.SelectMany(x => x).ToArray().AsSpan()).ToArray();
        var offsets = arrays.Select((a, i) => arrays.Take(i).Sum(x => x.Length) * 4).ToArray();
        var gltf = new
        {
            asset = new { version = "2.0" },
            scene = 0,
            scenes = new[] { new { nodes = new[] { 0 } } },
            nodes = new[] { new { mesh = 0 } },
            meshes = new[] { new { primitives = new[] { new { attributes = new { POSITION = 0, NORMAL = 1, TEXCOORD_0 = 2, TANGENT = 3 }, mode = 4 } } } },
            buffers = new[] { new { byteLength = bytes.Length, uri = "data:application/octet-stream;base64," + Convert.ToBase64String(bytes) } },
            bufferViews = arrays.Select((a, i) => new { buffer = 0, byteOffset = offsets[i], byteLength = a.Length * 4 }).ToArray(),
            accessors = new object[]
            {
                new { bufferView = 0, componentType = 5126, count = 6, type = "VEC3", min = new[] { 0f, 0f, 0f }, max = new[] { 3f, 1f, 0f } },
                new { bufferView = 1, componentType = 5126, count = 6, type = "VEC3" },
                new { bufferView = 2, componentType = 5126, count = 6, type = "VEC2" },
                new { bufferView = 3, componentType = 5126, count = 6, type = "VEC4" },
            },
        };

        var path = Path.Combine(Path.GetTempPath(), $"stride-tangents-{Guid.NewGuid():N}.gltf");
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(gltf));
            var converter = new MeshConverter(null);
            var binding = Assert.Single(converter.Convert(path, path, false).Meshes).Draw.VertexBuffers[0];

            var elements = binding.Declaration.EnumerateWithOffsets().ToList();
            Assert.DoesNotContain(elements, e => e.VertexElement.SemanticName == "BITANGENT");
            var tangent = Assert.Single(elements, e => e.VertexElement.SemanticName == "TANGENT");
            Assert.Equal(PixelFormat.R32G32B32A32_Float, tangent.VertexElement.Format);
            var position = Assert.Single(elements, e => e.VertexElement.SemanticName == "POSITION");

            var data = binding.Buffer.GetSerializationData().Content;
            return Enumerable.Range(0, binding.Count).Select(v => binding.Offset + v * binding.Declaration.VertexStride)
                .Select(start => (Position: MemoryMarshal.Read<Vector3>(data.AsSpan(start + position.Offset)), Tangent: MemoryMarshal.Read<Vector4>(data.AsSpan(start + tangent.Offset))))
                .ToList();
        }
        finally { File.Delete(path); }
    }
}
