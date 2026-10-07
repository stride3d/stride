// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
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

                var simulation = e2.GetSimulation();

                // Topdown ray test at sample points, and whether XZ bounds are correct
                for (int z = -10; z < hSource.Subdivision + 10; z++)
                {
                    for (int x = -10; x < hSource.Subdivision + 10; x++)
                    {
                        const float rayLength = 5f;

                        var origin = new Vector3(x, 1, z) * (hSource.Size / hSource.Subdivision);
                        origin.Y = planeHeight + rayLength;
                        var rayHit = simulation.RayCast(origin, new Vector3(0, -1, 0), rayLength * 1.01f, out var hitResult);
                        if (origin.Z >= 0 && origin.Z < hSource.Size && origin.X >= 0 && origin.X < hSource.Size)
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

                    var target = new Vector3(posNorm.X, 0, posNorm.Y) * hSource.Size;
                    target.Y = planeHeight;

                    var origin = target - dir * distFromTarget;
                    bool rayHit = simulation.RayCast(origin, dir, distFromTarget * 2f, out var hitResult);
                    if (target.X < 0 || target.X > hSource.Size || target.Z < 0 || target.Z > hSource.Size)
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
