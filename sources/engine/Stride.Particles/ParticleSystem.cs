// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Linq;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Particles.BoundingShapes;
using Stride.Particles.DebugDraw;
using Stride.Particles.Initializers;

namespace Stride.Particles
{
    [DataContract("ParticleSystem")]
    public class ParticleSystem : IDisposable
    {
        /// <summary>
        /// If positive, it indicates how much remaining time there is before the system calls Stop()
        /// </summary>
        private float timeout = 0f;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ParticleSystem"/> is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        [DataMember(-10)]
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Fixes local space location back to world space location. Used for debug drawing.
        /// </summary>
        /// <param name="translation">The locator's translation</param>
        /// <param name="rotation">The locator's quaternion rotation</param>
        /// <param name="scale">The locator's non-uniform scaling</param>
        /// <returns></returns>
        private bool ToWorldSpace(ref Vector3 translation, ref Quaternion rotation, ref Vector3 scale)
        {
            scale *= UniformScale;

            rotation *= Rotation;

            Rotation.Rotate(ref translation);
            translation *= UniformScale;
            translation += Translation;

            return true;
        }

        /// <summary>
        /// Tries to acquire and draw a debug shape for better feedback and visualization.
        /// </summary>
        /// <param name="debugDrawShape">The type of the debug shape (sphere, cone, etc.)</param>
        /// <param name="translation">The shape's translation</param>
        /// <param name="rotation">The shape's rotation</param>
        /// <param name="scale">The shape's non-uniform scaling</param>
        /// <returns><c>true</c> if debug shape can be displayed</returns>
        public bool TryGetDebugDrawShape(ref DebugDrawShape debugDrawShape, ref Vector3 translation, ref Quaternion rotation, ref Vector3 scale)
        {
            foreach (var particleEmitter in Emitters)
            {
                if (!particleEmitter.Enabled)
                    continue;

                foreach (var initializer in particleEmitter.Initializers)
                {
                    if (!initializer.Enabled)
                        continue;

                    if (initializer.TryGetDebugDrawShape(out debugDrawShape, out translation, out rotation, out scale))
                    {
                        // Convert to world space if local
                        if (particleEmitter.SimulationSpace == EmitterSimulationSpace.Local)
                            return ToWorldSpace(ref translation, ref rotation, ref scale);

                        return true;
                    }
                }

                foreach (var updater in particleEmitter.Updaters)
                {
                    if (!updater.Enabled)
                        continue;

                    if (updater.TryGetDebugDrawShape(out debugDrawShape, out translation, out rotation, out scale))
                    {
                        // Convert to world space if local
                        if (particleEmitter.SimulationSpace == EmitterSimulationSpace.Local)
                            return ToWorldSpace(ref translation, ref rotation, ref scale);

                        return true;
                    }
                }
            }

            if (BoundingShape == null)
                return false;

            if (BoundingShape.TryGetDebugDrawShape(out debugDrawShape, out translation, out rotation, out scale))
                return ToWorldSpace(ref translation, ref rotation, ref scale);

            return false;
        }

        /// <summary>
        /// Settings class which contains miscellaneous settings for the particle system
        /// </summary>
        /// <userdoc>
        /// Miscellaneous settings for the particle system. These settings are intended to be shared and are set during authoring of the particle system
        /// </userdoc>
        [DataMember(3)]
        [NotNull]
        [Display("Settings")]
        public ParticleSystemSettings Settings { get; set; } = new ParticleSystemSettings();

        /// <summary>
        /// AABB of this Particle System
        /// </summary>
        /// <userdoc>
        /// AABB (Axis-Aligned Bounding Box) used for fast culling and optimizations. Can be specified by the user. Leave it Null to disable culling.
        /// </userdoc>
        [DataMember(5)]
        [Display("Culling AABB")]
        public BoundingShape BoundingShape { get; set; } = null;

        /// <summary>
        /// Gets the current AABB of the <see cref="ParticleSystem"/>
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public BoundingBox GetAABB()
        {
            return BoundingShape?.GetAABB(Translation, Rotation, UniformScale) ?? new BoundingBox(Translation, Translation);
        }

        private int oldEmitterCount = 0;
        private readonly SafeList<ParticleEmitter> emitters;
        /// <summary>
        /// List of Emitters in this <see cref="ParticleSystem"/>. Each Emitter has a separate <see cref="ParticlePool"/> (group) of Particles in it
        /// </summary>
        /// <userdoc>
        /// List of emitters in this particle system. Each Emitter has a separate particle pool (group) of particles in it
        /// </userdoc>
        [DataMember(10)]
        [Display("Emitters")]
        //[NotNullItems] // This attribute is not supported for non-derived classes
        [MemberCollection(CanReorderItems = true)]
        public SafeList<ParticleEmitter> Emitters
        {
            get
            {
                return emitters;
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ParticleSystem()
        {
            emitters = new SafeList<ParticleEmitter>();
        }

        /// <summary>
        /// Translation of the ParticleSystem. Usually inherited directly from the ParticleSystemComponent or can be directly set.
        /// </summary>
        /// <userdoc>
        /// Translation of the ParticleSystem. Usually inherited directly from the ParticleSystemComponent or can be directly set.
        /// </userdoc>
        [DataMemberIgnore]
        public Vector3 Translation = Vector3.Zero;

        /// <summary>
        /// Rotation of the ParticleSystem, expressed as a quaternion rotation. Usually inherited directly from the ParticleSystemComponent or can be directly set.
        /// </summary>
        /// <userdoc>
        /// Rotation of the ParticleSystem, expressed as a quaternion rotation. Usually inherited directly from the ParticleSystemComponent or can be directly set.
        /// </userdoc>
        [DataMemberIgnore]
        public Quaternion Rotation = Quaternion.Identity;

        /// <summary>
        /// Scale of the ParticleSystem. Only uniform scale is supported. Usually inherited directly from the ParticleSystemComponent or can be directly set.
        /// </summary>
        /// <userdoc>
        /// Scale of the ParticleSystem. Only uniform scale is supported. Usually inherited directly from the ParticleSystemComponent or can be directly set.
        /// </userdoc>
        [DataMemberIgnore]
        public float UniformScale = 1f;


        /// <summary>
        /// Invalidates relation of this emitter to any other emitters that might be referenced
        /// </summary>
        public void InvalidateRelations()
        {
            // Setting the count to an invalid value will force validation update on the next step
            oldEmitterCount = -1;
        }

//        private Object thisLock = new Object();

        private void PreUpdate()
        {
//            lock (thisLock)
//            {
                foreach (var particleEmitter in Emitters)
                {
                    particleEmitter.PreUpdate();
                }
//            }
        }

        /// <summary>
        /// Updates the particles
        /// </summary>
        /// <param name="dt">Delta time - time, in seconds, elapsed since the last Update call to this particle system</param>
        /// <userdoc>
        /// Updates the particle system and all particles contained within. Delta time is the time, in seconds, which has passed since the last Update call.
        /// </userdoc>
        public void Update(float dt)
        {
            PreUpdate();

            if (timeout > 0f)
            {
                timeout -= dt;
                if (timeout <= 0f)
                {
                    Stop();
                }
            }

            // Check for changes in the emitters
            if (oldEmitterCount != Emitters.Count)
            {
                foreach (var particleEmitter in Emitters)
                {
                    particleEmitter.InvalidateRelations();
                }

                oldEmitterCount = Emitters.Count;
            }

            if (BoundingShape != null) BoundingShape.Dirty = true;

            // If the particle system is paused skip the rest of the update state
            if (isPaused)
            {
                foreach (var particleEmitter in Emitters)
                {
                    if (particleEmitter.Enabled)
                    {
                        particleEmitter.UpdatePaused(this);
                    }
                }

                return;
            }

            // If the particle system hasn't started yet do it now
            //  This includes warming up the system by simulating the emitters in background
            if (!hasStarted)
            {
                hasStarted = true;
                if (Settings.WarmupTime > 0)
                {
                    var remainingTime = Settings.WarmupTime;
                    var timeStep = 1f/30f;
                    while (remainingTime > 0)
                    {
                        var warmingUp = Math.Min(remainingTime, timeStep);

                        foreach (var particleEmitter in Emitters)
                        {
                            if (particleEmitter.Enabled)
                            {
                                particleEmitter.Update(warmingUp, this);
                            }
                        }

                        remainingTime -= warmingUp;
                    }
                    
                }
            }

            // Update all the emitters by delta time
            foreach (var particleEmitter in Emitters)
            {
                if (particleEmitter.Enabled)
                {
                    particleEmitter.Update(dt, this);
                }
            }            
        }

        /// <summary>
        /// Resets the particle system, resetting all values to their initial state
        /// </summary>
        public void ResetSimulation()
        {
            foreach (var particleEmitter in Emitters)
            {
                particleEmitter.ResetSimulation();
            }

            hasStarted = false;
        }

        [DataMemberIgnore]
        public bool IsPaused => isPaused;

        /// <summary>
        /// isPaused shows if the simulation progresses by delta time every frame or no
        /// </summary>
        private bool isPaused;

        /// <summary>
        /// hasStarted shows if the simulation has started yet or no
        /// </summary>
        private bool hasStarted;

        /// <summary>
        /// Pauses the particle system simulation
        /// </summary>
        public void Pause()
        {
            isPaused = true;
        }

        /// <summary>
        /// Use to both start a new simulation or continue a paused one
        /// </summary>
        public void Play()
        {
            isPaused = false;

            foreach (var particleEmitter in Emitters)
            {
                particleEmitter.CanEmitParticles = true;
            }
        }

        /// <summary>
        /// Stops the particle simulation by resetting it to its initial state and pausing it
        /// </summary>
        public void Stop()
        {
            ResetSimulation();
            isPaused = true;
        }

        /// <summary>
        /// Disables emission of new particles and sets a time limit on the system. After the time expires, the system stops.
        /// </summary>
        public void Timeout(float timeLimit)
        {
            if (timeLimit < 0f)
                return;

            foreach (var particleEmitter in Emitters)
            {
                particleEmitter.CanEmitParticles = false;
            }

            timeout = timeLimit;
        }

        /// <summary>
        /// Use to stop emitting new particles, but continue updating existing ones
        /// </summary>
        public void StopEmitters()
        {
            foreach (var particleEmitter in Emitters)
            {
                particleEmitter.CanEmitParticles = false;
            }
        }

        /// <summary>
        /// Gets the first emitter with matching name which is contained in this <see cref="ParticleSystem"/>
        /// </summary>
        /// <param name="name">Name of the emitter. Some emitters might not have a name and cannot be referenced</param>
        /// <returns><see cref="ParticleEmitter"/> with the same <see cref="ParticleEmitter.EmitterName"/> or <c>null</c> if not found</returns>
        public ParticleEmitter GetEmitterByName(string name)
        {
            return string.IsNullOrEmpty(name) ?
                null : 
                Emitters.FirstOrDefault(e => !string.IsNullOrEmpty(e.EmitterName) && e.EmitterName.Equals(name));
        }

        #region Dispose
        private bool disposed;

        ~ParticleSystem()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            disposed = true;

            // Dispose unmanaged resources

            if (!disposing)
                return;

            // Dispose managed resources
            foreach (var particleEmitter in Emitters)
            {
                particleEmitter.Dispose();
            }

            Emitters.Clear();
        }
        #endregion Dispose

    }
}
