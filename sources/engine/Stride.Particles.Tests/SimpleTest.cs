// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Mathematics;

namespace Stride.Particles.Tests
{
    public class SimpleTest
    {
        [Fact]
        public void OnePlusOne()
        {
            var i = 1;
            i++;

            Assert.Equal(2, i);
        }

        [Fact]
        public void TestEmitter()
        {
            var dummySystem = new ParticleSystem();

            var emitter = new ParticleEmitter();
            emitter.MaxParticlesOverride = 10;
            emitter.ParticleLifetime = new Vector2(1, 1);
            emitter.EmitParticles(5);

            emitter.Update(0.016f, dummySystem);

            Assert.Equal(5, emitter.LivingParticles);
        }
    }
}
