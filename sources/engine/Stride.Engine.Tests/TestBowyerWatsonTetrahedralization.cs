// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Xunit;
using Stride.Core.Mathematics;
using Stride.Rendering.LightProbes;

namespace Stride.Engine.Tests
{
    public class TestBowyerWatsonTetrahedralization
    {
        [Fact]
        public void TestCube()
        {
            // Build cube from (0,0,0) to (1,1,1)
            var positions = new List<Vector3>();
            for (int i = 0; i < 8; ++i)
            {
                positions.Add(new Vector3
                {
                    X = i % 2 == 0 ? 0.0f : 1.0f,
                    Y = i / 4 == 0 ? 0.0f : 1.0f,
                    Z = (i / 2) % 2 == 0 ? 0.0f : 1.0f,
                });
            }

            AssertConnectivity(new BowyerWatsonTetrahedralization().Compute(positions));
        }

        [Theory]
        [InlineData(4)]
        [InlineData(12)]
        [InlineData(32)]
        public void TestRandomPoints(int count)
        {
            var random = new System.Random(1234);
            var positions = new List<Vector3>();
            for (var i = 0; i < count; i++)
                positions.Add(new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()));

            var tetra = new BowyerWatsonTetrahedralization();
            var first = tetra.Compute(positions);
            var second = tetra.Compute(positions);
            AssertConnectivity(first);
            AssertConnectivity(second);
            Assert.Equal(first.Tetrahedra.Count, second.Tetrahedra.Count);
            Assert.Equal(first.Faces.Count, second.Faces.Count);
        }

        private static unsafe void AssertConnectivity(BowyerWatsonTetrahedralization.Result result)
        {
            Assert.NotEmpty(result.Tetrahedra);
            for (var index = 0; index < result.Tetrahedra.Count; index++)
            {
                var tetrahedron = result.Tetrahedra[index];
                for (var side = 0; side < 4; side++)
                {
                    Assert.InRange(tetrahedron.Vertices[side], 0, result.Vertices.Length - 1);
                    var neighbourIndex = tetrahedron.Neighbours[side];
                    if (neighbourIndex == -1)
                        continue;

                    Assert.InRange(neighbourIndex, 0, result.Tetrahedra.Count - 1);
                    var faceIndex = tetrahedron.Faces[side];
                    var face = result.Faces[faceIndex < 0 ? ~faceIndex : faceIndex];
                    if (faceIndex < 0)
                    {
                        Assert.Equal(index, face.BackTetrahedron);
                        Assert.Equal(side, face.BackFace);
                        Assert.Equal(neighbourIndex, face.FrontTetrahedron);
                    }
                    else
                    {
                        Assert.Equal(index, face.FrontTetrahedron);
                        Assert.Equal(side, face.FrontFace);
                        Assert.Equal(neighbourIndex, face.BackTetrahedron);
                    }
                }
            }
        }
    }
}
