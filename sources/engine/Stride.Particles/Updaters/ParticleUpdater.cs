// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Particles.Modules
{
    /// <summary>
    /// The <see cref="ParticleUpdater"/> updates one or more fields, such as velocity or position, in all living particles in a target <see cref="ParticlePool"/>
    /// </summary>
    [DataContract("ParticleUpdater")]
    public abstract class ParticleUpdater : ParticleModule
    {
        /// <summary>
        /// All updaters are called exactly once during each <see cref="ParticleEmitter"/>'s update.
        /// Most updaters are called before spawning the new particles for the frame, but post updaters are called after that.
        /// </summary>
        /// <userdoc>
        /// Most updaters are called before spawning the new particles for the frame, but post updaters are called after that.
        /// This is important when the updater needs to verify a value even after it was initialized for the first time.
        /// </userdoc>
        [DataMemberIgnore]
        public virtual bool IsPostUpdater => false;

        /// <summary>
        /// Updates all particles in the <see cref="ParticlePool"/> using this updater
        /// </summary>
        /// <param name="dt">Delta time in seconds which has passed since the last update call</param>
        /// <param name="pool">The target <see cref="ParticlePool"/> which needs to be updated</param>
        public abstract void Update(float dt, ParticlePool pool);
    }
}
