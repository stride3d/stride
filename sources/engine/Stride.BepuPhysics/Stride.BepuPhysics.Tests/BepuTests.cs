// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using BepuPhysics.CollisionDetection;
using Stride.BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.Core.Mathematics;
using Xunit;
using Stride.Engine;
using Stride.Graphics.Regression;

namespace Stride.BepuPhysics.Tests
{
    public class BepuTests : GameTestBase
    {
        [Fact]
        public static void MatrixTest()
        {
            // Some initial expectation as to how the layers are laid out in memory
            Assert.Equal(0, CollisionMatrix.LayersToIndex(CollisionLayer.Layer0, CollisionLayer.Layer0));
            Assert.Equal(1, CollisionMatrix.LayersToIndex(CollisionLayer.Layer1, CollisionLayer.Layer0));
            Assert.Equal(1, CollisionMatrix.LayersToIndex(CollisionLayer.Layer0, CollisionLayer.Layer1));
            Assert.Equal(32, CollisionMatrix.LayersToIndex(CollisionLayer.Layer1, CollisionLayer.Layer1));

            // Ensure that all combinations have exactly one bit associated to them
            int previous = 0;
            for (CollisionLayer l = 0; l <= CollisionLayer.Layer31; l++)
            {
                for (CollisionLayer l2 = l; l2 <= CollisionLayer.Layer31; l2++)
                {
                    int index = CollisionMatrix.LayersToIndex(l, l2);
                    Assert.InRange(index, 0, CollisionMatrix.DataBits-1);
                    Assert.Equal(previous, index);
                    previous++;
                }
            }

            // Ensure iterating reads from the same bits as single shot sample
            for (int otherLayer = 0, head = CollisionMatrix.LayersToIndex(0, CollisionLayer.Layer31);
                  otherLayer <= (int)CollisionLayer.Layer31;
                  head += (otherLayer >= (int)CollisionLayer.Layer31 ? 1 : 31 - otherLayer), otherLayer++)
            {
                Assert.Equal(CollisionMatrix.LayersToIndex((CollisionLayer)otherLayer, CollisionLayer.Layer31), head);
            }

            // Test that basic writes lead to expected outcome
            CollisionMatrix collisions = default;
            collisions.Set(CollisionLayer.Layer1, CollisionLayer.Layer8, true);
            collisions.Set(CollisionLayer.Layer7, CollisionLayer.Layer17, true);
            collisions.Set(CollisionLayer.Layer1, CollisionLayer.Layer0, true);
            collisions.Set(CollisionLayer.Layer2, CollisionLayer.Layer31, true);
            collisions.Set(CollisionLayer.Layer31, CollisionLayer.Layer31, true);
            collisions.Set(CollisionLayer.Layer3, CollisionLayer.Layer8, true);

            Assert.Equal(CollisionMask.Layer31 | CollisionMask.Layer2, collisions.Get(CollisionLayer.Layer31));
            Assert.Equal(CollisionMask.Layer8, collisions.Get(CollisionLayer.Layer3));
            Assert.Equal(CollisionMask.Layer1 | CollisionMask.Layer3, collisions.Get(CollisionLayer.Layer8));
            Assert.Equal(CollisionMask.Layer8 | CollisionMask.Layer0, collisions.Get(CollisionLayer.Layer1));
            Assert.Equal(CollisionMask.Layer7, collisions.Get(CollisionLayer.Layer17));
            Assert.Equal(CollisionMask.Layer17, collisions.Get(CollisionLayer.Layer7));
            Assert.Equal(CollisionMask.Layer31, collisions.Get(CollisionLayer.Layer2));

            collisions.Set(CollisionLayer.Layer1, CollisionLayer.Layer8, false);
            collisions.Set(CollisionLayer.Layer7, CollisionLayer.Layer17, false);
            collisions.Set(CollisionLayer.Layer1, CollisionLayer.Layer0, false);
            collisions.Set(CollisionLayer.Layer2, CollisionLayer.Layer31, false);
            collisions.Set(CollisionLayer.Layer31, CollisionLayer.Layer31, false);
            collisions.Set(CollisionLayer.Layer3, CollisionLayer.Layer8, false);

            Assert.Equal(new CollisionMatrix(), collisions);
        }

        [Fact]
        public static void ConstraintsTest()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                var e1 = new BodyComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } };
                var e2 = new BodyComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } };
                var e3 = new BodyComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } };
                var c = new HingeConstraintComponent { A = e1, B = e2 };

                Assert.False(c.Attached);

                game.SceneSystem.SceneInstance.RootScene.Entities.AddRange(new EntityComponent[]{ e1, e2, e3, c }.Select(x => new Entity{ x }));

                Assert.True(c.Attached);

                e1.SimulationSelector = new IndexBasedSimulationSelector { Index = 1 };

                Assert.False(c.Attached);

                e1.SimulationSelector = new IndexBasedSimulationSelector { Index = 0 };

                Assert.True(c.Attached);

                e1.Entity.Scene = null;

                Assert.False(c.Attached);

                c.A = e3;

                Assert.True(c.Attached);

                ((BoxCollider)((CompoundCollider)e3.Collider).Colliders[0]).Mass *= 2f;

                Assert.True(c.Attached);

                c.A = null;

                Assert.False(c.Attached);

                game.Exit();
            });
            RunGameTest(game);
        }

        [Fact]
        public static void OnContactRemovalTest()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                var c1 = new CharacterComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } };
                var c2 = new StaticComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } };

                var e1 = new Entity { c1 };
                var e2 = new Entity { c2 };

                e1.Transform.Position.Y = 3;

                game.SceneSystem.SceneInstance.RootScene.Entities.AddRange(new[] { e1, e2 });

                var simulation = e1.GetSimulation();

                while (c1.Contacts.Count == 0)
                    await simulation.AfterUpdate(); // Wait for a collision

                foreach (var component in c1.Contacts.Select(x => x.Source).ToArray())
                    component.Entity.Scene = null;

                Assert.Empty(c1.Contacts);

                game.Exit();
            });
            RunGameTest(game);
        }

        [Fact]
        public static void OnTriggerRemovalTest()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                int pairEnded = 0, pairCreated = 0, contactAdded = 0, contactRemoved = 0, startedTouching = 0, stoppedTouching = 0;
                var trigger = new Trigger();
                var e1 = new Entity { new BodyComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } }, ContactEventHandler = trigger } };
                var e2 = new Entity { new StaticComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } } };
                trigger.PairCreated += () => pairCreated++;
                trigger.PairEnded += () => pairEnded++;
                trigger.ContactAdded += () => contactAdded++;
                trigger.ContactRemoved += () => contactRemoved++;
                trigger.StartedTouching += () => startedTouching++;
                trigger.StoppedTouching += () => stoppedTouching++;

                // Remove the component as soon as it enters the trigger to test if the system handles that case properly
                trigger.PairCreated += () => e1.Scene = null;

                e1.Transform.Position.Y = 3;

                game.SceneSystem.SceneInstance.RootScene.Entities.AddRange(new[] { e1, e2 });

                var simulation = e1.GetSimulation();

                while (pairEnded == 0)
                    await simulation.AfterUpdate();

                Assert.Equal(1, pairCreated);
                Assert.Equal(0, contactAdded);
                Assert.Equal(0, startedTouching);

                Assert.Equal(pairCreated, pairEnded);
                Assert.Equal(contactAdded, contactRemoved);
                Assert.Equal(startedTouching, stoppedTouching);

                game.Exit();
            });
            RunGameTest(game);
        }

        [Fact]
        public static void OnTriggerTest()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                int pairEnded = 0, pairCreated = 0, contactAdded = 0, contactRemoved = 0, startedTouching = 0, stoppedTouching = 0;
                var trigger = new Trigger();
                var e1 = new Entity { new BodyComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } }, ContactEventHandler = trigger } };
                var e2 = new Entity { new StaticComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } } };
                trigger.PairCreated += () => pairCreated++;
                trigger.PairEnded += () => pairEnded++;
                trigger.ContactAdded += () => contactAdded++;
                trigger.ContactRemoved += () => contactRemoved++;
                trigger.StartedTouching += () => startedTouching++;
                trigger.StoppedTouching += () => stoppedTouching++;

                e1.Transform.Position.Y = 3;

                game.SceneSystem.SceneInstance.RootScene.Entities.AddRange(new[] { e1, e2 });

                var simulation = e1.GetSimulation();

                while (pairEnded == 0)
                    await simulation.AfterUpdate();

                Assert.Equal(1, pairCreated);
                Assert.NotEqual(0, contactAdded);
                Assert.Equal(1, startedTouching);

                Assert.Equal(pairCreated, pairEnded);
                Assert.Equal(contactAdded, contactRemoved);
                Assert.Equal(startedTouching, stoppedTouching);

                game.Exit();
            });
            RunGameTest(game);
        }

        [Fact]
        public static void OnRaycastRemovalTest()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                var c1 = new CharacterComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } };
                var c2 = new StaticComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } };

                var e1 = new Entity { c1 };
                var e2 = new Entity { c2 };

                e1.Transform.Position.Y = 3;

                game.SceneSystem.SceneInstance.RootScene.Entities.AddRange(new []{ e1, e2 });

                var simulation = e1.GetSimulation();

                Assert.Equal(2, TestRemovalUnsafe(simulation));
                Assert.Equal(0, TestRemovalUnsafe(simulation));

                game.Exit();
            });
            RunGameTest(game);

            int TestRemovalUnsafe(BepuSimulation simulation)
            {
                Span<HitInfoStack> list = stackalloc HitInfoStack[16];
                var hits = simulation.RayCastPenetrating(new Vector3(0, 6, 0), new Vector3(0, -1, 0), 10, list);
                foreach (var hitInfo in hits)
                {
                    hitInfo.Collidable.Entity.Scene = null;
                }

                return hits.Span.Length;
            }
        }

        private class Trigger : IContactEventHandler
        {
            public bool NoContactResponse => true;

            public event Action? ContactAdded, ContactRemoved, StartedTouching, StoppedTouching, PairCreated, PairEnded;

            public void OnStartedTouching<TManifold>(CollidableComponent eventSource, CollidableComponent other, ref TManifold contactManifold, bool flippedManifold, int workerIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
            {
                StartedTouching?.Invoke();
            }

            public void OnStoppedTouching<TManifold>(CollidableComponent eventSource, CollidableComponent other, ref TManifold contactManifold, bool flippedManifold, int workerIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
            {
                StoppedTouching?.Invoke();
            }

            public void OnContactAdded<TManifold>(CollidableComponent eventSource, CollidableComponent other, ref TManifold contactManifold, bool flippedManifold, int contactIndex, int workerIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
            {
                ContactAdded?.Invoke();
            }

            public void OnContactRemoved<TManifold>(CollidableComponent eventSource, CollidableComponent other, ref TManifold contactManifold, bool flippedManifold, int contactIndex, int workerIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
            {
                ContactRemoved?.Invoke();
            }

            public void OnPairCreated<TManifold>(CollidableComponent eventSource, CollidableComponent other, ref TManifold contactManifold, bool flippedManifold, int workerIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
            {
                PairCreated?.Invoke();
            }

            public void OnPairEnded(CollidableComponent eventSource, CollidableComponent other, BepuSimulation bepuSimulation)
            {
                PairEnded?.Invoke();
            }
        }
    }
}
