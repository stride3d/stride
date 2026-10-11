// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
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

        [Fact]
        public static void SetDataRebuildsWhenOnlyTheShapeOfTheGridChanges()
        {
            using var collider = new VoxelCollider();
            collider.SetData(3, 2, 4, new float[24]);
            var field = new float[24];
            field[(1 * 3 + 2) * 2 + 1] = 7f;
            collider.SetData(4, 3, 2, field);

            Assert.Equal(new Int3(4, 3, 2), collider.SampleCount);
            Assert.Equal(7f, collider.GetVoxel(1, 2, 1));
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
            const int stride = 16;
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
