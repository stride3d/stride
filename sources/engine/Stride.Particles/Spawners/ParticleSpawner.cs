// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Particles.Spawners
{
    public enum SpawnerLoopCondition : byte
    {
        /// <summary>
        /// Looping spawner will loop to the beginning of its Delay state when its Duration is over
        /// If there is no delay it's indistinguishable from LoopingNoDelay
        /// </summary>
        [Display("Looping")]
        Looping,

        /// <summary>
        /// LoopingNoDelay spawner will loop to the beginning of its Active state when its Duration is over
        ///     essentially skipping the Delay state after the first loop
        /// If there is no delay it's indistinguishable from Looping
        /// </summary>
        [Display("Looping, no delay")]
        LoopingNoDelay,

        /// <summary>
        /// OneShot particle spawners will not loop and will only be ative for a period of time equal to its Duration
        /// </summary>
        [Display("One shot")]
        OneShot,

    }

    public enum SpawnerState : byte
    {
        /// <summary>
        /// A spawner in Inactive state hasn't been updated yet. Upon constructing, the Spawner is Inactive,
        ///     but can't return to this state anymore
        /// </summary>
        Inactive,

        /// <summary>
        /// A spawner starts in Rest state and stays in this state for as long as it is delayed.
        /// While in Rest state it doesn't emit particles and switches to Active state after the Rest state is over
        /// </summary>
        Rest,

        /// <summary>
        /// A spawner in Active state emits particles. After the Active state expires, the spawner can switch to
        ///     Rest, Active or Dead state depending on its looping condition.
        /// </summary>
        Active,

        /// <summary>
        /// A spawner in Dead state is not emitting particles and likely not switching to Rest or Active anymore.
        /// </summary>
        Dead,
    }

    /// <summary>
    /// <see cref="ParticleSpawner"/> governs the rate at which new particles are emitted into the <see cref="ParticleEmitter"/>
    /// Multiple spawners with different triggering conditions can be part of the same <see cref="ParticleEmitter"/>
    /// </summary>
    [DataContract("ParticleSpawner")]
    public abstract class ParticleSpawner 
    {
        [DataMemberIgnore]
        private ParticleEmitter emitter;

        [DataMemberIgnore]
        private SpawnerState state = SpawnerState.Inactive;

        [DataMemberIgnore]
        private float stateDuration;

        [DataMemberIgnore]
        private RandomSeed randomSeed = new RandomSeed(0);

        [DataMemberIgnore]
        private uint randomOffset = 0;

        [DataMemberIgnore]
        private Vector2 delay = Vector2.Zero;

        [DataMemberIgnore]
        private Vector2 duration = Vector2.One;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ParticleSpawner"/> is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        [DataMember(-10)]
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Indicates if the spawner should loop, and if there is a delay every time it loops
        /// </summary>
        /// <userdoc>
        /// Indicates if the spawner should loop, and if there is a delay every time it loops
        /// </userdoc>
        [DataMember(5)]
        [Display("Loop")]
        public SpawnerLoopCondition LoopCondition { get; set; } = SpawnerLoopCondition.Looping;

        /// <summary>
        /// The minimum and maximum time the spawner should wait before starting to emit particles
        /// </summary>
        /// <userdoc>
        /// The minimum and maximum time the spawner should wait before starting to emit particles
        /// </userdoc>
        [DataMember(10)]
        [Display("Delay")]
        public Vector2 Delay
        {
            get { return delay; }
            set
            {
                delay = value;
                delay.X = Math.Max(delay.X, 0f);
                delay.Y = Math.Max(delay.Y, 0f);
            }
        }

        /// <summary>
        /// The minimum and maximum duration the spawner will be active once it starts spawning particles
        /// </summary>
        /// <userdoc>
        /// The minimum and maximum duration the spawner will be active once it starts spawning particles
        /// </userdoc>
        [DataMember(15)]
        [Display("Duration")]
        public Vector2 Duration
        {
            get { return duration; }
            set
            {
                duration = value;
                duration.X = Math.Max(duration.X, 0.001f);
                duration.Y = Math.Max(duration.Y, 0.001f);
            }
        }


        /// <summary>
        /// Restarts the spawner setting it to inactive state and elapsed time = 0
        /// </summary>
        internal virtual void ResetSimulation()
        {
            state = SpawnerState.Inactive;
            stateDuration = 0f;
        }


        /// <summary>
        /// Marking the spawner as dirty will notify the parent emitter that the maximum number of particles need to be recalculated
        /// </summary>
        protected void MarkAsDirty()
        {
            if (emitter != null)
            {
                emitter.DirtyParticlePool = true;
            }
        }

        /// <summary>
        /// Gets a next random float value from the random seed
        /// </summary>
        /// <returns>Random float value in the range [0..1)</returns>
        private float NextFloat()
        {
            return randomSeed.GetFloat(unchecked(randomOffset++));
        }

        /// <summary>
        /// Changes the spawners internal state (active, rest, etc.) to a new value.
        /// </summary>
        /// <param name="newState">The new state</param>
        private void SwitchToState(SpawnerState newState)
        {
            state = newState;

            if (state == SpawnerState.Active)
            {
                stateDuration = duration.X + (duration.Y - duration.X) * NextFloat();
            }
            else
            if (state == SpawnerState.Rest)
            {
                stateDuration = delay.X + (delay.Y - delay.X) * NextFloat();
            }
            else
            {
                stateDuration = 10f;
            }

            NotifyStateSwitch(newState);
        }

        /// <summary>
        /// Updates and gets the current internal state
        /// </summary>
        /// <param name="dt">Delta time in seconds since the last <see cref="GetUpdatedState"/> was called</param>
        /// <param name="emitter">Parent <see cref="ParticleEmitter"/> for this spawner</param>
        /// <returns></returns>
        protected SpawnerState GetUpdatedState(float dt, ParticleEmitter emitter)
        {
            // If this is the first time we activate the spawner add it to the emitter list and initialize its random seed
            if (this.emitter == null)
            {
                this.emitter = emitter;
                randomSeed = emitter.RandomSeedGenerator.GetNextSeed();
                emitter.DirtyParticlePool = true;
            }

            var remainingTime = dt;
            while (stateDuration <= remainingTime)
            {
                remainingTime -= stateDuration;

                switch (state)
                {
                    case SpawnerState.Inactive:
                        SwitchToState(SpawnerState.Rest);
                        break;

                    case SpawnerState.Rest:
                        SwitchToState(SpawnerState.Active);
                        break;

                    case SpawnerState.Dead:
                        SwitchToState(SpawnerState.Dead);
                        break;

                    case SpawnerState.Active:
                        if (LoopCondition == SpawnerLoopCondition.OneShot)
                        {
                            SwitchToState(SpawnerState.Dead);
                            break;
                        }
                        if (LoopCondition == SpawnerLoopCondition.Looping)
                        {
                            SwitchToState(SpawnerState.Rest);
                            break;
                        }

                        SwitchToState(SpawnerState.Active);
                        break;

                    default:
                        SwitchToState(SpawnerState.Dead);
                        break;
                }
            }

            stateDuration -= remainingTime;
            return state;
        }

        /// <summary>
        /// This method will be called form the emitter when it needs to poll how many particles to spawn (usually once per frame)
        /// </summary>
        /// <param name="dt">Time it has past since the last update (in seconds)</param>
        /// <param name="emitter">Parent emitter in which new particles should be emitter</param>
        public abstract void SpawnNew(float dt, ParticleEmitter emitter);

        /// <summary>
        /// Get the maximum number of particles this spawner can emit in one second
        /// </summary>
        /// <returns>Peak particles per second</returns>
        public abstract int GetMaxParticlesPerSecond();

        /// <summary>
        /// Invalidates relation of this emitter to any other emitters that might be referenced
        /// </summary>
        public virtual void InvalidateRelations()
        {

        }

        /// <summary>
        /// Will be called when the state changes. Override if you need to set/reset variables based on state changes
        /// </summary>
        /// <param name="newState">The new state</param>
        protected virtual void NotifyStateSwitch(SpawnerState newState)
        {
            
        }

    }
}
