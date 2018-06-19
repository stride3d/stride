// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using NUnit.Framework;
using Xenko.Core.Mathematics;
using Xenko.Particles.Initializers;
using Xenko.Particles.Modules;
using Xenko.Particles.Sorters;

namespace Xenko.Particles.Tests
{
    class ParticlePoolTest
    {       

        /// <summary>
        /// Test the <see cref="ParticlePool"/> behavior similar to what an <see cref="ParticleEmitter"/> would do
        /// </summary>
        /// <param name="policy">Stack or Ring allocation policy</param>
        [TestCase(ParticlePool.ListPolicy.Stack)]
        [TestCase(ParticlePool.ListPolicy.Ring)]
        public unsafe void PoolCapacity(ParticlePool.ListPolicy policy)
        {
            const int maxParticles = 10;
            var pool = new ParticlePool(0, maxParticles, policy);

            const bool forceCreation = true;
            pool.FieldExists(ParticleFields.Position,       forceCreation);
            pool.FieldExists(ParticleFields.RemainingLife,  forceCreation);
            pool.FieldExists(ParticleFields.Velocity,       forceCreation);
            pool.FieldExists(ParticleFields.Size,           forceCreation);

            var testPos = new Vector3(1, 2, 3);
            var testVel = new Vector3(5, 6, 7);
            var testLife = 5f;
            var testSize = 4f;

            // Spawn all particles
            for (int i = 0; i < maxParticles; i++)
            {
                pool.AddParticle();
            }


            {
                // Field accessors break every time there is a change in the pool, so we need to exract them every time
                // We can extract them before the tight loop on all living particles
                var positionField   = pool.GetField(ParticleFields.Position);
                var lifetimeField   = pool.GetField(ParticleFields.RemainingLife);
                var velocityField   = pool.GetField(ParticleFields.Velocity);
                var sizeField       = pool.GetField(ParticleFields.Size);

                foreach (var particle in pool)
                {
                    *((Vector3*)particle[positionField]) = testPos;

                    *((float*)particle[lifetimeField]) = testLife;

                    *((Vector3*)particle[velocityField]) = testVel;

                    *((float*)particle[sizeField]) = testSize;
                }
            }

            // Double the pool capacity and assert that the first half of particles still have the same fields
            pool.SetCapacity(2 * maxParticles);
            {
                // Field accessors break every time there is a change in the pool
                var positionField = pool.GetField(ParticleFields.Position);
                var lifetimeField = pool.GetField(ParticleFields.RemainingLife);
                var velocityField = pool.GetField(ParticleFields.Velocity);
                var sizeField = pool.GetField(ParticleFields.Size);

                var sorter = new ParticleSorterLiving(pool);
                var sortedList = sorter.GetSortedList(new Vector3(0, 0, -1));

                var i = 0;
                foreach (var particle in sortedList)
                {
                    Assert.That(*((Vector3*)particle[positionField]), Is.EqualTo(testPos));

                    Assert.That(*((float*)particle[lifetimeField]), Is.EqualTo(testLife));

                    Assert.That(*((Vector3*)particle[velocityField]), Is.EqualTo(testVel));

                    Assert.That(*((float*)particle[sizeField]), Is.EqualTo(testSize));

                    i++;
                }

                sorter.FreeSortedList(ref sortedList);

                // Assert that the number of living particles is still maxParticles, not maxParticles x2
                Assert.That(i, Is.EqualTo(maxParticles));
            }

            // Halve the pool capacity from its original size. Now all the particles should still have the same fields
            pool.SetCapacity(maxParticles / 2);
            {
                // Field accessors break every time there is a change in the pool
                var positionField = pool.GetField(ParticleFields.Position);
                var lifetimeField = pool.GetField(ParticleFields.RemainingLife);
                var velocityField = pool.GetField(ParticleFields.Velocity);
                var sizeField = pool.GetField(ParticleFields.Size);

                var sorter = new ParticleSorterLiving(pool);
                var sortedList = sorter.GetSortedList(new Vector3(0, 0, -1));

                var i = 0;
                foreach (var particle in sortedList)
                {
                    Assert.That(*((Vector3*)particle[positionField]), Is.EqualTo(testPos));

                    Assert.That(*((float*)particle[lifetimeField]), Is.EqualTo(testLife));

                    Assert.That(*((Vector3*)particle[velocityField]), Is.EqualTo(testVel));

                    Assert.That(*((float*)particle[sizeField]), Is.EqualTo(testSize));

                    i++;
                }

                sorter.FreeSortedList(ref sortedList);

                // Assert that the number of living particles is still maxParticles /2, not maxParticles x2
                Assert.That(i, Is.EqualTo(maxParticles / 2));
            }

        }

        /// <summary>
        /// Test the <see cref="ParticlePool"/> behavior when adding or removing <see cref="ParticleFields"/> to it
        /// </summary>
        /// <param name="policy">Stack or Ring allocation policy</param>
        [TestCase(ParticlePool.ListPolicy.Stack)]
        [TestCase(ParticlePool.ListPolicy.Ring)]
        public unsafe void PoolFields(ParticlePool.ListPolicy policy)
        {
            const int maxParticles = 10;
            var pool = new ParticlePool(0, maxParticles, policy);

            // Spawn all particles
            for (int i = 0; i < maxParticles; i++)
            {
                pool.AddParticle();
            }

            const bool forceCreation = true;

            // Position
            pool.FieldExists(ParticleFields.Position, forceCreation);
            var testPos = new Vector3(1, 2, 3);
            {
                // Field accessors break every time there is a change in the pool, so we need to exract them every time
                // We can extract them before the tight loop on all living particles
                var positionField = pool.GetField(ParticleFields.Position);

                foreach (var particle in pool)
                {
                    *((Vector3*)particle[positionField]) = testPos;
                }
            }

            // Life
            pool.FieldExists(ParticleFields.RemainingLife, forceCreation);
            var testLife = 5f;
            {
                // Field accessors break every time there is a change in the pool, so we need to exract them every time
                // We can extract them before the tight loop on all living particles
                var positionField = pool.GetField(ParticleFields.Position);
                var lifetimeField = pool.GetField(ParticleFields.RemainingLife);
                foreach (var particle in pool)
                {
                    Assert.That(*((Vector3*)particle[positionField]), Is.EqualTo(testPos));

                    *((float*)particle[lifetimeField]) = testLife;
                }
            }


            // Velocity
            pool.FieldExists(ParticleFields.Velocity, forceCreation);
            var testVel = new Vector3(5, 6, 7);
            {
                // Field accessors break every time there is a change in the pool, so we need to exract them every time
                // We can extract them before the tight loop on all living particles
                var positionField = pool.GetField(ParticleFields.Position);
                var lifetimeField = pool.GetField(ParticleFields.RemainingLife);
                var velocityField = pool.GetField(ParticleFields.Velocity);

                foreach (var particle in pool)
                {
                    Assert.That(*((Vector3*)particle[positionField]), Is.EqualTo(testPos));

                    Assert.That(*((float*)particle[lifetimeField]), Is.EqualTo(testLife));

                    *((Vector3*)particle[velocityField]) = testVel;
                }
            }

            // Size
            pool.FieldExists(ParticleFields.Size, forceCreation);
            var testSize = 4f;
            {
                // Field accessors break every time there is a change in the pool, so we need to exract them every time
                // We can extract them before the tight loop on all living particles
                var positionField = pool.GetField(ParticleFields.Position);
                var lifetimeField = pool.GetField(ParticleFields.RemainingLife);
                var velocityField = pool.GetField(ParticleFields.Velocity);
                var sizeField = pool.GetField(ParticleFields.Size);

                foreach (var particle in pool)
                {
                    Assert.That(*((Vector3*)particle[positionField]), Is.EqualTo(testPos));

                    Assert.That(*((float*)particle[lifetimeField]), Is.EqualTo(testLife));

                    Assert.That(*((Vector3*)particle[velocityField]), Is.EqualTo(testVel));

                    *((float*)particle[sizeField]) = testSize;
                }
            }

            // II. Change the capacity and assert that fields are still accessible
            pool.SetCapacity(2 * maxParticles);
            {
                // Field accessors break every time there is a change in the pool
                var positionField = pool.GetField(ParticleFields.Position);
                var lifetimeField = pool.GetField(ParticleFields.RemainingLife);
                var velocityField = pool.GetField(ParticleFields.Velocity);
                var sizeField = pool.GetField(ParticleFields.Size);

                var sorter = new ParticleSorterLiving(pool);
                var sortedList = sorter.GetSortedList(new Vector3(0, 0, -1));

                var i = 0;
                foreach (var particle in sortedList)
                {
                    Assert.That(*((Vector3*)particle[positionField]), Is.EqualTo(testPos));

                    Assert.That(*((float*)particle[lifetimeField]), Is.EqualTo(testLife));

                    Assert.That(*((Vector3*)particle[velocityField]), Is.EqualTo(testVel));

                    Assert.That(*((float*)particle[sizeField]), Is.EqualTo(testSize));

                    i++;
                }

                sorter.FreeSortedList(ref sortedList);

                // Assert that the number of living particles is still maxParticles, not maxParticles x2
                Assert.That(i, Is.EqualTo(maxParticles));
            }

            // Halve the pool capacity from its original size. Now all the particles should still have the same fields
            pool.SetCapacity(maxParticles / 2);
            {
                // Field accessors break every time there is a change in the pool
                var positionField = pool.GetField(ParticleFields.Position);
                var lifetimeField = pool.GetField(ParticleFields.RemainingLife);
                var velocityField = pool.GetField(ParticleFields.Velocity);
                var sizeField = pool.GetField(ParticleFields.Size);

                var sorter = new ParticleSorterLiving(pool);
                var sortedList = sorter.GetSortedList(new Vector3(0, 0, -1));

                var i = 0;
                foreach (var particle in sortedList)
                {
                    Assert.That(*((Vector3*)particle[positionField]), Is.EqualTo(testPos));

                    Assert.That(*((float*)particle[lifetimeField]), Is.EqualTo(testLife));

                    Assert.That(*((Vector3*)particle[velocityField]), Is.EqualTo(testVel));

                    Assert.That(*((float*)particle[sizeField]), Is.EqualTo(testSize));

                    i++;
                }

                sorter.FreeSortedList(ref sortedList);

                // Assert that the number of living particles is still maxParticles /2, not maxParticles x2
                Assert.That(i, Is.EqualTo(maxParticles / 2));
            }

            // III. Remove fields and assert the remaining fields are unchanged

            // Remove velocity
            pool.RemoveField(ParticleFields.Velocity);
            {
                // Field accessors break every time there is a change in the pool
                var positionField = pool.GetField(ParticleFields.Position);
                var lifetimeField = pool.GetField(ParticleFields.RemainingLife);
                var velocityField = pool.GetField(ParticleFields.Velocity);
                var sizeField = pool.GetField(ParticleFields.Size);

                Assert.That(velocityField.IsValid(), Is.EqualTo(false));

                var sorter = new ParticleSorterLiving(pool);
                var sortedList = sorter.GetSortedList(new Vector3(0, 0, -1));

                var i = 0;
                foreach (var particle in sortedList)
                {
                    Assert.That(*((Vector3*)particle[positionField]), Is.EqualTo(testPos));

                    Assert.That(*((float*)particle[lifetimeField]), Is.EqualTo(testLife));

                    Assert.That(*((float*)particle[sizeField]), Is.EqualTo(testSize));

                    i++;
                }

                sorter.FreeSortedList(ref sortedList);

                // Assert that the number of living particles is still maxParticles /2, not maxParticles x2
                Assert.That(i, Is.EqualTo(maxParticles / 2));
            }

            // Remove size
            pool.RemoveField(ParticleFields.Size);
            {
                // Field accessors break every time there is a change in the pool
                var positionField = pool.GetField(ParticleFields.Position);
                var lifetimeField = pool.GetField(ParticleFields.RemainingLife);
                var velocityField = pool.GetField(ParticleFields.Velocity);
                var sizeField = pool.GetField(ParticleFields.Size);

                Assert.That(velocityField.IsValid(), Is.EqualTo(false));
                Assert.That(sizeField.IsValid(), Is.EqualTo(false));

                var sorter = new ParticleSorterLiving(pool);
                var sortedList = sorter.GetSortedList(new Vector3(0, 0, -1));

                var i = 0;
                foreach (var particle in sortedList)
                {
                    Assert.That(*((Vector3*)particle[positionField]), Is.EqualTo(testPos));

                    Assert.That(*((float*)particle[lifetimeField]), Is.EqualTo(testLife));

                    i++;
                }

                sorter.FreeSortedList(ref sortedList);

                // Assert that the number of living particles is still maxParticles /2, not maxParticles x2
                Assert.That(i, Is.EqualTo(maxParticles / 2));
            }

            // Remove position
            pool.RemoveField(ParticleFields.Position);
            {
                // Field accessors break every time there is a change in the pool
                var positionField = pool.GetField(ParticleFields.Position);
                var lifetimeField = pool.GetField(ParticleFields.RemainingLife);
                var velocityField = pool.GetField(ParticleFields.Velocity);
                var sizeField = pool.GetField(ParticleFields.Size);

                Assert.That(velocityField.IsValid(), Is.EqualTo(false));
                Assert.That(sizeField.IsValid(), Is.EqualTo(false));
                Assert.That(positionField.IsValid(), Is.EqualTo(false));

                var sorter = new ParticleSorterLiving(pool);
                var sortedList = sorter.GetSortedList(new Vector3(0, 0, -1));

                var i = 0;
                foreach (var particle in sortedList)
                {
                    Assert.That(*((float*)particle[lifetimeField]), Is.EqualTo(testLife));

                    i++;
                }

                sorter.FreeSortedList(ref sortedList);

                // Assert that the number of living particles is still maxParticles /2, not maxParticles x2
                Assert.That(i, Is.EqualTo(maxParticles / 2));
            }
        }

        /// <summary>
        /// Test the <see cref="ParticleEmitter"/> update for 10 seconds
        /// </summary>
        [Test]
        public void EmitterUpdate()
        {
            var emitter = new ParticleEmitter();
            var dummyParticleSystem = new ParticleSystem();
            const float dt = 0.0166666666667f;

            // Updating the emitter forces the creation of a default spawner (100 particles per second)
            emitter.Update(dt, dummyParticleSystem);

            var forceField = new UpdaterForceField();
            emitter.Updaters.Add(forceField);

            var positionSeed = new InitialPositionSeed();
            emitter.Initializers.Add(positionSeed);

            var velocitySeed = new InitialVelocitySeed();
            emitter.Initializers.Add(velocitySeed);

            // Fixed delta time for simulating 60 fps
            var totalTime = 0f;

            // Simulate 10 seconds
            while (totalTime < 10)
            {
                emitter.Update(dt, dummyParticleSystem);
                totalTime += dt;
            }

            emitter.Updaters.Remove(forceField);

            // Simulate 10 seconds
            while (totalTime < 10)
            {
                emitter.Update(dt, dummyParticleSystem);
                totalTime += dt;
            }

            emitter.Initializers.Remove(positionSeed);

            // Simulate 10 seconds
            while (totalTime < 10)
            {
                emitter.Update(dt, dummyParticleSystem);
                totalTime += dt;
            }

            emitter.Updaters.Add(forceField);
            emitter.Initializers.Add(positionSeed);

            // Simulate 10 seconds
            while (totalTime < 10)
            {
                emitter.Update(dt, dummyParticleSystem);
                totalTime += dt;
            }

            // The emitter update doesn't assert anything as the state of the particle system will be difficult to predict
            //  Such tests will be part of the GraphicsTests
        }


        [TestCase(ParticlePool.ListPolicy.Stack)]
        [TestCase(ParticlePool.ListPolicy.Ring)]
        public unsafe void ParticleGetSet(ParticlePool.ListPolicy policy)
        {
            const int maxParticles = 10;
            var pool = new ParticlePool(0, maxParticles, policy);

            // Spawn all particles
            for (var i = 0; i < maxParticles; i++)
            {
                pool.AddParticle();
            }

            const bool forceCreation = true;
            pool.FieldExists(ParticleFields.Position, forceCreation);
            pool.FieldExists(ParticleFields.RemainingLife, forceCreation);
            pool.FieldExists(ParticleFields.Velocity, forceCreation);
            pool.FieldExists(ParticleFields.Size, forceCreation);

            var positionField = pool.GetField(ParticleFields.Position);
            var lifetimeField = pool.GetField(ParticleFields.RemainingLife);
            var velocityField = pool.GetField(ParticleFields.Velocity);
            var sizeField     = pool.GetField(ParticleFields.Size);

            var vectorToSet = new Vector3(0, 0, 0);
            var scalarToSet = 0f;

            foreach (var particle in pool)
            {
                vectorToSet.Y = scalarToSet;

                *((Vector3*)particle[positionField]) = vectorToSet;

                *((float*)particle[lifetimeField])   = scalarToSet;

                *((Vector3*)particle[velocityField]) = vectorToSet;

                *((float*)particle[sizeField])       = scalarToSet;

                scalarToSet++;
            }

            // Assert the values are the same
            scalarToSet = 0f;
            foreach (var particle in pool)
            {
                Assert.That(*((Vector3*)particle[positionField]), Is.EqualTo(new Vector3(0, scalarToSet, 0)));

                Assert.That(*((float*)particle[lifetimeField]), Is.EqualTo(scalarToSet));

                Assert.That(*((Vector3*)particle[velocityField]), Is.EqualTo(new Vector3(0, scalarToSet, 0)));

                Assert.That(*((float*)particle[sizeField]), Is.EqualTo(scalarToSet));

                scalarToSet++;
            }

            // "Update" the values with delta time
            var dt = 0.033333f;
            foreach (var particle in pool)
            {
                var pos = ((Vector3*)particle[positionField]);
                var vel = ((Vector3*)particle[velocityField]);

                *pos += *vel * dt;

                *((float*)particle[lifetimeField]) += 1;
            }

            scalarToSet = 0f;
            foreach (var particle in pool)
            {
                Assert.That(*((Vector3*)particle[positionField]), Is.EqualTo(new Vector3(0, scalarToSet, 0) + *((Vector3*)particle[velocityField]) * dt));

                Assert.That(*((float*)particle[lifetimeField]), Is.EqualTo(scalarToSet + 1));

                scalarToSet++;
            }

        }
    }
}
