// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using NUnit.Framework;
using Xenko.Core.Mathematics;

namespace Xenko.Particles.Tests
{
    class SimpleTest
    {
        [Test]
        public void OnePlusOne()
        {
            var i = 1;
            i++;

            Assert.That(i, Is.EqualTo(2));
        }

        [Test]
        public void TestEmitter()
        {
            var dummySystem = new ParticleSystem();

            var emitter = new ParticleEmitter();
            emitter.MaxParticlesOverride = 10;
            emitter.ParticleLifetime = new Vector2(1, 1);
            emitter.EmitParticles(5);

            emitter.Update(0.016f, dummySystem);

            Assert.That(emitter.LivingParticles, Is.EqualTo(5));
        }
    }
}
