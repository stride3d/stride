// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;

namespace Stride.Particles
{
    /// <summary>
    /// The most basic unit of a <see cref="ParticleSystem"/>
    /// You can access individual fields with a <see cref="ParticleFieldAccessor"/>
    /// </summary>
    public struct  Particle
    {
#if PARTICLES_SOA
        public readonly int Index;

        public Particle(int index)
        {
            Index = index;
        }

        /// <summary>
        /// Creates an invalid <see cref="Particle"/>. Accessing the invalid <see cref="Particle"/> is not resticted by the engine.
        /// </summary>
        /// <returns></returns>
        static internal Particle Invalid()
        {
            return new Particle(int.MaxValue);
        }
#else
        /// <summary>
        /// Pointer to the particle data block
        /// </summary>
        public readonly IntPtr Pointer;

        /// <summary>
        /// Creates a particle from a raw pointer, assuming the pointer references valid particle data block
        /// </summary>
        /// <param name="pointer"></param>
        public Particle(IntPtr pointer)
        {
            Pointer = pointer;
        }

        /// <summary>
        /// Creates an invalid <see cref="Particle"/>. Accessing the invalid <see cref="Particle"/> is not resticted by the engine.
        /// </summary>
        /// <returns></returns>
        static internal Particle Invalid()
        {
            return new Particle(IntPtr.Zero);
        }
#endif

        #region Accessors

        /// <summary>
        /// Gets the particle's field value. However, you should try to use the indexer wherever possible.
        /// </summary>
        /// <typeparam name="T">The field type.</typeparam>
        /// <param name="accessor">The field accessor</param>
        /// <returns>The field value.</returns>
        public T Get<T>(ParticleFieldAccessor<T> accessor) where T : struct
        {
#if PARTICLES_SOA
            return Utilities.Read<T>(accessor[Index]);
#else
            return Utilities.Read<T>(Pointer + accessor);
#endif
        }

        /// <summary>
        /// Sets the particle's field to a value. However, you should try to use the indexer wherever possible.
        /// </summary>
        /// <typeparam name="T">The field type.</typeparam>
        /// <param name="accessor">The field accessor</param>
        /// <param name="value">The value to set</param>
        public void Set<T>(ParticleFieldAccessor<T> accessor, ref T value) where T : struct
        {
#if PARTICLES_SOA
            Utilities.Write(accessor[Index], ref value);
#else
            Utilities.Write(Pointer + accessor, ref value);
#endif
        }

        /// <summary>
        /// Sets the particle's field to a value. However, you should try to use the indexer wherever possible.
        /// </summary>
        /// <typeparam name="T">The field type.</typeparam>
        /// <param name="accessor">The field accessor</param>
        /// <param name="value">The value to set</param>
        public void Set<T>(ParticleFieldAccessor<T> accessor, T value) where T : struct
        {
#if PARTICLES_SOA
            Utilities.Write(accessor[Index], ref value);
#else
            Utilities.Write(Pointer + accessor, ref value);
#endif
        }

        #endregion

#if PARTICLES_SOA
        public IntPtr this[ParticleFieldAccessor accessor] => accessor[Index];

        public static implicit operator int(Particle particle) => particle.Index;
#else
        public static implicit operator IntPtr(Particle particle) => particle.Pointer;

        public IntPtr this[ParticleFieldAccessor accessor] => Pointer + accessor;
#endif


        public override bool Equals(object other)
        {
            if (other == null)
                return false;

            return (this == (Particle)other);
        }


#if PARTICLES_SOA
        /// <summary>
        /// Since particles are only indices, the comparison is only meaningful if it's done within the same particle pool
        /// </summary>
        /// <param name="particleLeft">Left side particle to compare</param>
        /// <param name="particleRight">Right side particle to compare</param>
        /// <returns></returns>
        public static bool operator ==(Particle particleLeft, Particle particleRight) => (particleLeft.Index == particleRight.Index);
        public static bool operator !=(Particle particleLeft, Particle particleRight) => (particleLeft.Index != particleRight.Index);

        public override int GetHashCode()
        {
            return Index;
        }
#else
        /// <summary>
        /// Checks if the two particles point to the same pointer.
        /// </summary>
        /// <param name="particleLeft">Left side particle to compare</param>
        /// <param name="particleRight">Right side particle to compare</param>
        /// <returns></returns>
        public static bool operator ==(Particle particleLeft, Particle particleRight) => (particleLeft.Pointer == particleRight.Pointer);
        public static bool operator !=(Particle particleLeft, Particle particleRight) => (particleLeft.Pointer != particleRight.Pointer);

        public override int GetHashCode()
        {
            return Pointer.ToInt32();
        }
#endif
    }
}
