// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Particles.Updaters;

namespace Stride.Particles.Spawners
{
    /// <summary>
    /// <see cref="ParticleSpawnTriggerLifetime"/> triggers when the parent particle's remaining lifetime is within the specified limit
    /// </summary>
    [DataContract("ParticleSpawnTriggerLifetime")]
    [Display("Lifetime")]
    public class ParticleSpawnTriggerLifetime : ParticleSpawnTrigger<float>
    {
        private bool limitsAreInOrder;

        /// <summary>
        /// If the parent particle is younger than the lower limit, it won't spawn children. When the lower limit is higher than the upper limit the condition is reversed.
        /// </summary>
        [DataMember(10)]
        [DataMemberRange(0, 1, 0.01, 0.1, 3)]
        [Display("Lower Limit")]
        public float LifetimeLowerLimit { get; set; } = 0f;

        /// <summary>
        /// If the parent particle is older than the upper limit, it won't spawn children. When the upper limit is smaller than the lower limit the condition is reversed.
        /// </summary>
        [DataMember(20)]
        [DataMemberRange(0, 1, 0.01, 0.1, 3)]
        [Display("Upper Limit")]
        public float LifetimeUpperLimit { get; set; } = 1f;

        public override void PrepareFromPool(ParticlePool pool)
        {
            limitsAreInOrder = (LifetimeLowerLimit <= LifetimeUpperLimit);

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

            // We store remaining lifetime in the particle field, so for progress [0..1) we need to take (1 - remaining)
            var currentLifetime = 1f - (*((float*)parentParticle[FieldAccessor]));

            // TODO - Time difference ?
            return ((currentLifetime >= LifetimeLowerLimit) ^ (currentLifetime <= LifetimeUpperLimit) ^ limitsAreInOrder) ? 1f : 0f;
        }
    }
}
