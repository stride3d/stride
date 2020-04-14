// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Particles.Spawners
{
    /// <summary>
    /// <see cref="ParticleSpawnTrigger"/> governs the condition under which particle emission is triggered for spawners like <see cref="SpawnerFromParent"/>
    /// </summary>
    public abstract class ParticleSpawnTrigger
    {
        /// <summary>
        /// Prepares fields accessors before the 
        /// </summary>
        /// <param name="pool"></param>
        public abstract void PrepareFromPool(ParticlePool pool);

        /// <summary>
        /// For positive values, shows if the condition was met and how much is the magnitude
        /// </summary>
        /// <param name="parentParticle">Parent particle which attributes are used for testing</param>
        /// <returns>0f if it hasn't triggered, positive value otherwise, which also indicates the magnitude of the trigger</returns>
        public abstract float HasTriggered(Particle parentParticle);

        /// <summary>
        /// Override to add the required fields to the parent emitter
        /// </summary>
        /// <param name="parentEmitter">Parent emitter to which required fields should be added</param>
        public virtual void AddRequiredParentFields(ParticleEmitter parentEmitter) { }

        /// <summary>
        /// Override to remove the required fields from the parent emitter
        /// </summary>
        /// <param name="parentEmitter">Parent emitter from which required fields should be removed</param>
        public virtual void RemoveRequiredParentFields(ParticleEmitter parentEmitter) { }

    }

    /// <inheritdoc/>
    public abstract class ParticleSpawnTrigger<T> : ParticleSpawnTrigger where T : struct
    {
        protected ParticleFieldAccessor<T> FieldAccessor;         
    }
}
