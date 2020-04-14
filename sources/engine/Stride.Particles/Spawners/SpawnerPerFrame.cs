// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;

namespace Stride.Particles.Spawners
{
    /// <summary>
    /// A particle spawner which continuously spawns particles. Number of particles to be spawned is given in seconds.
    /// </summary>
    [DataContract("SpawnerPerFrame")]
    [Display("Per frame")]
    public sealed class SpawnerPerFrame : ParticleSpawner
    {
        [DataMemberIgnore]
        private float carryOver;

        [DataMemberIgnore]
        private float spawnCount;

        [DataMemberIgnore]
        private float defaultFramerate = 60;


        /// <summary>
        /// Default constructor
        /// </summary>
        public SpawnerPerFrame()
        {
            spawnCount = 1f;
            carryOver = 0;
        }


        /// <summary>
        /// The amount of particles this spawner will emit each frame
        /// </summary>
        /// <userdoc>
        /// The amount of particles this spawner will emit each frame
        /// </userdoc>
        [DataMember(40)]
        [Display("Particles/frame")]
        public float SpawnCount
        {
            get { return spawnCount; }
            set
            {
                MarkAsDirty();
                spawnCount = value;
            }
        }

        /// <summary>
        /// The maximum framerate you expect your game to achieve. It is only used for maximum particles estimation, not for actual spawning rate
        /// </summary>
        /// <userdoc>
        /// The maximum framerate you expect your game to achieve. It is only used for maximum particles estimation, not for actual spawning rate
        /// </userdoc>
        [DataMember(45)]
        [Display("Framerate")]
        public float Framerate
        {
            get { return defaultFramerate; }
            set
            {
                MarkAsDirty();
                defaultFramerate = value;
            }
        }

        /// <inheritdoc />
        public override int GetMaxParticlesPerSecond()
        {
            return (int)Math.Ceiling(SpawnCount * defaultFramerate);
        }

        /// <inheritdoc />
        public override void SpawnNew(float dt, ParticleEmitter emitter)
        {
            var spawnerState = GetUpdatedState(dt, emitter);
            if (spawnerState != SpawnerState.Active)
                return;

            var toSpawn = spawnCount + carryOver;

            var integerPart = (int)Math.Floor(toSpawn);
            carryOver = toSpawn - integerPart;

            emitter.EmitParticles(integerPart);
        }

    }
}

