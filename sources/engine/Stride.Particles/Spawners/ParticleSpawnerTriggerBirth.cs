// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Particles.Spawners
{
    /// <summary>
    /// <see cref="ParticleSpawnTriggerBirth"/> triggers when the parent particle is first spawned
    /// </summary>
    [DataContract("ParticleSpawnTriggerBirth")]
    [Display("On Birth")]
    public class ParticleSpawnTriggerBirth : ParticleSpawnTrigger<float>
    {
        public override void PrepareFromPool(ParticlePool pool)
        {
            if (pool == null)
            {
                FieldAccessor = ParticleFieldAccessor<float>.Invalid();
                return;
            }

            FieldAccessor = pool.GetField(ParticleFields.RemainingLife);
        }

        public unsafe override float HasTriggered(Particle parentParticle)
        {
            if (!FieldAccessor.IsValid())
                return 0f;

            var currentLifetime = 1f - (*((float*)parentParticle[FieldAccessor]));

            return (currentLifetime <= MathUtil.ZeroTolerance) ? 1f : 0f;
        }
    }
}
