// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using NUnit.Framework;
using Xenko.Core.Mathematics;
using Xenko.Particles.Sorters;
using Xenko.Particles.Updaters.FieldShapes;

namespace Xenko.Particles.Tests
{
    internal class ParticleForcesTest
    {
        [Test]
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
            Assert.That(falloff, Is.EqualTo(0.1f));
            Assert.That(alongAxis, Is.EqualTo(new Vector3(0, 1, 0)));
            Assert.That(aroundAxis, Is.EqualTo(new Vector3(0, 0, -1)));
            Assert.That(awayAxis, Is.EqualTo(new Vector3(1, 0, 0)));

            falloff = shapeSphere.GetDistanceToCenter(new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.That(falloff, Is.EqualTo(0.5f));
            Assert.That(alongAxis, Is.EqualTo(new Vector3(0, 1, 0)));
            Assert.That(aroundAxis, Is.EqualTo(new Vector3(0, 0, -1)));
            Assert.That(awayAxis, Is.EqualTo(new Vector3(1, 0, 0)));

            falloff = shapeSphere.GetDistanceToCenter(new Vector3(1, 0, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.That(falloff, Is.EqualTo(1f));
            Assert.That(alongAxis, Is.EqualTo(new Vector3(0, 1, 0)));
            Assert.That(aroundAxis, Is.EqualTo(new Vector3(0, 0, -1)));
            Assert.That(awayAxis, Is.EqualTo(new Vector3(1, 0, 0)));

            // Box
            var shapeBox = new Cube();
            shapeBox.PreUpdateField(unitPos, unitRot, unitScl);

            falloff = shapeBox.GetDistanceToCenter(new Vector3(0.3f, 0, 0.4f), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.That(falloff, Is.EqualTo(0.4f)); // Bigger than the two
            Assert.That(alongAxis, Is.EqualTo(new Vector3(0, 1, 0)));
            Assert.That(aroundAxis, Is.EqualTo(new Vector3(0.8f, 0, -0.6f)));
            Assert.That(awayAxis, Is.EqualTo(new Vector3(0.6f, 0, 0.8f)));

            falloff = shapeBox.GetDistanceToCenter(new Vector3(0.5f, 0, 0.4f), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.That(falloff, Is.EqualTo(0.5f));
            Assert.That(alongAxis, Is.EqualTo(new Vector3(0, 1, 0)));

            falloff = shapeBox.GetDistanceToCenter(new Vector3(1, 0, 0.4f), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.That(falloff, Is.EqualTo(1f));
            Assert.That(alongAxis, Is.EqualTo(new Vector3(0, 1, 0)));

            // Cylinder
            var shapeCylinder = new Cylinder();
            shapeCylinder.PreUpdateField(unitPos, unitRot, unitScl);

            falloff = shapeCylinder.GetDistanceToCenter(new Vector3(0, 0, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.That(falloff, Is.EqualTo(0));

            falloff = shapeCylinder.GetDistanceToCenter(new Vector3(0, 0.5f, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.That(falloff, Is.EqualTo(0)); // No falloff along the Y-axis

            falloff = shapeCylinder.GetDistanceToCenter(new Vector3(0, 1, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.That(falloff, Is.EqualTo(1));

            falloff = shapeCylinder.GetDistanceToCenter(new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.That(falloff, Is.EqualTo(0.5f));

            falloff = shapeCylinder.GetDistanceToCenter(new Vector3(1, 0, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.That(falloff, Is.EqualTo(1f));

            // Torus
            var shapeTorus = new Torus();
            shapeTorus.PreUpdateField(unitPos, unitRot, unitScl);

            falloff = shapeTorus.GetDistanceToCenter(new Vector3(0, 0, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.That(falloff, Is.EqualTo(1)); // This is actually outside the torus

            falloff = shapeTorus.GetDistanceToCenter(new Vector3(0.5f, 0, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.That(falloff, Is.EqualTo(1)); // This is on the torus surface, inner circle
            Assert.That(alongAxis, Is.EqualTo(new Vector3(0, 0, -1)));
            Assert.That(awayAxis, Is.EqualTo(new Vector3(-1, 0, 0)));
            Assert.That(aroundAxis, Is.EqualTo(new Vector3(0, 1, 0)));

            falloff = shapeTorus.GetDistanceToCenter(new Vector3(1, 0, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.That(falloff, Is.EqualTo(0)); // This is on the torus axis
            Assert.That(alongAxis, Is.EqualTo(new Vector3(0, 0, -1)));

            falloff = shapeTorus.GetDistanceToCenter(new Vector3(1.5f, 0, 0), new Vector3(0, 1, 0), out alongAxis, out aroundAxis, out awayAxis);
            Assert.That(falloff, Is.EqualTo(1)); // This is on the torus surface, outer circle
            Assert.That(alongAxis, Is.EqualTo(new Vector3(0, 0, -1)));
            Assert.That(awayAxis, Is.EqualTo(new Vector3(1, 0, 0)));
            Assert.That(aroundAxis, Is.EqualTo(new Vector3(0, -1, 0)));
        }

        [Test]
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
                    Assert.That(particle.Get(customField), Is.EqualTo(sortedNone[i++]));
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
                    Assert.That(particle.Get(customField), Is.EqualTo(sortedDepth[i++]));
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
                    Assert.That(particle.Get(customField), Is.EqualTo(sortedAge[i++]));
                }
            }

        }

    }
}
