// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Definitions.Colliders.Voxels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics.Regression;
using Xunit;
using BitOperations = System.Numerics.BitOperations;
using NVector3 = System.Numerics.Vector3;

namespace Stride.BepuPhysics.Tests
{
    public class VoxelColliderTests : GameTestBase
    {
        private const float IsoLevel = 0.5f;
        private const float Tolerance = 0.05f;
        private const float DegenerateArea = 1e-10f;

        /// <summary>A floor filling the samples up to <paramref name="topSample"/>, with a surface half a cell above it.</summary>
        private static float[] Floor(int samples, int topSample)
        {
            var field = new float[samples * samples * samples];
            for (int x = 0; x < samples; ++x)
            {
                for (int y = 0; y <= topSample; ++y)
                {
                    for (int z = 0; z < samples; ++z)
                        field[(x * samples + y) * samples + z] = 1f;
                }
            }
            return field;
        }

        /// <summary>A ball of radius <paramref name="radius"/> around the centre of the grid, as a distance-based density.</summary>
        private static float[] Ball(int samples, float radius)
        {
            var centre = new NVector3((samples - 1) * 0.5f);
            var field = new float[samples * samples * samples];
            for (int x = 0; x < samples; ++x)
            {
                for (int y = 0; y < samples; ++y)
                {
                    for (int z = 0; z < samples; ++z)
                        field[(x * samples + y) * samples + z] = IsoLevel + radius - NVector3.Distance(new NVector3(x, y, z), centre);
                }
            }
            return field;
        }

        private static unsafe List<Triangle> Triangles(float[] field, int samples, bool surfaceNets, bool invertWinding)
        {
            var triangles = new List<Triangle>();
            fixed (float* data = field)
            {
                var shape = new VoxelTriangleShape<FloatVoxelSource>
                {
                    SurfaceNets = surfaceNets,
                    GridData = new VoxelGridData<FloatVoxelSource>
                    {
                        Source = new FloatVoxelSource { Samples = new Buffer<float>(data, field.Length), SamplesX = samples, SamplesY = samples, SamplesZ = samples },
                        CellSize = NVector3.One,
                        IsoLevel = IsoLevel,
                        InvertWinding = invertWinding,
                        SealBorder = true,
                    },
                };
                for (int x = 0; x < samples - 1; ++x)
                {
                    for (int y = 0; y < samples - 1; ++y)
                    {
                        for (int z = 0; z < samples - 1; ++z)
                        {
                            for (int slot = 0; slot < VoxelTriangleShape<FloatVoxelSource>.SlotsPerCell; ++slot)
                            {
                                if (shape.TryGetTriangle(x, y, z, slot, out var triangle))
                                    triangles.Add(triangle);
                            }
                        }
                    }
                }
            }
            return triangles;
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public static void TrianglesCollideFromTheAir(bool surfaceNets, bool invertWinding)
        {
            const int samples = 11;
            var centre = new NVector3((samples - 1) * 0.5f);
            var triangles = Triangles(Ball(samples, 3f), samples, surfaceNets, invertWinding);

            Assert.NotEmpty(triangles);
            foreach (var triangle in triangles)
            {
                // Bepu triangles collide on the side cross(C - A, B - A) points to.
                var facing = NVector3.Cross(triangle.C - triangle.A, triangle.B - triangle.A);
                // Two crossings can land on the same point; such a triangle has no side and collides with nothing.
                if (facing.LengthSquared() < DegenerateArea)
                    continue;
                var outward = (triangle.A + triangle.B + triangle.C) / 3f - centre;
                Assert.Equal(!invertWinding, NVector3.Dot(facing, outward) > 0);
            }
        }

        /// <summary>Every marching cubes triangle collides from the air, in the cells the surface crosses in several sheets facing different ways too.</summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static unsafe void MarchingCubesTrianglesFaceTheAirInEveryCase(bool invertWinding)
        {
            const int caseCount = 1 << VoxelCell.CornerCount;
            const int stride = VoxelGridData<FloatVoxelSource>.TableStride;
            var field = new float[VoxelCell.CornerCount];
            fixed (float* data = field)
            {
                var shape = new VoxelTriangleShape<FloatVoxelSource>
                {
                    GridData = new VoxelGridData<FloatVoxelSource>
                    {
                        Source = new FloatVoxelSource { Samples = new Buffer<float>(data, field.Length), SamplesX = 2, SamplesY = 2, SamplesZ = 2 },
                        CellSize = NVector3.One,
                        IsoLevel = IsoLevel,
                        InvertWinding = invertWinding,
                    },
                };

                for (int cubeIndex = 1; cubeIndex < caseCount - 1; ++cubeIndex)
                {
                    // One cell whose corners are solid or air as the case says
                    for (int corner = 0; corner < VoxelCell.CornerCount; ++corner)
                    {
                        var at = Corner(corner);
                        field[((int)at.X * 2 + (int)at.Y) * 2 + (int)at.Z] = ((cubeIndex >> corner) & 1) != 0 ? 0f : 1f;
                    }
                    Assert.Equal(cubeIndex, shape.GridData.CubeIndex(0, 0, 0));

                    for (int slot = 0; shape.TryGetTriangle(0, 0, 0, slot, out var triangle); ++slot)
                    {
                        // Each vertex is on an edge going from a solid corner to an air one
                        var towardsAir = NVector3.Zero;
                        for (int k = 0; k < 3; ++k)
                        {
                            int edge = VoxelGridData<FloatVoxelSource>.TriangleTable[cubeIndex * stride + slot * 3 + k];
                            int a = VoxelGridData<FloatVoxelSource>.EdgeCorners[edge * 2], b = VoxelGridData<FloatVoxelSource>.EdgeCorners[edge * 2 + 1];
                            towardsAir += ((cubeIndex >> a) & 1) != 0 ? Corner(a) - Corner(b) : Corner(b) - Corner(a);
                        }
                        // Bepu triangles collide on the side cross(C - A, B - A) points to.
                        var facing = NVector3.Cross(triangle.C - triangle.A, triangle.B - triangle.A);
                        Assert.True(NVector3.Dot(facing, towardsAir) > 0 != invertWinding, $"Case {cubeIndex}: triangle {slot} faces {facing}, the air is towards {towardsAir}");
                    }
                }
            }

            static NVector3 Corner(int corner)
            {
                var corners = VoxelGridData<FloatVoxelSource>.CubeCorners;
                return new NVector3(corners[corner * 3], corners[corner * 3 + 1], corners[corner * 3 + 2]);
            }
        }

        /// <summary>A cell the marching cubes surface crosses in two sheets: a ray stops on the sheet closing the pocket of air it starts in.</summary>
        [Fact]
        public static void RaysStopOnEachSheetOfAMarchingCubesCell()
        {
            RunScript(game =>
            {
                // Solid at (0, 0, 0), (1, 1, 0), (1, 1, 1) and (0, 1, 1): the corner (0, 1, 0) is a pocket of air between three of them.
                var field = new float[8];
                field[(0 * 2 + 0) * 2 + 0] = 1f;
                field[(1 * 2 + 1) * 2 + 0] = 1f;
                field[(1 * 2 + 1) * 2 + 1] = 1f;
                field[(0 * 2 + 1) * 2 + 1] = 1f;
                var collider = new VoxelCollider { CellShape = VoxelCellShape.MarchingCubes, IsoLevel = IsoLevel, SealBorder = false };
                collider.SetData(2, 2, 2, field);
                var terrain = new Entity { new StaticComponent { Collider = collider } };
                game.SceneSystem.SceneInstance.RootScene.Entities.Add(terrain);

                // Out of the pocket, through the triangle on the three edges leaving its corner: the plane x - y + z = -0.5
                var direction = Vector3.Normalize(new Vector3(1f, -1f, 1f));
                Assert.True(terrain.GetSimulation().RayCast(new Vector3(0.05f, 0.95f, 0.05f), direction, 1.5f, out var hit));
                Assert.Equal(0.35f / MathF.Sqrt(3f), hit.Distance, Tolerance);
                return Task.CompletedTask;
            });
        }

        [Fact]
        public static void SetDataRebuildsWhenOnlyTheShapeOfTheGridChanges()
        {
            RunScript(game =>
            {
                // 24 solid samples as 2 x 1 x 3 cells, then as 3 x 2 x 1: the same memory, a grid one cell longer and one higher
                var field = new float[24];
                Array.Fill(field, 1f);
                var collider = new VoxelCollider { CellShape = VoxelCellShape.Box, IsoLevel = IsoLevel, SealBorder = false };
                collider.SetData(3, 2, 4, field);
                var terrain = new Entity { new StaticComponent { Collider = collider } };
                game.SceneSystem.SceneInstance.RootScene.Entities.Add(terrain);
                var simulation = terrain.GetSimulation();
                Assert.True(simulation.RayCast(new Vector3(0.5f, 10f, 0.5f), -Vector3.UnitY, 20f, out var hit));
                Assert.Equal(1f, hit.Point.Y, Tolerance);
                Assert.False(simulation.RayCast(new Vector3(2.5f, 10f, 0.5f), -Vector3.UnitY, 20f, out _));

                field[(1 * 3 + 2) * 2 + 1] = 7f;
                collider.SetData(4, 3, 2, field);

                Assert.Equal(new Int3(4, 3, 2), collider.SampleCount);
                Assert.Equal(7f, collider.GetVoxel(1, 2, 1));
                Assert.True(simulation.RayCast(new Vector3(0.5f, 10f, 0.5f), -Vector3.UnitY, 20f, out hit));
                Assert.Equal(2f, hit.Point.Y, Tolerance);
                Assert.True(simulation.RayCast(new Vector3(2.5f, 10f, 0.5f), -Vector3.UnitY, 20f, out hit));
                Assert.Equal(2f, hit.Point.Y, Tolerance);
                return Task.CompletedTask;
            });
        }

        /// <summary>A ray going through the surface reports each child it crosses once, whichever cells around the ray test it.</summary>
        [Theory]
        [InlineData(VoxelCellShape.Box, 1f)]
        [InlineData(VoxelCellShape.Sphere, 1.4f)]
        [InlineData(VoxelCellShape.MarchingCubes, 1f)]
        [InlineData(VoxelCellShape.SurfaceNets, 1f)]
        public static void PenetratingRaysReportEachChildOnce(VoxelCellShape cellShape, float sphereRadiusScale)
        {
            RunScript(game =>
            {
                const int samples = 10;
                var collider = new VoxelCollider { CellShape = cellShape, IsoLevel = IsoLevel, SphereRadiusScale = sphereRadiusScale };
                collider.SetData(samples, samples, samples, Floor(samples, 4));
                var terrain = new Entity { new StaticComponent { Collider = collider } };
                game.SceneSystem.SceneInstance.RootScene.Entities.Add(terrain);

                // Slanted, so it changes cell on every axis on its way through the floor
                var hits = new List<HitInfo>();
                terrain.GetSimulation().RayCastPenetrating(new Vector3(3.3f, 9f, 4.6f), Vector3.Normalize(new Vector3(0.3f, -1f, 0.2f)), 20f, hits);

                Assert.NotEmpty(hits);
                var children = new HashSet<int>();
                Assert.All(hits, hit => Assert.True(children.Add(hit.ChildIndex), $"Child {hit.ChildIndex} is reported twice, at {hit.Point}"));
                // The triangles only collide from above, the ray meets the surface once
                if (cellShape is VoxelCellShape.MarchingCubes or VoxelCellShape.SurfaceNets)
                    Assert.Single(hits);
                return Task.CompletedTask;
            });
        }

        [Fact]
        public static void SetDataRejectsBadGrids()
        {
            using var collider = new VoxelCollider();
            Assert.Throws<ArgumentOutOfRangeException>(() => collider.SetData(1, 2, 2, new float[4]));
            Assert.Throws<ArgumentException>(() => collider.SetData(2, 2, 2, new float[7]));
            Assert.Throws<InvalidOperationException>(() => collider.GetVoxel(0, 0, 0));
        }

        [Fact]
        public static void SetDataRejectsGridsBeyondTheIndexSpace()
        {
            // 2000 * 2000 * 600 = 2.4 billion samples, past the int index space; rejected before allocating anything.
            using var collider = new VoxelCollider();
            Assert.Throws<ArgumentException>(() => collider.SetData(2000, 2000, 600, ReadOnlySpan<float>.Empty));
            Assert.False(collider.HasData);
        }

        [Fact]
        public static void GridsWithMoreChildrenThanTheIndexSpaceAreRejected()
        {
            // 1000 x 1000 x 400 samples fit an int, six triangle slots for each of their cells do not; rejected before allocating anything.
            using var collider = new VoxelCollider { CellShape = VoxelCellShape.SurfaceNets };
            var rejected = Assert.Throws<ArgumentException>(() => collider.SetData(1000, 1000, 400, ReadOnlySpan<float>.Empty));
            Assert.Equal("samplesX", rejected.ParamName);
            Assert.False(collider.HasData);

            // One box per cell fits; such a grid cannot switch to triangles, and keeps its cell shape.
            var boxes = new PackedVoxelCollider([], 1000) { CellShape = VoxelCellShape.Box };
            Assert.Throws<ArgumentException>(() => boxes.CellShape = VoxelCellShape.MarchingCubes);
            Assert.Equal(VoxelCellShape.Box, boxes.CellShape);
        }

        [Fact]
        public static void InvalidSettingsThrow()
        {
            using var collider = new VoxelCollider { CellSize = 0.5f, SphereRadiusScale = 1.4f, Mass = 3f };
            foreach (var invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => collider.CellSize = invalid);
                Assert.Throws<ArgumentOutOfRangeException>(() => collider.SphereRadiusScale = invalid);
                Assert.Throws<ArgumentOutOfRangeException>(() => collider.Mass = invalid);
            }
            // Any finite iso level is one
            foreach (var invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                Assert.Throws<ArgumentOutOfRangeException>(() => collider.IsoLevel = invalid);
            collider.IsoLevel = -2f;
            Assert.Equal(-2f, collider.IsoLevel);
            Assert.Equal(0.5f, collider.CellSize);
            Assert.Equal(1.4f, collider.SphereRadiusScale);
            Assert.Equal(3f, collider.Mass);
        }

        private struct ChildCounter : BepuUtilities.IBreakableForEach<int>
        {
            public int Count;

            public bool LoopBody(int childIndex)
            {
                ++Count;
                return true;
            }
        }

        [Fact]
        public static unsafe void SphereOverlapQueriesReachTheOvershootAndNoFurther()
        {
            // One solid cell of size 1 whose sphere, of radius 0.7, sticks 0.2 out of it on every side.
            var field = new float[8];
            Array.Fill(field, 1f);
            fixed (float* data = field)
            {
                var shape = new VoxelSphereShape<FloatVoxelSource>(new VoxelGridData<FloatVoxelSource>
                {
                    Source = new FloatVoxelSource { Samples = new Buffer<float>(data, field.Length), SamplesX = 2, SamplesY = 2, SamplesZ = 2 },
                    CellSize = NVector3.One,
                    IsoLevel = IsoLevel,
                    SphereRadius = 0.7f,
                });

                var inTheOvershoot = new ChildCounter();
                VoxelShapeHelpers.EnumerateOverlaps(ref shape, new NVector3(0.4f, 1.15f, 0.4f), new NVector3(0.6f, 1.3f, 0.6f), ref inTheOvershoot);
                Assert.Equal(1, inTheOvershoot.Count);

                var aboveIt = new ChildCounter();
                VoxelShapeHelpers.EnumerateOverlaps(ref shape, new NVector3(0.4f, 1.3f, 0.4f), new NVector3(0.6f, 1.4f, 0.6f), ref aboveIt);
                Assert.Equal(0, aboveIt.Count);
            }
        }

        /// <summary>Marching cubes triangles stay in their cell: a query only gets the ones of the cells it touches, surface nets ones those of the cells around too.</summary>
        [Theory]
        [InlineData(false, 2)]
        [InlineData(true, 2 * 3 * 3)]
        public static unsafe void TriangleQueriesReachAsFarAsTheTrianglesDo(bool surfaceNets, int expected)
        {
            const int samples = 10;
            var field = Floor(samples, 4);
            fixed (float* data = field)
            {
                var shape = new VoxelTriangleShape<FloatVoxelSource>
                {
                    SurfaceNets = surfaceNets,
                    GridData = new VoxelGridData<FloatVoxelSource>
                    {
                        Source = new FloatVoxelSource { Samples = new Buffer<float>(data, field.Length), SamplesX = samples, SamplesY = samples, SamplesZ = samples },
                        CellSize = NVector3.One,
                        IsoLevel = IsoLevel,
                        SealBorder = true,
                    },
                };

                // Inside the cell (4, 4, 4), which the flat surface crosses at y = 4.5 in two triangles
                var inOneCell = new ChildCounter();
                VoxelShapeHelpers.EnumerateOverlaps(ref shape, new NVector3(4.2f, 4.3f, 4.2f), new NVector3(4.8f, 4.7f, 4.8f), ref inOneCell);
                Assert.Equal(expected, inOneCell.Count);
            }
        }

        [Fact]
        public static void AColliderWithoutFieldHasNothingToDraw()
        {
            // Debug views and the editor gizmo expect one model per transform
            using var collider = new VoxelCollider();
            ICollider shape = collider;
            var models = new List<BasicMeshBuffers>();
            shape.AppendModel(models, null!, out _);
            Assert.Equal(0, shape.Transforms);
            Assert.Empty(models);

            collider.SetData(2, 2, 2, [1f, 0f, 1f, 0f, 1f, 0f, 1f, 0f]);
            shape.AppendModel(models, null!, out _);
            Assert.Equal(1, shape.Transforms);
            Assert.Single(models);
        }

        [Fact]
        public static void CornerAndEdgeTablesFollowTheCellLayout()
        {
            // Corners go round the bottom face, then round the top one above them
            var corners = VoxelGridData<FloatVoxelSource>.CubeCorners;
            Assert.Equal(VoxelCell.CornerCount * 3, corners.Length);
            for (int corner = 0; corner < VoxelCell.CornerCount; ++corner)
            {
                Assert.Equal(((corner + 1) >> 1) & 1, corners[corner * 3]);
                Assert.Equal(corner >> 2, corners[corner * 3 + 1]);
                Assert.Equal((corner >> 1) & 1, corners[corner * 3 + 2]);
            }

            // Edges follow the bottom ring, the top ring, then join each bottom corner to the one above
            var edges = VoxelGridData<FloatVoxelSource>.EdgeCorners;
            Assert.Equal(VoxelCell.EdgeCount * 2, edges.Length);
            for (int i = 0; i < 4; ++i)
            {
                Assert.Equal((i, (i + 1) % 4), ((int)edges[i * 2], (int)edges[i * 2 + 1]));
                Assert.Equal((4 + i, 4 + (i + 1) % 4), ((int)edges[8 + i * 2], (int)edges[8 + i * 2 + 1]));
                Assert.Equal((i, i + 4), ((int)edges[16 + i * 2], (int)edges[16 + i * 2 + 1]));
            }
        }

        [Fact]
        public static void TriangleTableClosesTheSurfaceOfEveryCase()
        {
            const int caseCount = 1 << VoxelCell.CornerCount;
            const int stride = VoxelGridData<FloatVoxelSource>.TableStride;
            var corners = VoxelGridData<FloatVoxelSource>.CubeCorners;
            var edges = VoxelGridData<FloatVoxelSource>.EdgeCorners;
            var table = VoxelGridData<FloatVoxelSource>.TriangleTable;
            Assert.Equal(caseCount * stride, table.Length);

            // The two faces of the cell each edge lies on, bit axis * 2 + side
            var edgeFaces = new int[VoxelCell.EdgeCount];
            for (int edge = 0; edge < VoxelCell.EdgeCount; ++edge)
            {
                for (int axis = 0; axis < 3; ++axis)
                {
                    var side = corners[edges[edge * 2] * 3 + axis];
                    if (corners[edges[edge * 2 + 1] * 3 + axis] == side)
                        edgeFaces[edge] |= 1 << (axis * 2 + side);
                }
            }

            for (int cubeIndex = 0; cubeIndex < caseCount; ++cubeIndex)
            {
                var entries = table.Slice(cubeIndex * stride, stride);
                var count = entries.IndexOf((sbyte)-1);
                Assert.True(count >= 0 && count % 3 == 0 && count / 3 <= VoxelCell.MaxMarchingCubesTrianglesPerCell, $"Case {cubeIndex} has {count} entries");
                Assert.True(entries[count..].IndexOfAnyExcept((sbyte)-1) < 0, $"Case {cubeIndex} has entries after its end");
                Assert.Equal(count / 3, VoxelGridData<FloatVoxelSource>.MarchingCubesTriangleCount(cubeIndex));

                // Vertices sit on exactly the edges whose corners are on opposite sides of the iso level
                int crossedEdges = 0, usedEdges = 0;
                for (int edge = 0; edge < VoxelCell.EdgeCount; ++edge)
                {
                    if ((((cubeIndex >> edges[edge * 2]) ^ (cubeIndex >> edges[edge * 2 + 1])) & 1) != 0)
                        crossedEdges |= 1 << edge;
                }
                for (int i = 0; i < count; ++i)
                    usedEdges |= 1 << entries[i];
                Assert.Equal(crossedEdges, usedEdges);

                // A triangle side is shared by two triangles, or is the one boundary segment through each of its two crossings on a face of the cell
                var sides = new Dictionary<(int, int), int>();
                for (int i = 0; i < count; i += 3)
                {
                    for (int k = 0; k < 3; ++k)
                    {
                        int a = entries[i + k], b = entries[i + (k + 1) % 3];
                        var side = (Math.Min(a, b), Math.Max(a, b));
                        sides[side] = sides.GetValueOrDefault(side) + 1;
                    }
                }
                var boundaryEnds = new int[6, VoxelCell.EdgeCount];
                foreach (var ((a, b), uses) in sides)
                {
                    Assert.True(uses <= 2, $"Case {cubeIndex}: side {a}-{b} is shared by {uses} triangles");
                    if (uses == 2)
                        continue;
                    var sharedFaces = edgeFaces[a] & edgeFaces[b];
                    Assert.True(sharedFaces != 0, $"Case {cubeIndex}: open side {a}-{b} crosses the inside of the cell");
                    var face = BitOperations.TrailingZeroCount(sharedFaces);
                    ++boundaryEnds[face, a];
                    ++boundaryEnds[face, b];
                }
                for (int face = 0; face < 6; ++face)
                {
                    for (int edge = 0; edge < VoxelCell.EdgeCount; ++edge)
                    {
                        var expected = (edgeFaces[edge] & (1 << face)) != 0 && (crossedEdges & (1 << edge)) != 0 ? 1 : 0;
                        Assert.True(expected == boundaryEnds[face, edge], $"Case {cubeIndex}: edge {edge} ends {boundaryEnds[face, edge]} boundary segments on face {face}, expected {expected}");
                    }
                }
            }
        }

        [Theory]
        [InlineData(VoxelCellShape.Box, 5f)]
        [InlineData(VoxelCellShape.Sphere, 5f)]
        [InlineData(VoxelCellShape.MarchingCubes, 4.5f)]
        [InlineData(VoxelCellShape.SurfaceNets, 4.5f)]
        public static void RayHitsTheSurfaceAndSeesEdits(VoxelCellShape cellShape, float surface)
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                const int samples = 10;
                var collider = new VoxelCollider { CellShape = cellShape, IsoLevel = IsoLevel };
                collider.SetData(samples, samples, samples, Floor(samples, 4));
                var terrain = new Entity { new StaticComponent { Collider = collider } };
                game.SceneSystem.SceneInstance.RootScene.Entities.Add(terrain);
                var simulation = terrain.GetSimulation();

                // A cell centre, where the sphere children are at their top.
                var origin = new Vector3(4.5f, 20f, 4.5f);
                Assert.True(simulation.RayCast(origin, -Vector3.UnitY, 40f, out var hit));
                Assert.Equal(surface, hit.Point.Y, Tolerance);
                Assert.True(MathUtil.WithinEpsilon(hit.Normal.Y, 1f, 1e-3f), $"Normal {hit.Normal}");

                // Carve a pit two samples deep; the simulation reads the samples directly.
                for (int x = 3; x <= 6; ++x)
                {
                    for (int z = 3; z <= 6; ++z)
                    {
                        collider.SetVoxel(x, 4, z, 0f);
                        collider.SetVoxel(x, 3, z, 0f);
                    }
                }
                Assert.True(simulation.RayCast(origin, -Vector3.UnitY, 40f, out var pit));
                Assert.Equal(surface - 2f, pit.Point.Y, Tolerance);

                game.Exit();
            });
            RunGameTest(game);
        }

        [Theory]
        [InlineData(VoxelCellShape.Box, 5f, 0.1f)]
        [InlineData(VoxelCellShape.Sphere, 5f, 0.3f)] // Bodies can settle between the tangent spheres.
        [InlineData(VoxelCellShape.MarchingCubes, 4.5f, 0.1f)]
        [InlineData(VoxelCellShape.SurfaceNets, 4.5f, 0.1f)]
        public static void BodiesRestOnTheSurface(VoxelCellShape cellShape, float surface, float tolerance)
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                const int samples = 10;
                var collider = new VoxelCollider { CellShape = cellShape, IsoLevel = IsoLevel };
                collider.SetData(samples, samples, samples, Floor(samples, 4));
                var terrain = new Entity { new StaticComponent { Collider = collider } };
                var sphere = new Entity { new BodyComponent { Collider = new CompoundCollider { Colliders = { new SphereCollider { Radius = 0.5f } } } } };
                var box = new Entity { new BodyComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider { Size = new Vector3(1f) } } } } };
                sphere.Transform.Position = new Vector3(4.5f, 8f, 4.5f);
                box.Transform.Position = new Vector3(2.5f, 8f, 6.5f);
                game.SceneSystem.SceneInstance.RootScene.Entities.AddRange(new[] { terrain, sphere, box });

                var simulation = terrain.GetSimulation();
                while (game.UpdateTime.Total.TotalSeconds < 3d)
                    await simulation.AfterUpdate();

                Assert.Equal(surface + 0.5f, sphere.Transform.Position.Y, tolerance);
                Assert.Equal(surface + 0.5f, box.Transform.Position.Y, tolerance);

                game.Exit();
            });
            RunGameTest(game);
        }

        /// <summary>Runs <paramref name="script"/> in a game, then throws what it threw: thrown past an await, an exception leaves the game running.</summary>
        private static void RunScript(Func<GameTest, Task> script)
        {
            var game = new GameTest();
            Exception? failure = null;
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;
                try
                {
                    await script(game);
                }
                catch (Exception e)
                {
                    failure = e;
                }
                game.Exit();
            });
            RunGameTest(game);
            if (failure is not null)
                ExceptionDispatchInfo.Throw(failure);
        }

        /// <summary>Overlap and sweep queries see the cells, with query shapes of types no collider of the simulation has.</summary>
        [Theory]
        [InlineData(VoxelCellShape.Box, 5f)]
        [InlineData(VoxelCellShape.Sphere, 5f)]
        [InlineData(VoxelCellShape.MarchingCubes, 4.5f)]
        [InlineData(VoxelCellShape.SurfaceNets, 4.5f)]
        public static void OverlapAndSweepQueriesSeeTheCells(VoxelCellShape cellShape, float surface)
        {
            RunScript(game =>
            {
                const int samples = 10;
                var collider = new VoxelCollider { CellShape = cellShape, IsoLevel = IsoLevel };
                collider.SetData(samples, samples, samples, Floor(samples, 4));
                var terrain = new Entity { new StaticComponent { Collider = collider } };
                game.SceneSystem.SceneInstance.RootScene.Entities.Add(terrain);
                var simulation = terrain.GetSimulation();
                var floor = terrain.Get<StaticComponent>();

                // Above the middle of a cell, where the sphere children are at their top: sunk a quarter into the surface, then clear of it.
                var sunk = new RigidPose(new Vector3(4.5f, surface + 0.25f, 4.5f), Quaternion.Identity);
                var clear = new RigidPose(new Vector3(4.5f, surface + 1f, 4.5f), Quaternion.Identity);
                var overlaps = new List<OverlapInfo>();
                simulation.Overlap(new Sphere(0.5f), sunk, overlaps);
                Assert.NotEmpty(overlaps);
                int afterTheSphere = overlaps.Count;
                simulation.Overlap(new Box(1f, 1f, 1f), sunk, overlaps);
                Assert.True(overlaps.Count > afterTheSphere, "The box overlaps nothing");
                int afterTheBox = overlaps.Count;
                simulation.Overlap(new Capsule(0.5f, 0.5f), sunk, overlaps);
                Assert.True(overlaps.Count > afterTheBox, "The capsule overlaps nothing");
                Assert.All(overlaps, overlap => Assert.Same(floor, overlap.Collidable));

                overlaps.Clear();
                simulation.Overlap(new Sphere(0.5f), clear, overlaps);
                simulation.Overlap(new Box(1f, 1f, 1f), clear, overlaps);
                Assert.Empty(overlaps);

                // Thrown down from three above the surface, the sphere touches it after two and a half.
                var above = new RigidPose(new Vector3(4.5f, surface + 3f, 4.5f), Quaternion.Identity);
                Assert.True(simulation.SweepCast(new Sphere(0.5f), above, new BodyVelocity(-Vector3.UnitY, Vector3.Zero), 10f, out var hit));
                Assert.Same(floor, hit.Collidable);
                Assert.Equal(2.5f, hit.Distance, 0.1f);
                Assert.False(simulation.SweepCast(new Sphere(0.5f), above, new BodyVelocity(Vector3.UnitY, Vector3.Zero), 10f, out _));
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Two chunks of a floor without sealed borders leave no hole where they meet: marching cubes chunks sharing a plane of samples,
        /// surface nets chunks sharing two, as their quads stop half a cell short of the border.
        /// </summary>
        [Theory]
        [InlineData(VoxelCellShape.MarchingCubes, 1)]
        [InlineData(VoxelCellShape.SurfaceNets, 2)]
        public static void UnsealedChunksLeaveNoHoleWhereTheyMeet(VoxelCellShape cellShape, int sharedSamplePlanes)
        {
            RunScript(game =>
            {
                const int samples = 6;
                Entity Chunk(float x)
                {
                    var collider = new VoxelCollider { CellShape = cellShape, IsoLevel = IsoLevel, SealBorder = false };
                    collider.SetData(samples, samples, samples, Floor(samples, 2));
                    var chunk = new Entity { new StaticComponent { Collider = collider } };
                    chunk.Transform.Position = new Vector3(x, 0f, 0f);
                    return chunk;
                }

                float second = samples - sharedSamplePlanes;
                var first = Chunk(0f);
                game.SceneSystem.SceneInstance.RootScene.Entities.AddRange(new[] { first, Chunk(second) });
                var simulation = first.GetSimulation();

                // Straight down every tenth of a cell, from a cell and a half before the second chunk to as far into it
                for (int i = -15; i <= 15; ++i)
                {
                    var x = second + i * 0.1f;
                    Assert.True(simulation.RayCast(new Vector3(x, 10f, 2.5f), -Vector3.UnitY, 20f, out var hit), $"The ray at x = {x} falls between the chunks");
                    Assert.Equal(2.5f, hit.Point.Y, Tolerance);
                }
                return Task.CompletedTask;
            });
        }

        /// <summary>A source may have fewer than two samples on an axis, which makes no cell: it is refused as the collider is attached.</summary>
        [Fact]
        public static void ASourceWithoutACellIsRefused()
        {
            RunScript(game =>
            {
                var terrain = new Entity { new StaticComponent { Collider = new PackedVoxelCollider(GC.AllocateArray<ushort>(1, pinned: true), 1) { CellShape = VoxelCellShape.Box } } };
                Assert.Throws<InvalidOperationException>(() => game.SceneSystem.SceneInstance.RootScene.Entities.Add(terrain));
                return Task.CompletedTask;
            });
        }

        /// <summary>Bepu does not look at what a sleeping body rests on: carving the field under it leaves it there until it is woken up.</summary>
        [Fact]
        public static void EditsUnderASleepingBodyNeedItAwake()
        {
            RunScript(async game =>
            {
                const int samples = 10;
                var collider = new VoxelCollider { CellShape = VoxelCellShape.Box, IsoLevel = IsoLevel };
                collider.SetData(samples, samples, samples, Floor(samples, 4));
                var terrain = new Entity { new StaticComponent { Collider = collider } };
                var body = new BodyComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider { Size = new Vector3(1f) } } } };
                var crate = new Entity { body };
                crate.Transform.Position = new Vector3(4.5f, 5.6f, 4.5f);
                game.SceneSystem.SceneInstance.RootScene.Entities.AddRange(new[] { terrain, crate });
                var simulation = terrain.GetSimulation();
                for (int step = 0; step < 60; ++step)
                    await simulation.AfterUpdate();
                float resting = crate.Transform.Position.Y;

                body.Awake = false;
                for (int x = 3; x <= 6; ++x)
                {
                    for (int z = 3; z <= 6; ++z)
                    {
                        collider.SetVoxel(x, 4, z, 0f);
                        collider.SetVoxel(x, 3, z, 0f);
                    }
                }
                for (int step = 0; step < 30; ++step)
                    await simulation.AfterUpdate();
                Assert.Equal(resting, crate.Transform.Position.Y, Tolerance);

                body.Awake = true;
                for (int step = 0; step < 90; ++step)
                    await simulation.AfterUpdate();
                Assert.Equal(resting - 2f, crate.Transform.Position.Y, 0.1f);
            });
        }

        /// <summary>A wall one cell thick across the X axis of a grid of box cells, starting at <paramref name="wallCell"/>.</summary>
        private static Entity BoxWall(int samples, int wallCell, float cellSize)
        {
            // Two solid layers of samples: with an iso level above one half, only the cells between them are solid.
            var field = new float[samples * samples * samples];
            for (int x = wallCell; x <= wallCell + 1; ++x)
            {
                for (int y = 0; y < samples; ++y)
                {
                    for (int z = 0; z < samples; ++z)
                        field[(x * samples + y) * samples + z] = 1f;
                }
            }
            var collider = new VoxelCollider { CellShape = VoxelCellShape.Box, CellSize = cellSize, IsoLevel = 0.75f, SealBorder = false };
            collider.SetData(samples, samples, samples, field);
            return new Entity { new StaticComponent { Collider = collider } };
        }

        [Fact]
        public static void SlantedRaysStopAtAOneCellBoxWall()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                const int samples = 12;
                const int wallCell = 8;
                const float cellSize = 0.3f;
                var terrain = BoxWall(samples, wallCell, cellSize);
                game.SceneSystem.SceneInstance.RootScene.Entities.Add(terrain);
                var simulation = terrain.GetSimulation();

                // Through the middle of the grid's x = 0 face and eight air cells: the ray meets the wall's face right where it enters the wall's cell.
                var middle = (samples - 1) * cellSize * 0.5f;
                var origin = new Vector3(-0.5f, middle, middle);
                for (int i = -8; i <= 8; ++i)
                {
                    for (int j = -8; j <= 8; ++j)
                    {
                        var direction = Vector3.Normalize(new Vector3(1f, i * 0.045f, j * 0.045f));
                        Assert.True(simulation.RayCast(origin, direction, 10f, out var hit), $"The ray towards {direction} goes through the wall");
                        Assert.Equal(wallCell * cellSize, hit.Point.X, Tolerance);
                    }
                }

                game.Exit();
            });
            RunGameTest(game);
        }

        [Fact]
        public static void RaysEnteringOnACellBoundaryStopAtAOneCellBoxWall()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                const int samples = 12;
                const int wallCell = 8;
                var terrain = BoxWall(samples, wallCell, 1f);
                game.SceneSystem.SceneInstance.RootScene.Entities.Add(terrain);
                var simulation = terrain.GetSimulation();

                // Each ray enters the x = 0 face on the line between two rows of cells, going up or down: rounding decides the row it starts in.
                for (int row = 2; row <= 8; ++row)
                {
                    for (int i = -20; i <= 20; ++i)
                    {
                        if (i == 0)
                            continue;
                        var slope = i * 0.01f;
                        var direction = Vector3.Normalize(new Vector3(1f, slope, 0f));
                        Assert.True(simulation.RayCast(new Vector3(-1f, row - slope, 5.5f), direction, 20f, out var hit), $"The ray entering at row {row} with slope {slope} goes through the wall");
                        Assert.Equal(wallCell, hit.Point.X, Tolerance);
                    }
                }

                game.Exit();
            });
            RunGameTest(game);
        }

        [Fact]
        public static void BoxChildrenRotateWithTheBody()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                // A 2x2x2 solid cube (one layer of cells); SealBorder would otherwise empty the border cells.
                var field = new float[27];
                Array.Fill(field, 1f);
                var collider = new VoxelCollider { CellShape = VoxelCellShape.Box, IsoLevel = IsoLevel, SealBorder = false };
                collider.SetData(3, 3, 3, field);
                var terrain = new Entity { new StaticComponent { Collider = collider } };
                // An eighth of a turn around Z stands the cube on its edge: its ridge runs along (0, 2 * sqrt(2), z), with a slope at 45° on each side.
                terrain.Transform.Rotation = Quaternion.RotationZ(MathF.PI / 4f);

                // A single cell stretched into a 3 x 1 x 1 beam, stood on its end by a quarter turn: its top is 3 above the ground.
                var beamCollider = new VoxelCollider { CellShape = VoxelCellShape.Box, IsoLevel = IsoLevel, SealBorder = false };
                beamCollider.SetData(2, 2, 2, [1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f]);
                var beam = new Entity { new StaticComponent { Collider = beamCollider } };
                beam.Transform.Position = new Vector3(10f, 0f, 0f);
                beam.Transform.Scale = new Vector3(3f, 1f, 1f);
                beam.Transform.Rotation = Quaternion.RotationZ(MathF.PI / 2f);

                var ball = new Entity { new BodyComponent { Collider = new CompoundCollider { Colliders = { new SphereCollider { Radius = 0.25f } } } } };
                ball.Transform.Position = new Vector3(9.5f, 5f, 0.5f);
                game.SceneSystem.SceneInstance.RootScene.Entities.AddRange(new[] { terrain, beam, ball });
                var simulation = terrain.GetSimulation();

                // On the slope, a quarter of a cell off the ridge and in the middle of a cell along Z.
                Assert.True(simulation.RayCast(new Vector3(0.25f, 10f, 0.5f), -Vector3.UnitY, 20f, out var hit));
                Assert.Equal(2f * MathF.Sqrt(2f) - 0.25f, hit.Point.Y, Tolerance);
                Assert.True(hit.Normal.Y > 0.5f, $"Normal {hit.Normal}");

                // Contacts see the same boxes as rays: the ball rests on the end of the beam, not on a box left lying at mid-height.
                while (game.UpdateTime.Total.TotalSeconds < 3d)
                    await simulation.AfterUpdate();
                Assert.Equal(3.25f, ball.Transform.Position.Y, Tolerance);

                game.Exit();
            });
            RunGameTest(game);
        }

        [Fact]
        public static void OversizedSpheresStayInTheBoundsOfARotatedStretchedGrid()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                // One cell stretched to 1 x 2 x 1: its sphere, of radius 0.7, sticks 0.2 out of it along the local X and Z axes only.
                var collider = new VoxelCollider { CellShape = VoxelCellShape.Sphere, IsoLevel = IsoLevel, SealBorder = false, SphereRadiusScale = 1.4f };
                collider.SetData(2, 2, 2, [1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f]);
                var terrain = new Entity { new StaticComponent { Collider = collider } };
                terrain.Transform.Scale = new Vector3(1f, 2f, 1f);
                // A quarter turn around Z sends local X to world Y: the cell covers y in [0, 1] and the sphere, centred on (-1, 0.5, 0.5), reaches 1.2.
                terrain.Transform.Rotation = Quaternion.RotationZ(MathF.PI / 2f);
                game.SceneSystem.SceneInstance.RootScene.Entities.Add(terrain);

                // A ray skimming 0.1 above the cell only meets the overshoot.
                Assert.True(terrain.GetSimulation().RayCast(new Vector3(-1f, 1.1f, -5f), Vector3.UnitZ, 10f, out var hit));
                Assert.Equal(0.5f - MathF.Sqrt(0.13f), hit.Point.Z, Tolerance);

                game.Exit();
            });
            RunGameTest(game);
        }

        [Fact]
        public static void OversizedSpheresCoverTheirOvershoot()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                // One cell, so the compound is a single sphere; SphereRadiusScale 1.4 gives radius 0.7 around the body's world centre (0.5, 0.5, 0.5).
                var collider = new VoxelCollider { CellShape = VoxelCellShape.Sphere, IsoLevel = IsoLevel, SealBorder = false, SphereRadiusScale = 1.4f };
                collider.SetData(2, 2, 2, [1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f]);
                var terrain = new Entity { new StaticComponent { Collider = collider } };
                game.SceneSystem.SceneInstance.RootScene.Entities.Add(terrain);
                var simulation = terrain.GetSimulation();

                // x = 1.1 is outside the cell's AABB (world [0, 1]) but inside the sphere's overshoot (world [-0.2, 1.2]).
                // Without the bounds padding the query would clip to the cell box and miss the sphere.
                Assert.True(simulation.RayCast(new Vector3(1.1f, 10f, 0.5f), -Vector3.UnitY, 20f, out var hit));
                // Lateral distance 0.6 from the sphere's centre; the ray lands 0.5 + sqrt(0.13) above the ground.
                Assert.Equal(0.5f + MathF.Sqrt(0.13f), hit.Point.Y, Tolerance);
                Assert.True(hit.Normal.Y > 0f, $"Normal {hit.Normal}");

                // Straight down on the top of the sphere, a point on the very face of the padded bounds the ray is clipped to.
                Assert.True(simulation.RayCast(new Vector3(0.5f, 10f, 0.5f), -Vector3.UnitY, 20f, out hit));
                Assert.Equal(1.2f, hit.Point.Y, Tolerance);

                // Through a corner of the cell the sphere leaves empty, out of its top face, and only then into the overshoot.
                var direction = Vector3.Normalize(new Vector3(0.45f, 0.16f, 0.45f));
                Assert.True(simulation.RayCast(new Vector3(0.05f, 0.99f, 0.05f) - direction * 3f, direction, 20f, out hit));
                Assert.True(hit.Point.Y > 1f, $"Hit {hit.Point}");
                Assert.Equal(0.7f, Vector3.Distance(hit.Point, new Vector3(0.5f)), Tolerance);

                // A small body dropped on the top of the sphere rests on the overshoot (world y = 1.2), 0.2 past the cell's own top.
                var ball = new Entity { new BodyComponent { Collider = new CompoundCollider { Colliders = { new SphereCollider { Radius = 0.05f } } } } };
                ball.Transform.Position = new Vector3(0.5f, 3f, 0.5f);
                game.SceneSystem.SceneInstance.RootScene.Entities.Add(ball);
                while (game.UpdateTime.Total.TotalSeconds < 3d)
                    await simulation.AfterUpdate();
                Assert.Equal(1.2f + 0.05f, ball.Transform.Position.Y, 0.05f);

                game.Exit();
            });
            RunGameTest(game);
        }

        [Fact]
        public static void EntityScaleStretchesTheGrid()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                const int samples = 10;
                var collider = new VoxelCollider { CellShape = VoxelCellShape.SurfaceNets, IsoLevel = IsoLevel };
                collider.SetData(samples, samples, samples, Floor(samples, 4));
                var terrain = new Entity { new StaticComponent { Collider = collider } };
                terrain.Transform.Position = new Vector3(10f, 0f, 0f);
                terrain.Transform.Scale = new Vector3(1f, 2f, 1f);
                game.SceneSystem.SceneInstance.RootScene.Entities.Add(terrain);

                Assert.True(terrain.GetSimulation().RayCast(new Vector3(14.5f, 30f, 4.5f), -Vector3.UnitY, 60f, out var hit));
                Assert.Equal(9f, hit.Point.Y, Tolerance);

                game.Exit();
            });
            RunGameTest(game);
        }

        [Fact]
        public static void ADynamicVoxelBodyRestsFlat()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                const int samples = 6;
                var field = new float[samples * samples * samples];
                Array.Fill(field, 1f);
                var collider = new VoxelCollider { CellShape = VoxelCellShape.Box, IsoLevel = IsoLevel };
                collider.SetData(samples, samples, samples, field);
                var body = new BodyComponent { Collider = collider };
                var crate = new Entity { body };
                crate.Transform.Position = new Vector3(0f, 3f, 0f);
                var floor = new Entity { new StaticComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider { Size = new Vector3(20f, 1f, 20f) } } } } };
                floor.Transform.Position = new Vector3(0f, -0.5f, 0f);
                game.SceneSystem.SceneInstance.RootScene.Entities.AddRange(new[] { floor, crate });

                Assert.Equal(new Vector3(2.5f), body.CenterOfMass);

                var simulation = crate.GetSimulation();
                while (game.UpdateTime.Total.TotalSeconds < 3d)
                    await simulation.AfterUpdate();

                // Sealing empties the grid's edges but leaves the middle of its bottom face solid, from the entity's position up.
                Assert.Equal(0f, crate.Transform.Position.Y, 0.1f);
                Assert.True(MathUtil.WithinEpsilon(MathF.Abs(Quaternion.Dot(crate.Transform.Rotation, Quaternion.Identity)), 1f, 1e-3f), $"Rotation {crate.Transform.Rotation}");

                game.Exit();
            });
            RunGameTest(game);
        }

        [Fact]
        public static void DisposeDetaches()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                const int samples = 10;
                var collider = new VoxelCollider();
                collider.SetData(samples, samples, samples, Floor(samples, 4));
                var terrain = new Entity { new StaticComponent { Collider = collider } };
                game.SceneSystem.SceneInstance.RootScene.Entities.Add(terrain);
                var simulation = terrain.GetSimulation();
                Assert.True(simulation.RayCast(new Vector3(4.5f, 20f, 4.5f), -Vector3.UnitY, 40f, out _));

                collider.Dispose();

                Assert.False(collider.HasData);
                Assert.False(simulation.RayCast(new Vector3(4.5f, 20f, 4.5f), -Vector3.UnitY, 40f, out _));

                game.Exit();
            });
            RunGameTest(game);
        }

        /// <summary>Density in the low byte of a ushort, the rest left to the caller.</summary>
        private struct PackedSource : IVoxelDensitySource
        {
            public Buffer<ushort> Samples;
            public int SamplesX { get; set; }
            public int SamplesY { get; set; }
            public int SamplesZ { get; set; }
            public readonly float Density(int x, int y, int z) => (Samples[(x * SamplesY + y) * SamplesZ + z] & 0xFF) / 255f;
        }

        /// <summary>Reads samples it does not own, as a game keeping its own voxel memory would.</summary>
        private sealed unsafe class PackedVoxelCollider(ushort[] pinnedSamples, int samplesPerAxis) : VoxelColliderBase<PackedSource>
        {
            protected override bool TryGetSource(out PackedSource source)
            {
                var samples = (ushort*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(pinnedSamples));
                source = new PackedSource { Samples = new Buffer<ushort>(samples, pinnedSamples.Length), SamplesX = samplesPerAxis, SamplesY = samplesPerAxis, SamplesZ = samplesPerAxis };
                return true;
            }
        }

        [Fact]
        public static void ACustomLayoutIsReadInPlace()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                const int samples = 10;
                const ushort Material = 0x0700;
                var floor = Floor(samples, 4);
                // Pinned, as the source hands its address to the physics; a material in the high byte, which the source ignores.
                var packed = GC.AllocateArray<ushort>(floor.Length, pinned: true);
                for (int i = 0; i < floor.Length; ++i)
                    packed[i] = (ushort)(Material | (floor[i] > 0f ? 0xFF : 0));
                var custom = new PackedVoxelCollider(packed, samples) { IsoLevel = IsoLevel };
                var copied = new VoxelCollider { IsoLevel = IsoLevel };
                copied.SetData(samples, samples, samples, floor);

                var customTerrain = new Entity { new StaticComponent { Collider = custom } };
                var copiedTerrain = new Entity { new StaticComponent { Collider = copied } };
                copiedTerrain.Transform.Position = new Vector3(20f, 0f, 0f);
                game.SceneSystem.SceneInstance.RootScene.Entities.AddRange(new[] { customTerrain, copiedTerrain });
                var simulation = customTerrain.GetSimulation();

                Assert.True(simulation.RayCast(new Vector3(4.5f, 20f, 4.5f), -Vector3.UnitY, 40f, out var hit));
                Assert.Same(customTerrain.Get<StaticComponent>(), hit.Collidable);
                Assert.Equal(4.5f, hit.Point.Y, Tolerance);
                Assert.True(simulation.RayCast(new Vector3(24.5f, 20f, 4.5f), -Vector3.UnitY, 40f, out hit));
                Assert.Same(copiedTerrain.Get<StaticComponent>(), hit.Collidable);

                // The collider reads the caller's memory: an edit there is seen without telling it.
                for (int x = 3; x <= 6; ++x)
                {
                    for (int z = 3; z <= 6; ++z)
                    {
                        packed[(x * samples + 4) * samples + z] = Material;
                        packed[(x * samples + 3) * samples + z] = Material;
                    }
                }
                Assert.True(simulation.RayCast(new Vector3(4.5f, 20f, 4.5f), -Vector3.UnitY, 40f, out hit));
                Assert.Equal(2.5f, hit.Point.Y, Tolerance);

                game.Exit();
            });
            RunGameTest(game);
        }
    }
}
