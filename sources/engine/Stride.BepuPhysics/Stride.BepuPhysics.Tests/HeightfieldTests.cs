// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.Core.Mathematics;
using Xunit;
using Stride.Engine;
using Stride.Graphics.Regression;
using Stride.Heightfield;

namespace Stride.BepuPhysics.Tests
{
    public class HeightfieldTests : GameTestBase
    {
        [Fact]
        public static void RaycastHeightfieldTest()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                const float planeHeight = 1f;

                var heightfield = SetupHeightfieldPlane(planeHeight, game, out BepuSimulation simulation);

                // Topdown ray test at sample points, and whether XZ bounds are correct
                for (int z = -10; z < heightfield.Subdivision + 10; z++)
                {
                    for (int x = -10; x < heightfield.Subdivision + 10; x++)
                    {
                        const float rayLength = 5f;

                        var origin = new Vector3(x, 1, z) * (heightfield.Size / heightfield.Subdivision);
                        origin.Y = planeHeight + rayLength;
                        var rayHit = simulation.RayCast(origin, new Vector3(0, -1, 0), rayLength * 1.01f, out var hitResult);
                        if (origin.Z >= 0 && origin.Z < heightfield.Size && origin.X >= 0 && origin.X < heightfield.Size)
                        {
                            Assert.True(rayHit, "Must hit topdown ray in bounds");
                            Assert.True(MathF.Abs(hitResult.Point.Y - planeHeight) <= float.Epsilon, "Hit point must be at plane's height");
                        }
                        else
                        {
                            Assert.False(rayHit, "Must not hit topdown ray out of bounds");
                        }
                    }
                }

                // Test at an angle
                var random = new Random(1010);
                for (int i = 0; i < 256; i++)
                {
                    Vector2 posNorm = new Vector2(random.NextSingle(), random.NextSingle()) * 2f - new Vector2(1f);

                    var dir = new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle());
                    dir = dir * 2f - 1f;
                    dir = Vector3.Normalize(dir);
                    float distFromTarget = random.NextSingle() * 10f;

                    var target = new Vector3(posNorm.X, 0, posNorm.Y) * heightfield.Size;
                    target.Y = planeHeight;

                    var origin = target - dir * distFromTarget;
                    bool rayHit = simulation.RayCast(origin, dir, distFromTarget * 2f, out var hitResult);
                    if (target.X < 0 || target.X > heightfield.Size || target.Z < 0 || target.Z > heightfield.Size)
                    {
                        Assert.False(rayHit, "Should not hit target out of bounds");
                    }
                    else if (dir.Y >= 0)
                    {
                        // From how we configured this test, the only faces we could hit from this angle are the backfaces.
                        // Similar to meshes, backfaces should be ignored when performing raycasts
                        Assert.False(rayHit, "Must not hit backface target");
                    }
                    else
                    {
                        Assert.True(rayHit, "Must hit target within bounds");
                        Assert.True(MathUtil.NearEqual(distFromTarget, hitResult.Distance), "Hit distance must match expected distance from target");
                    }
                }

                game.Exit();
            });
            RunGameTest(game);
        }

        [Fact]
        public static void SweepHeightfieldTest()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                const float planeHeight = 1f;

                var heightfield = SetupHeightfieldPlane(planeHeight, game, out BepuSimulation simulation);

                var random = new Random(1010);
                for (int i = 0; i < 256; i++)
                {
                    Vector2 posNorm = new Vector2(random.NextSingle(), random.NextSingle()) * 2f - new Vector2(1f);

                    var dir = new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle());
                    dir = dir * 2f - 1f;
                    dir = Vector3.Normalize(dir);
                    float distFromTarget = random.NextSingle() * 10f;
                    float boxSize = 0.1f + random.NextSingle();
                    float halfBoxSize = boxSize * 0.5f;

                    var target = new Vector3(posNorm.X, 0, posNorm.Y) * heightfield.Size;
                    target.Y = planeHeight;

                    var origin = target - dir * distFromTarget;

                    // A sweepcast between a box and a single-sided plane is equivalent to a raycast against a plane enlarged by half the box's size in all axes, see minkowski addition
                    var planeBox = new BoundingBox(new Vector3(0, planeHeight, 0) - halfBoxSize, new Vector3(heightfield.Size, planeHeight, heightfield.Size) + halfBoxSize);
                    bool raycast = planeBox.Intersects(new Ray(origin, dir), out float raycastHitDist);

                    bool sweepcast = simulation.SweepCast(new Box(boxSize, boxSize, boxSize), new RigidPose(origin, Quaternion.Identity), new BodyVelocity(dir, default), distFromTarget * 2f, out var hitResult);

                    if (raycast)
                    {
                        if (raycastHitDist == 0)
                            Assert.False(sweepcast, "Sweep should fail when the initial pose already overlaps with the shape");
                        else
                            Assert.True(sweepcast, "Must hit target within bounds");
                        // Sweep tests are not as precise as the box-ray test above,
                        // it's an iterative process similar to root-finding up to within an epsilon or amount of iterations.
                        // See ConvexPairSweepTask.Sweep
                        Assert.True(MathUtil.WithinEpsilon(raycastHitDist, hitResult.Distance, 0.01f), "Hit distance must match expected distance from target");
                    }
                    else if (dir.Y >= 0)
                    {
                        // From how we configured this test, the only faces we could hit from this angle are the backfaces.
                        // Similar to meshes, backfaces should be ignored when performing raycasts
                        Assert.False(sweepcast, "Must not hit backface target");
                    }
                    else
                    {
                        Assert.False(sweepcast, "Should not hit target out of bounds");
                    }
                }

                game.Exit();
            });
            RunGameTest(game);
        }

        [Fact]
        public static void OverlapHeightfieldTest()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                const float planeHeight = 1f;

                var heightfield = SetupHeightfieldPlane(planeHeight, game, out BepuSimulation simulation);

                Span<CollidableStack> dummy = stackalloc CollidableStack[1];

                var random = new Random(1010);
                for (int i = 0; i < 256; i++)
                {
                    Vector2 posNorm = new Vector2(random.NextSingle(), random.NextSingle()) * 2f - new Vector2(1f);

                    float distFromTarget = random.NextSingle() * 10f;
                    float boxSize = 0.1f + random.NextSingle();
                    float halfBoxSize = boxSize * 0.5f;

                    var target = new Vector3(posNorm.X, 0, posNorm.Y) * heightfield.Size;
                    target.Y = planeHeight;

                    var origin = target - Vector3.UnitY * distFromTarget;

                    // An overlap test between a box and a single-sided plane is equivalent to an overlap test between a point and a plane enlarged by half the box's size in all axes, see minkowski addition
                    var planeBox = new BoundingBox(new Vector3(0, planeHeight, 0) - halfBoxSize, new Vector3(heightfield.Size, planeHeight, heightfield.Size) + halfBoxSize);
                    var containmentType = planeBox.Contains(in origin);

                    bool physicsHit = false;
                    foreach (var _ in simulation.Overlap(new Box(boxSize, boxSize, boxSize), new RigidPose(origin, Quaternion.Identity), dummy))
                        physicsHit = true;

                    switch (containmentType)
                    {
                        case ContainmentType.Disjoint:
                            Assert.False(physicsHit, "Math result does not match physics");
                            break;
                        case ContainmentType.Contains:
                            Assert.True(physicsHit, "Math result does not match physics");
                            break;
                        case ContainmentType.Intersects:
                            Assert.True(physicsHit, "Math result does not match physics");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                game.Exit();
            });
            RunGameTest(game);
        }

        private static HeightfieldSourceTest SetupHeightfieldPlane(float planeHeight, GameTest game, out BepuSimulation simulation)
        {
            var hSource = new HeightfieldSourceTest(new FlatPlane(planeHeight));
            var c2 = new StaticComponent
            {
                Collider = new HeightfieldCollider
                {
                    Source = hSource,
                }
            };

            var e2 = new Entity { c2 };

            game.SceneSystem.SceneInstance.RootScene.Entities.AddRange(new[] { e2 });

            simulation = e2.GetSimulation();
            return hSource;
        }

        private class HeightfieldSourceTest(IHeightfieldSampler Sampler) : IHeightfieldPhysicsSource
        {
            private int _coarseBlocksSubdivision;
            private HeightRange[] _coarseBlocks = Array.Empty<HeightRange>();
            private bool _initialized = false;

            public float Size => 193;

            public int Subdivision => 27;

            public float MinHeight
            {
                get
                {
                    if (_initialized == false)
                    {
                        Initialize();
                    }

                    return field;
                }
                private set;
            }

            public float MaxHeight
            {
                get
                {
                    if (_initialized == false)
                    {
                        Initialize();
                    }

                    return field;
                }
                private set;
            }

            public void GetColliderData(out IHeightfieldSampler sampler, out HeightRange[] coarseBlocks, out int coarseBlocksSubdivision)
            {
                Initialize();
                coarseBlocks = _coarseBlocks;
                coarseBlocksSubdivision = _coarseBlocksSubdivision;
                sampler = Sampler;
            }

            private void Initialize()
            {
                _initialized = true;

                int coarseBlockInterval = 10;
                _coarseBlocksSubdivision = Subdivision / coarseBlockInterval + (Subdivision % coarseBlockInterval == 0 ? 0 : 1);

                _coarseBlocks = new HeightRange[_coarseBlocksSubdivision * _coarseBlocksSubdivision];
                _coarseBlocks.AsSpan().Fill(new HeightRange(float.PositiveInfinity, float.NegativeInfinity));

                for (int i = 0; i < _coarseBlocks.Length; i++)
                    _coarseBlocks[i] = HeightRange.ExtractRange(i, _coarseBlocksSubdivision, coarseBlockInterval, Subdivision, Sampler);

                float min = float.PositiveInfinity;
                float max = float.NegativeInfinity;
                foreach (var coarseBlock in _coarseBlocks)
                {
                    min = MathF.Min(coarseBlock.MinHeight, min);
                    max = MathF.Max(coarseBlock.MaxHeight, max);
                }

                MinHeight = min;
                MaxHeight = max;
            }
        }

        public class FlatPlane(float height) : IHeightfieldSampler
        {
            public void FillSamples(Span<Sample> samples)
            {
                foreach (ref Sample sample in samples)
                {
                    sample.Height = height;
                }
            }
        }
    }
}
