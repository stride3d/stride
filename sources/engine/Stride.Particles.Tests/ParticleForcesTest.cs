// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xunit;
using Stride.Core.Mathematics;
using Stride.Particles.Sorters;
using Stride.Particles.Updaters.FieldShapes;

namespace Stride.Particles.Tests
{
    public class ParticleForcesTest
    {
        [Fact]
        public void ForceFieldShapes()
        {
            var unitPos = new Vector3(0, 0, 0);
            var unitRot = new Quaternion(0, 0, 0, 1);
            var unitScl = new Vector3(1, 1, 1);

            Vector3 alongAxis;
            Vector3 aroundAxis;
            Vector3 awayAxis;

            float falloff;

            // Sphere
            var shapeSphere = new Sphere();
            shapeSphere.PreUpdateField(unitPos, unitRot, unitScl);

            falloff = shapeSphere.GetDistanceToCenter(new Vector3(0.1f, 0, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.Equal(0.1f, falloff);
            Assert.Equal(new Vector3(0, 1, 0), alongAxis);
            Assert.Equal(new Vector3(0, 0, -1), aroundAxis);
            Assert.Equal(new Vector3(1, 0, 0), awayAxis);

            falloff = shapeSphere.GetDistanceToCenter(new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.Equal(0.5f, falloff);
            Assert.Equal(new Vector3(0, 1, 0), alongAxis);
            Assert.Equal(new Vector3(0, 0, -1), aroundAxis);
            Assert.Equal(new Vector3(1, 0, 0), awayAxis);

            falloff = shapeSphere.GetDistanceToCenter(new Vector3(1, 0, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.Equal(1f, falloff);
            Assert.Equal(new Vector3(0, 1, 0), alongAxis);
            Assert.Equal(new Vector3(0, 0, -1), aroundAxis);
            Assert.Equal(new Vector3(1, 0, 0), awayAxis);

            // Box
            var shapeBox = new Cube();
            shapeBox.PreUpdateField(unitPos, unitRot, unitScl);

            falloff = shapeBox.GetDistanceToCenter(new Vector3(0.3f, 0, 0.4f), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.Equal(0.4f, falloff); // Bigger than the two
            Assert.Equal(new Vector3(0, 1, 0), alongAxis);
            Assert.Equal(new Vector3(0.8f, 0, -0.6f), aroundAxis);
            Assert.Equal(new Vector3(0.6f, 0, 0.8f), awayAxis);

            falloff = shapeBox.GetDistanceToCenter(new Vector3(0.5f, 0, 0.4f), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.Equal(0.5f, falloff);
            Assert.Equal(new Vector3(0, 1, 0), alongAxis);

            falloff = shapeBox.GetDistanceToCenter(new Vector3(1, 0, 0.4f), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.Equal(1f, falloff);
            Assert.Equal(new Vector3(0, 1, 0), alongAxis);

            // Cylinder
            var shapeCylinder = new Cylinder();
            shapeCylinder.PreUpdateField(unitPos, unitRot, unitScl);

            falloff = shapeCylinder.GetDistanceToCenter(new Vector3(0, 0, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.Equal(0, falloff);

            falloff = shapeCylinder.GetDistanceToCenter(new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.Equal(0, falloff); // No falloff along the Y-axis

            falloff = shapeCylinder.GetDistanceToCenter(new Vector3(0, 1, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.Equal(1, falloff);

            falloff = shapeCylinder.GetDistanceToCenter(new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.Equal(0.5f, falloff);

            falloff = shapeCylinder.GetDistanceToCenter(new Vector3(1, 0, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.Equal(1f, falloff);

            // Torus
            var shapeTorus = new Torus();
            shapeTorus.PreUpdateField(unitPos, unitRot, unitScl);

            falloff = shapeTorus.GetDistanceToCenter(new Vector3(0, 0, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.Equal(1, falloff); // This is actually outside the torus

            falloff = shapeTorus.GetDistanceToCenter(new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.Equal(1, falloff); // This is on the torus surface, inner circle
            Assert.Equal(new Vector3(0, 0, -1), alongAxis);
            Assert.Equal(new Vector3(-1, 0, 0), awayAxis);
            Assert.Equal(new Vector3(0, 1, 0), aroundAxis);

            falloff = shapeTorus.GetDistanceToCenter(new Vector3(1, 0, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.Equal(0, falloff); // This is on the torus axis
            Assert.Equal(new Vector3(0, 0, -1), alongAxis);

            falloff = shapeTorus.GetDistanceToCenter(new Vector3(1.5f, 0, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.Equal(1, falloff); // This is on the torus surface, outer circle
            Assert.Equal(new Vector3(0, 0, -1), alongAxis);
            Assert.Equal(new Vector3(1, 0, 0), awayAxis);
            Assert.Equal(new Vector3(0, -1, 0), aroundAxis);
        }

        [Fact]
        public void Sorting()
        {
            var customFieldDesc = new ParticleFieldDescription<UInt32>("SomeField", 0);

            const int maxParticles = 4;
            var pool = new ParticlePool(0, maxParticles);

            const bool forceCreation = true;
            pool.FieldExists(ParticleFields.Position, forceCreation); // Force creation of the position field
            pool.FieldExists(ParticleFields.RemainingLife, forceCreation); // Force creation of the life field
            pool.FieldExists(customFieldDesc, forceCreation); // Force creation of the custom field we just declared

            // We can extract them before the tight loop on all living particles
            var posField = pool.GetField(ParticleFields.Position);
            var lifeField = pool.GetField(ParticleFields.RemainingLife);
            var customField = pool.GetField(customFieldDesc);

            // Ad 4 particles
            var particle1 = pool.AddParticle();
            var particle2 = pool.AddParticle();
            var particle3 = pool.AddParticle();
            var particle4 = pool.AddParticle();

            particle1.Set(customField, (uint)1);
            particle2.Set(customField, (uint)2);
            particle3.Set(customField, (uint)3);
            particle4.Set(customField, (uint)4);

            particle1.Set(lifeField, 0.4f);
            particle2.Set(lifeField, 0.8f);
            particle3.Set(lifeField, 0.2f);
            particle4.Set(lifeField, 0.6f);

            particle1.Set(posField, new Vector3(0, 0, 3));
            particle2.Set(posField, new Vector3(0, 0, 9));
            particle3.Set(posField, new Vector3(0, 0, 5));
            particle4.Set(posField, new Vector3(0, 0, 1));

            // Don't sort
            uint[] sortedNone = { 1, 2, 3, 4 }; // List of expected values
            {
                var i = 0;
                foreach (var particle in pool)
                {
                    Assert.Equal(sortedNone[i++], particle.Get(customField));
                }
            }

            // Sort by depth
            uint[] sortedDepth = { 4, 1, 3, 2 }; // List of expected values
            {
                var depthSorter = new ParticleSorterDepth(pool);
                var sortedList = depthSorter.GetSortedList(new Vector3(0, 0, 1));

                var i = 0;
                foreach (var particle in sortedList)
                {
                    Assert.Equal(sortedDepth[i++], particle.Get(customField));
                }
            }

            // Sort by age
            uint[] sortedAge = { 2, 4, 1, 3 }; // List of expected values
            {
                var ageSorter = new ParticleSorterAge(pool);
                var sortedList = ageSorter.GetSortedList(new Vector3(0, 0, 1));

                var i = 0;
                foreach (var particle in sortedList)
                {
                    Assert.Equal(sortedAge[i++], particle.Get(customField));
                }
            }

        }

    }
}
