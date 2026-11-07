// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Stride.Core.Diagnostics;
using Stride.Graphics.Data;
using Stride.Importer.ThreeD;
using Xunit;

namespace Stride.Assets.Tests;

public class TestModelImport
{
    [Fact]
    public void LinesInAFileGiveNoMesh()
    {
        const string triangleAndLine = """
            v 0 0 0
            v 1 0 0
            v 0 1 0
            v 5 5 5
            v 6 6 6
            f 1 2 3
            l 4 5
            """;

        var path = Path.Combine(Path.GetTempPath(), $"stride-line-mesh-{Guid.NewGuid():N}.obj");
        try
        {
            File.WriteAllText(path, triangleAndLine);

            var mesh = Assert.Single(new MeshConverter(null).Convert(path, path, false).Meshes);

            Assert.Equal(3, mesh.Draw.DrawCount);
            var indices = mesh.Draw.IndexBuffer.Buffer.GetSerializationData().Content;
            for (var i = 0; i < mesh.Draw.DrawCount; i++)
                Assert.InRange(BitConverter.ToUInt16(indices, i * sizeof(ushort)), 0, mesh.Draw.VertexBuffers[0].Count - 1);
        }
        finally
        {
            File.Delete(path);
        }
    }

    public static TheoryData<string, byte[], string> UnreadableFiles => new()
    {
        { "text.fbx", Encoding.ASCII.GetBytes("This is not a model."), "FBX-Parser" },
        { "truncated.fbx", [.. Encoding.ASCII.GetBytes("Kaydara FBX Binary  \0\u001a\0"), .. BitConverter.GetBytes(7400), 0, 0, 0], "unexpected end of file" },
        { "missing-buffer.gltf", Encoding.ASCII.GetBytes(MissingBufferGltf), "missing.bin" },
        { "broken.dae", Encoding.ASCII.GetBytes("<?xml version=\"1.0\"?><COLLADA version=\"1.4.1\"><library_geometries>"), "malformed XML" },
    };

    // A triangle whose buffer file is not next to the .gltf
    private const string MissingBufferGltf = """
        {
            "asset": { "version": "2.0" },
            "scene": 0,
            "scenes": [ { "nodes": [ 0 ] } ],
            "nodes": [ { "mesh": 0 } ],
            "meshes": [ { "primitives": [ { "attributes": { "POSITION": 0 } } ] } ],
            "accessors": [ { "bufferView": 0, "componentType": 5126, "count": 3, "type": "VEC3", "min": [ 0, 0, 0 ], "max": [ 1, 1, 0 ] } ],
            "bufferViews": [ { "buffer": 0, "byteLength": 36 } ],
            "buffers": [ { "uri": "missing.bin", "byteLength": 36 } ]
        }
        """;

    [Theory]
    [MemberData(nameof(UnreadableFiles))]
    public void UnreadableFileGivesTheReasonOfAssimp(string fileName, byte[] content, string reason)
    {
        var folder = Directory.CreateTempSubdirectory("stride-unreadable-model-").FullName;
        try
        {
            var path = Path.Combine(folder, fileName);
            File.WriteAllBytes(path, content);

            foreach (var import in new Action[]
            {
                () => new MeshConverter(null).Convert(path, path, true, ["Material"]),
                () => new MeshConverter(null).ConvertAnimation(path, path, 0),
                () => new MeshConverter(null).ConvertSkeleton(path, path),
            })
            {
                var exception = Assert.Throws<InvalidOperationException>(import);
                Assert.Contains(path, exception.Message);
                Assert.Contains(reason, exception.Message);
            }

            var logger = new LoggerResult();
            Assert.Null(new MeshConverter(logger).ExtractEntity(path, null, false, true));
            var error = Assert.Single(logger.Messages, x => x.Type == LogMessageType.Error);
            Assert.Contains(path, error.Text);
            Assert.Contains(reason, error.Text);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
