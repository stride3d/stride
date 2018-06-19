// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Particles.Updaters;

namespace Xenko.Particles.Spawners
{
    /// <summary>
    /// <see cref="ParticleSpawnTriggerDistance"/> triggers when the parent particle tarvels beyond set distance
    /// </summary>
    [DataContract("ParticleSpawnTriggerDistance")]
    [Display("Distance")]
    public class ParticleSpawnTriggerDistance : ParticleSpawnTrigger<Vector3>
    {
        protected ParticleFieldAccessor<Vector3> SecondFieldAccessor;

        public override void PrepareFromPool(ParticlePool pool)
        {
            if (pool == null)
            {
                FieldAccessor = ParticleFieldAccessor<Vector3>.Invalid();
                SecondFieldAccessor = ParticleFieldAccessor<Vector3>.Invalid();
                return;
            }

            FieldAccessor = pool.GetField(ParticleFields.Position);
            SecondFieldAccessor = pool.GetField(ParticleFields.OldPosition);
        }

        public unsafe override float HasTriggered(Particle parentParticle)
        {
            if (!FieldAccessor.IsValid() || !SecondFieldAccessor.IsValid())
                return 0f;

            var deltaPosition = ((*((Vector3*)parentParticle[FieldAccessor])) - (*((Vector3*)parentParticle[SecondFieldAccessor]))).Length();

            return deltaPosition;
        }

        /// <inheritdoc/>
        public override void AddRequiredParentFields(ParticleEmitter parentEmitter)
        {
            parentEmitter?.AddRequiredField(ParticleFields.Position);
            parentEmitter?.AddRequiredField(ParticleFields.OldPosition);
        }

        /// <inheritdoc/>
        public override void RemoveRequiredParentFields(ParticleEmitter parentEmitter)
        {
            parentEmitter?.RemoveRequiredField(ParticleFields.Position);
            parentEmitter?.RemoveRequiredField(ParticleFields.OldPosition);
        }

    }
}
