// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
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
}
