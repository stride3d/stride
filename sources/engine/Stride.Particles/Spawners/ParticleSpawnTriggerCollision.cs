// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Particles.Updaters;

namespace Stride.Particles.Spawners
{
    /// <summary>
    /// <see cref="ParticleSpawnTriggerCollision"/> triggers when the parent particle collides with a surface
    /// </summary>
    [DataContract("ParticleSpawnTriggerCollision")]
    [Display("On Hit")]
    public class ParticleSpawnTriggerCollision : ParticleSpawnTrigger<ParticleCollisionAttribute>
    {
        public override void PrepareFromPool(ParticlePool pool)
        {
            if (pool == null)
            {
                FieldAccessor = ParticleFieldAccessor<ParticleCollisionAttribute>.Invalid();
                return;
            }

            FieldAccessor = pool.GetField(ParticleFields.CollisionControl);
        }

        public unsafe override float HasTriggered(Particle parentParticle)
        {
            if (!FieldAccessor.IsValid())
                return 0f;

            var collisionAttribute = (*((ParticleCollisionAttribute*)parentParticle[FieldAccessor]));
            return (collisionAttribute.HasColided) ? 1f : 0f;
        }
    }
}
