// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
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

                var heightfield = SetupHeightfieldPlane(planeHeight, game, out BepuSimulation simulation, out _);

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

                var heightfield = SetupHeightfieldPlane(planeHeight, game, out BepuSimulation simulation, out _);

                var random = new Random(1010);
                for (int i = 0; i < 256; i++)
                {
                    var posNorm = new Vector2(random.NextSingle(), random.NextSingle()) * 2f - new Vector2(1f);

                    float distFromTarget = random.NextSingle() * 10f;
                    float rayDistWithEpsilon = distFromTarget * 1.1f;
                    float boxSize = 0.1f + random.NextSingle();
                    float halfBoxSize = boxSize * 0.5f;

                    var target = new Vector3(posNorm.X, 0, posNorm.Y) * heightfield.Size;
                    target.Y = planeHeight;

                    Ray ray;
                    ray.Direction = new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle());
                    ray.Direction = ray.Direction * 2f - 1f;
                    ray.Direction = Vector3.Normalize(ray.Direction);
                    ray.Position = target - ray.Direction * distFromTarget;

                    // A sweepcast between a box and a single-sided plane is equivalent to a raycast against a plane enlarged by half the box's size in all axes, see minkowski addition
                    var planeBox = new BoundingBox(new Vector3(0, planeHeight, 0) - halfBoxSize, new Vector3(heightfield.Size, planeHeight, heightfield.Size) + halfBoxSize);
                    bool raycast = planeBox.Intersects(in ray, out float raycastHitDist);

                    bool sweepcast = simulation.SweepCast(new Box(boxSize, boxSize, boxSize), new RigidPose(ray.Position, Quaternion.Identity), new BodyVelocity(ray.Direction, default), rayDistWithEpsilon, out var hitResult);

                    // Sweep tests are not as precise as the box-ray test above,
                    // it's an iterative process similar to root-finding up to within an epsilon or amount of iterations.
                    // See ConvexPairSweepTask.Sweep
                    // We'll take that into consideration when validating hits

                    if (sweepcast)
                    {
                        // Raycast extends indefinitely, sweepcast only up to rayDistWithEpsilon
                        // If sweepcast hit, so should the raycast
                        Assert.True(raycast, "Sweep hit something it shouldn't");
                        Assert.True(MathUtil.WithinEpsilon(raycastHitDist, hitResult.Distance, 0.01f), "Hit distance must match expected distance from target");
                    }
                    else if (raycast)
                    {
                        if (raycastHitDist == 0)
                        {
                            Assert.False(sweepcast, "Sweep should fail when the initial pose already overlaps with the shape");
                        }
                        else if (raycastHitDist >= (distFromTarget + rayDistWithEpsilon) * 0.5f)
                        {
                            // Given that raycast extends indefinitely, it can hit the side of the field when the sweepcast does not extend far enough.
                            // We're ignoring hits past half of the epsilon range instead of after the full epsilon range as
                            // we cannot guarantee a hit when near the end of the sweepcast because of numerical imprecision
                        }
                        else if (ray.Direction.Y >= 0)
                        {
                            // From how we configured this test, the only faces we could hit from this angle are the backfaces.
                            // Similar to meshes, backfaces should be ignored when performing sweepcasts
                            Assert.False(sweepcast, "Must not hit backface target");
                        }
                        else
                        {
                            Assert.True(sweepcast, "Must hit target within bounds");
                            Assert.True(MathUtil.WithinEpsilon(raycastHitDist, hitResult.Distance, 0.01f), "Hit distance must match expected distance from target");
                        }
                    }
                    else
                    {
                        // Neither one of them hit something, we're sweeping in the void
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

                var heightfield = SetupHeightfieldPlane(planeHeight, game, out BepuSimulation simulation, out _);

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

        [Fact]
        public static void SnapToHeightfieldTest()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                const float planeHeight = 1f;

                var heightfield = SetupHeightfieldPlane(planeHeight, game, out _, out var collider);

                var random = new Random(1010);
                for (int i = 0; i < 64; i++)
                {
                    var posNorm = new Vector2(random.NextSingle(), random.NextSingle()) * heightfield.Size;
                    var target = new Vector3(posNorm.X, planeHeight, posNorm.Y);

                    Assert.True(Vector3.NearEqual(target, collider.SnapToSurface(target), new Vector3(0.0001f)));
                }

                Assert.True(Vector3.NearEqual(new Vector3(0, 1, 0), collider.SnapToSurface(new Vector3(-1)), new Vector3(0.0001f)));
                Assert.True(Vector3.NearEqual(new Vector3(heightfield.Size, 1, heightfield.Size), collider.SnapToSurface(new Vector3(heightfield.Size)), new Vector3(0.0001f)));
                Assert.True(Vector3.NearEqual(new Vector3(0, 1, heightfield.Size * 0.5f), collider.SnapToSurface(new Vector3(-1, 0, heightfield.Size * 0.5f)), new Vector3(0.0001f)));

                game.Exit();
            });
            RunGameTest(game);
        }

        [Fact]
        public static void SnapToRotatedHeightfieldTest()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                const float planeHeight = 1f;

                var fieldPos = new Vector3(3, -4, 5);
                var fieldRot = Quaternion.RotationYawPitchRoll(0.3f, 0.2f, 0.1f);

                var heightfield = SetupHeightfieldPlane(planeHeight, game, out _, out var collider, fieldPos, fieldRot);

                var random = new Random(1010);
                for (int i = 0; i < 64; i++)
                {
                    var posNorm = new Vector2(random.NextSingle(), random.NextSingle()) * heightfield.Size;
                    var target = new Vector3(posNorm.X, planeHeight, posNorm.Y);
                    target = fieldPos + fieldRot * target;

                    var snapped = collider.SnapToSurface(target);

                    Assert.True(Vector3.NearEqual(target, snapped, new Vector3(0.0001f)));
                }

                game.Exit();
            });
            RunGameTest(game);
        }

        private static HeightfieldSourceTest SetupHeightfieldPlane(float planeHeight, GameTest game, out BepuSimulation simulation, out HeightfieldCollider collider, Vector3? position = null, Quaternion? rotation = null)
        {
            var hSource = new HeightfieldSourceTest(new FlatPlane(planeHeight));
            var c2 = new StaticComponent
            {
                Collider = collider = new HeightfieldCollider
                {
                    Source = hSource,
                }
            };

            var e2 = new Entity { c2 };
            if (position is { } p)
                e2.Transform.Position = p;
            if (rotation is { } q)
                e2.Transform.Rotation = q;

            game.SceneSystem.SceneInstance.RootScene.Entities.AddRange(new[] { e2 });

            simulation = e2.GetSimulation();
            return hSource;
        }

        private class HeightfieldSourceTest(IHeightfieldSampler Sampler) : IHeightfieldPhysicsSource
        {
            private const int CoarseBlockInterval = 10;
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

            public void GetColliderData(out IHeightfieldSampler sampler, out HeightRange[] coarseBlocks, out int coarseBlocksSubdivision, out int coarseBlockInterval)
            {
                Initialize();
                coarseBlocks = _coarseBlocks;
                coarseBlocksSubdivision = _coarseBlocksSubdivision;
                sampler = Sampler;
                coarseBlockInterval = CoarseBlockInterval;
            }

            private void Initialize()
            {
                _initialized = true;

                _coarseBlocksSubdivision = Subdivision / CoarseBlockInterval + (Subdivision % CoarseBlockInterval == 0 ? 0 : 1);

                _coarseBlocks = new HeightRange[_coarseBlocksSubdivision * _coarseBlocksSubdivision];
                _coarseBlocks.AsSpan().Fill(new HeightRange(float.PositiveInfinity, float.NegativeInfinity));

                for (int i = 0; i < _coarseBlocks.Length; i++)
                    _coarseBlocks[i] = HeightRange.ExtractRange(i, _coarseBlocksSubdivision, CoarseBlockInterval, Subdivision, Sampler);

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
