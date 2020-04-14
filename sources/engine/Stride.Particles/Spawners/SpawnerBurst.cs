// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Particles.Spawners
{
    /// <summary>
    /// A particle spawner which continuously spawns particles. Number of particles to be spawned is given in seconds.
    /// </summary>
    [DataContract("SpawnerBurst")]
    [Display("Burst")]
    public sealed class SpawnerBurst : ParticleSpawner
    {
        [DataMemberIgnore]
        private bool hasBursted;

        [DataMemberIgnore]
        private int spawnCount;

        public SpawnerBurst()
        {
            spawnCount = 100;
            hasBursted = false;
        }

        /// <summary>
        /// The amount of particles this spawner will emit in one burst when it activates
        /// </summary>
        /// <userdoc>
        /// The amount of particles this spawner will emit in one burst when it activates
        /// </userdoc>
        [DataMember(40)]
        [Display("Particles/burst")]
        public int SpawnCount
        {
            get { return spawnCount; }
            set
            {
                MarkAsDirty();
                spawnCount = Math.Max(1, value);
            }
        }

        /// <inheritdoc />
        public override int GetMaxParticlesPerSecond()
        {
            var delay = Math.Min(Delay.X, Delay.Y) + Math.Min(Duration.X, Duration.Y);

            var times = (delay <= 0.01f) ? 100 : 1f/delay;
            times = Math.Max(1, times);

            return (int)Math.Ceiling(SpawnCount * times);
        }

        /// <inheritdoc />
        public override void SpawnNew(float dt, ParticleEmitter emitter)
        {
            var spawnerState = GetUpdatedState(dt, emitter);

            if (spawnerState != SpawnerState.Active)
            {
                hasBursted = false;
                return;
            }

            if (hasBursted)
                return;

            hasBursted = true;

            emitter.EmitParticles(spawnCount);
        }

        /// <inheritdoc />
        protected override void NotifyStateSwitch(SpawnerState newState)
        {
            if (newState != SpawnerState.Active)
                hasBursted = false;
        }

    }
}

