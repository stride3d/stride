// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using Stride.BepuPhysics.Components;
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
        public static void ConstraintsForceTest()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                var e1 = new BodyComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } };
                var c = new OneBodyLinearMotorConstraintComponent
                {
                    A = e1,
                    LocalOffset = Vector3.Zero,
                    MotorMaximumForce = 3,
                    MotorDamping = 1,
                };

                Assert.Equal(0f, c.GetAccumulatedForceMagnitude());

                game.SceneSystem.SceneInstance.RootScene.Entities.AddRange(new EntityComponent[] { e1, c }.Select(x => new Entity { x }));

                Assert.Equal(0f, c.GetAccumulatedForceMagnitude());

                do
                {
                    await e1.Simulation!.AfterUpdate();

                    // Given current gravity, the constraint should pull the body in under 5 seconds,
                    // otherwise something is wrong and this loop would likely continue indefinitely
                    Assert.True(game.UpdateTime.Total.TotalSeconds < 5d);
                } while (c.GetAccumulatedForceMagnitude() < float.BitDecrement(c.MotorMaximumForce));

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
        public static void OnContactRollTest()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                int contactStarted = 0, contactStopped = 0, passedGoal = 0;
                var killTrigger = new ContactEvents { NoContactResponse = true };
                var contacts = new ContactEvents { NoContactResponse = false };
                var sphere = new Entity { new BodyComponent { Collider = new CompoundCollider { Colliders = { new SphereCollider() } } } };
                var slope = new Entity { new StaticComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider { Size = new(2, 0.1f, 2) } } }, ContactEventHandler = contacts } };
                var goal = new Entity { new StaticComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider { Size = new(10, 0.1f, 10) } } }, ContactEventHandler = killTrigger } };
                contacts.StartedTouching += (_, _) => contactStarted++;
                contacts.StoppedTouching += (_, _) => contactStopped++;
                killTrigger.StoppedTouching += (_, _) => passedGoal++;

                sphere.Transform.Position.Y = 3;
                slope.Transform.Rotation = Quaternion.RotationZ(10);
                goal.Transform.Position.Y = -10;

                game.SceneSystem.SceneInstance.RootScene.Entities.AddRange(new[] { sphere, slope, goal });

                var simulation = sphere.GetSimulation();

                while (passedGoal == 0)
                    await simulation.AfterUpdate();

                Assert.Equal(1, contactStarted);

                Assert.Equal(contactStarted, contactStopped);

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

                int startedTouching = 0, stoppedTouching = 0;
                var trigger = new ContactEvents { NoContactResponse = true };
                var e1 = new Entity { new BodyComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } }, ContactEventHandler = trigger } };
                var e2 = new Entity { new StaticComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } } };
                trigger.StartedTouching += (_, _) => startedTouching++;
                trigger.StoppedTouching += (_, _) => stoppedTouching++;

                // Remove the component as soon as it enters the trigger to test if the system handles that case properly
                trigger.StartedTouching += (_, _) => e1.Scene = null;

                e1.Transform.Position.Y = 3;

                game.SceneSystem.SceneInstance.RootScene.Entities.AddRange(new[] { e1, e2 });

                var simulation = e1.GetSimulation();

                while (stoppedTouching == 0)
                    await simulation.AfterUpdate();

                Assert.Equal(1, startedTouching);

                Assert.Equal(startedTouching, stoppedTouching);

                game.Exit();
            });
            RunGameTest(game);
        }

        [Fact]
        public void ContactImpulseTest()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                var contactE = new ContactSampleForces();
                var e1 = new Entity { new BodyComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } }, ContactEventHandler = contactE } };
                var e2 = new Entity { new StaticComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } } };

                var source = e1.Get<BodyComponent>()!;
                source.ContinuousDetectionMode = ContinuousDetectionMode.Continuous;
                e1.Transform.Position.Y = 3;

                game.SceneSystem.SceneInstance.RootScene.Entities.AddRange(new[] { e1, e2 });
                source.LinearVelocity = new Vector3(0, -100, 0);

                var simulation = e1.GetSimulation();

                while (contactE.Exit == false)
                    await simulation.AfterUpdate();

                Assert.Contains(contactE.ImpactForces, x => x.Length() > 100);

                game.Exit();
            });
            RunGameTest(game);
        }

        private class ContactSampleForces : IContactHandler
        {
            public bool NoContactResponse => false;

            public List<Vector3> ImpactForces = new();
            public bool Exit;

            public void OnStartedTouching<TManifold>(Contacts<TManifold> contacts) where TManifold : unmanaged, IContactManifold<TManifold>
            {
                foreach (var contact in contacts)
                {
                    ImpactForces.Add(contacts.ComputeImpactForce(contact));
                }
            }

            public void OnTouching<TManifold>(Contacts<TManifold> manifold) where TManifold : unmanaged, IContactManifold<TManifold>
            {
            }

            public void OnStoppedTouching<TManifold>(Contacts<TManifold> manifold) where TManifold : unmanaged, IContactManifold<TManifold>
            {
                Exit = true;
            }
        }

        [Fact]
        public static void OnTriggerTest()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                int startedTouching = 0, stoppedTouching = 0;
                var trigger = new ContactEvents { NoContactResponse = true };
                var e1 = new Entity { new BodyComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } }, ContactEventHandler = trigger } };
                var e2 = new Entity { new StaticComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } } };
                trigger.StartedTouching += (_, _) => startedTouching++;
                trigger.StoppedTouching += (_, _) => stoppedTouching++;

                e1.Transform.Position.Y = 3;

                game.SceneSystem.SceneInstance.RootScene.Entities.AddRange(new[] { e1, e2 });

                var simulation = e1.GetSimulation();

                while (stoppedTouching == 0)
                    await simulation.AfterUpdate();

                Assert.Equal(1, startedTouching);

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

        [Fact]
        public static void OnSimulationUpdateRemovalTest()
        {
            var game = new GameTest();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                var listOfUpdate = new List<int>();
                var c2 = new StaticComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider() } } };

                var allEntities = game.SceneSystem.SceneInstance.RootScene.Entities;

                Entity a, b, c, d, e;
                allEntities.AddRange(new[]
                {
                    a = new Entity { new SimUpdateListener { SimUpdate = () => { listOfUpdate.Add(0); } } },
                    b = new Entity { new SimUpdateListener { SimUpdate = () => { listOfUpdate.Add(1); } } },
                    c = new Entity { new SimUpdateListener { SimUpdate = () => { listOfUpdate.Add(2); } } },
                    d = new Entity { new SimUpdateListener { SimUpdate = () => { listOfUpdate.Add(3); } } },
                    new Entity { c2 },
                });

                var simulation = allEntities[0].GetSimulation();


                // First test, check if every component received its update
                {
                    await simulation.AfterUpdate();

                    Assert.Equal([0, 1, 2, 3], listOfUpdate);
                }


                // Second test, check if removing works appropriately
                {
                    listOfUpdate.Clear();
                    allEntities.Remove(b);
                    allEntities.Remove(c);

                    await simulation.AfterUpdate();

                    // We've removed the second and third before running sim,
                    // so only the first and fourth should report as having received the update
                    Assert.Equal([0, 3], listOfUpdate);
                }


                // Clearing multiple listeners while running a listener
                {
                    listOfUpdate.Clear();
                    allEntities.Remove(a);
                    allEntities.Remove(d);

                    var toRemove = new List<Entity>();
                    allEntities.AddRange(new[]
                    {
                        a = new Entity { new SimUpdateListener { SimUpdate = () => { listOfUpdate.Add(0); } } },
                        b = new Entity { new SimUpdateListener { SimUpdate = () => { listOfUpdate.Add(1); } } },
                        c = new Entity
                        {
                            new SimUpdateListener
                            {
                                SimUpdate = () =>
                                {
                                    listOfUpdate.Add(2);
                                    foreach (var entity in toRemove)
                                        allEntities.Remove(entity);
                                }
                            }
                        },
                        d = new Entity { new SimUpdateListener { SimUpdate = () => { listOfUpdate.Add(3); } } },
                        e = new Entity { new SimUpdateListener { SimUpdate = () => { listOfUpdate.Add(4); } } }
                    });

                    toRemove.AddRange([a, b, c, e]);

                    await simulation.AfterUpdate();

                    // We've removed a, b and c right after running them, e before it could run and haven't removed d
                    Assert.Equal([0, 1, 2, 3], listOfUpdate);
                }

                game.Exit();
            });
            RunGameTest(game);
        }

        private class SimUpdateListener : ScriptComponent, ISimulationUpdate
        {
            public Action? SimUpdate, AfterSimUpdate;

            public void SimulationUpdate(BepuSimulation simulation, float simTimeStep) => SimUpdate?.Invoke();

            public void AfterSimulationUpdate(BepuSimulation simulation, float simTimeStep) => AfterSimUpdate?.Invoke();
        }

        private class ContactEvents : IContactHandler
        {
            public required bool NoContactResponse { get; init; }

            public event Action<CollidableComponent, CollidableComponent>? StartedTouching, Touching, StoppedTouching;

            public void OnStartedTouching<TManifold>(Contacts<TManifold> manifold) where TManifold : unmanaged, IContactManifold<TManifold>
            {
                StartedTouching?.Invoke(manifold.EventSource, manifold.Other);
            }

            public void OnTouching<TManifold>(Contacts<TManifold> manifold) where TManifold : unmanaged, IContactManifold<TManifold>
            {
                Touching?.Invoke(manifold.EventSource, manifold.Other);
            }

            public void OnStoppedTouching<TManifold>(Contacts<TManifold> manifold) where TManifold : unmanaged, IContactManifold<TManifold>
            {
                StoppedTouching?.Invoke(manifold.EventSource, manifold.Other);
            }
        }
    }
}
