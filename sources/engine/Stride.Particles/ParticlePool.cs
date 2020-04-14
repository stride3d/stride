// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Stride.Core;

namespace Stride.Particles
{
    /// <summary>
    /// The <see cref="ParticlePool"/> is a class which manages an unmanaged memory block used for the particles.
    /// The maximum required size calculated on the number of particles and their fields' sizes is calculated every time the sizes or the count change
    /// </summary>
    public class ParticlePool : IDisposable, IEnumerable
    {             
        public delegate void CopyParticlePoolDelegate(IntPtr oldPool, int oldCapacity, int oldSize, IntPtr newPool, int newCapacity, int newSize);

        public enum ListPolicy
        {
            /// <summary>
            /// New particles are allocated from the next free index which loops when it reaches the end of the list.
            /// The pool doesn't care about dead particles - they don't move and get overwritten by new particles.
            /// </summary>
            Ring,

            /// <summary>
            /// New particles are allocated at the top of the stack. Dead particles are swapped out with the top particle.
            /// The stack stays small but order of the particles gets scrambled.
            /// </summary>
            Stack

            // OrderedStack,
            // DynamicStack            
        }

        private readonly ListPolicy listPolicy;

        /// <summary>
        /// For ring implementations, the index just increases, looping when it reaches max count.
        /// For stack implementations, the index points to the top of the stack and can reach 0 when there are no living particles.
        /// </summary>
        private int nextFreeIndex;

        private bool disposed;

        public const int DefaultMaxFielsPerPool = 16;

        private readonly Dictionary<ParticleFieldDescription, ParticleField> fields = new Dictionary<ParticleFieldDescription, ParticleField>(DefaultMaxFielsPerPool);

        private readonly List<ParticleFieldDescription> fieldDescriptions = new List<ParticleFieldDescription>(DefaultMaxFielsPerPool);



        /// <summary>
        /// <see cref="ParticlePool"/> constructor
        /// </summary>
        /// <param name="size">Initial size in bytes of a single particle</param>
        /// <param name="capacity">Initial capacity (maximum number of particles) of the pool</param>
        /// <param name="listPolicy">List policy - stack (living particles are in the front) or ring</param>
        public ParticlePool(int size, int capacity, ListPolicy listPolicy = ListPolicy.Stack)
        {
            this.listPolicy = listPolicy;

            nextFreeIndex = 0;

            ReallocatePool(size, capacity, (pool, oldCapacity, oldSize, newPool, newCapacity, newSize) => { });
        }



        /// <summary>
        /// <see cref="NextFreeIndex"/> points to the next index ready for allocation, between 0 and <see cref="ParticleCapacity"/> - 1.
        /// In case of stack list the <see cref="NextFreeIndex"/> equals the number of living particles in the pool.
        /// </summary>
        public int NextFreeIndex => nextFreeIndex;

        /// <summary>
        /// Returns the size of a single particle.
#if PARTICLES_SOA
        /// The size of the <see cref="Particle"/> equals the sum of all fields' strides.
#else
        /// The size of the <see cref="Particle"/> equals the pool's stride.
#endif
        /// </summary>
        public int ParticleSize { get; private set; }

        /// <summary>
        /// Gets how many more particles can be spawned
        /// </summary>
        public int AvailableParticles
        {
            get
            {
                if (listPolicy == ListPolicy.Ring)
                    return ParticleCapacity;

                return (ParticleCapacity - nextFreeIndex);
            }
        }

        /// <summary>
        /// Get the number of living (active) particles
        /// </summary>
        public int LivingParticles
        {
            get
            {
                if (listPolicy == ListPolicy.Ring)
                    return ParticleCapacity;

                return (nextFreeIndex);
            }
        }

        /// <summary>
        /// ParticleData is where the memory block (particle pool) actually resides.
        /// Its size equals <see cref="ParticleSize"/> * <see cref="ParticleCapacity"/>
        /// </summary>
        public IntPtr ParticleData { get; private set; } = IntPtr.Zero;

        /// <summary>
        /// The maximum allowed number of particles in this <see cref="ParticlePool"/>.
        /// Use <see cref="SetCapacity"/> if you need to change it.
        /// </summary>
        public int ParticleCapacity { get; private set; }



        /// <summary>
        /// Set a different capacity (maximum <see cref="Particle"/> count for this pool)
        /// Whenever possible, existing particles will be copied and continue simulation
        /// </summary>
        /// <param name="newCapacity">New maximum capacity</param>
        public void SetCapacity(int newCapacity)
        {
            if (newCapacity < 0 || newCapacity == ParticleCapacity)
                return;

            if (nextFreeIndex > newCapacity)
                nextFreeIndex = newCapacity;

            ReallocatePool(ParticleSize, newCapacity, ReallocateForCapacityChanged);
        }

        /// <summary>
        /// Moves the particle pool data to a new location, copying existing particles where possible
        /// </summary>
        /// <param name="newSize">New size for a single particle</param>
        /// <param name="newCapacity">New capacity (maximum number of particles)</param>
        /// <param name="poolCopy">Method to use for copying the particle data</param>
        private void ReallocatePool(int newSize, int newCapacity, CopyParticlePoolDelegate poolCopy)
        {
            if (newCapacity == ParticleCapacity && newSize == ParticleSize)
                return;

            var newParticleData = IntPtr.Zero;

            var newMemoryBlockSize = newCapacity * newSize;

            if (newMemoryBlockSize > 0)
                newParticleData = Utilities.AllocateMemory(newMemoryBlockSize);

            if (ParticleData != IntPtr.Zero && newParticleData != IntPtr.Zero)
            {
                poolCopy(ParticleData, ParticleCapacity, ParticleSize, newParticleData, newCapacity, newSize);

                Utilities.FreeMemory(ParticleData);
            }

            ParticleData = newParticleData;
            ParticleCapacity = newCapacity;
            ParticleSize = newSize;

            RecalculateFieldsArrays();
        }

        /// <summary>
        /// Clears all particle fields, but keeps the particle capacity the same.
        /// </summary>
        public void Reset()
        {
            fields.Clear();
            fieldDescriptions.Clear();
            ReallocatePool(0, ParticleCapacity, (pool, oldCapacity, oldSize, newPool, newCapacity, newSize) => { });
        }

        #region Dispose

        ~ParticlePool()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void DisposeParticleData()
        {
            if (ParticleData == IntPtr.Zero)
                return;

            Utilities.FreeMemory(ParticleData);
            ParticleData = IntPtr.Zero;
            ParticleCapacity = 0;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            disposed = true;

            // Dispose unmanaged resources
            DisposeParticleData();

            if (!disposing)
                // ReSharper disable once RedundantJumpStatement
                return;

            // Dispose managed resources
        }

        #endregion Dispose


        /// <summary>
        /// Copy data from one particle index to another
        /// </summary>
        /// <param name="dst">Index of the destination particle</param>
        /// <param name="src">Index of the source particle</param>
        private void CopyParticleData(int dst, int src)
        {
            var dstParticle = FromIndex(dst);
            var srcParticle = FromIndex(src);

#if PARTICLES_SOA
            foreach (var field in fields.Values)
            {
                var accessor = new ParticleFieldAccessor(field);
                Utilities.CopyMemory(dstParticle[accessor], srcParticle[accessor], field.Size);
            }
#else
            Utilities.CopyMemory(dstParticle, srcParticle, ParticleSize);
#endif
        }

        /// <summary>
        /// Get a particle from its index in the pool
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
#if PARTICLES_SOA
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public Particle FromIndex(int idx)
        {
            return new Particle(idx);
        }
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Particle FromIndex(int idx)
        {
            return new Particle(ParticleData + idx * ParticleSize);
        }
#endif

        /// <summary>
        /// Add a new particle to the pool. Doesn't worry about initialization.
        /// </summary>
        /// <returns></returns>
        public Particle AddParticle()
        {
            if (nextFreeIndex != ParticleCapacity)
            {
                return FromIndex(nextFreeIndex++);
            }

            if (listPolicy != ListPolicy.Ring || ParticleCapacity == 0)
                return Particle.Invalid();

            nextFreeIndex = 0;
            return FromIndex(nextFreeIndex++);
        }

        /// <summary>
        /// Removes the current particle
        /// </summary>
        /// <param name="particle">Reference to the particle being removed</param>
        /// <param name="oldIndex">Reference to the old current index so we can reposition it to point before the particle</param>
        /// <param name="indexMax">Maximum index value for this pool</param>
        private void RemoveCurrent(ref Particle particle, ref int oldIndex, ref int indexMax)
        {
            // In case of a Ring list we don't bother to remove dead particles
            if (listPolicy == ListPolicy.Ring)
                return;
            
            // Next free index shouldn't be 0 because we are removing a particle
            Debug.Assert(nextFreeIndex > 0);

            // Update the top index since the list is shorter now
            --nextFreeIndex;
            if (indexMax != oldIndex)
                CopyParticleData(oldIndex, indexMax);
            indexMax = nextFreeIndex - 1;

            particle = FromIndex(indexMax);        

            // We need to position the cursor of the enumerator to the previous particle, so that enumeration works fine
            oldIndex--;            
        }

#region Fields

        /// <summary>
        /// Recalculates the fields' offsets and strides. The methods are different for SoA and AoS policies
        /// </summary>
        private void RecalculateFieldsArrays()
        {
#if PARTICLES_SOA
            var fieldOffset = 0;
            foreach (var desc in fieldDescriptions)
            {
                var fieldSize = fields[desc].Size;
                fields[desc] = new ParticleField(fieldSize, ParticleData + fieldOffset * ParticleCapacity);
                fieldOffset += fieldSize;
            }
#else
            var fieldOffset = 0;
            foreach (var desc in fieldDescriptions)
            {
                fields[desc] = new ParticleField() { Offset = fieldOffset, Size = desc.FieldSize };
                fieldOffset += desc.FieldSize;
            }
#endif
        }

        /// <summary>
        /// Adds a particle field to this pool with the specified description, or gets an existing one
        /// </summary>
        /// <param name="fieldDesc">Description of the field</param>
        /// <returns>The newly added or already existing field</returns>
        internal ParticleField AddField(ParticleFieldDescription fieldDesc)
        {
            // Fields with a stride of 0 are meaningless and cannot be added or removed
            if (fieldDesc.FieldSize == 0)
            {
                return new ParticleField();
            }

            ParticleField existingField;
            if (fields.TryGetValue(fieldDesc, out existingField))
                return existingField;


            var newParticleSize = ParticleSize + fieldDesc.FieldSize;

#if PARTICLES_SOA
            var newField = new ParticleField(fieldDesc.FieldSize, IntPtr.Zero);
#else
            var newField = new ParticleField() { Offset = ParticleSize, Size = fieldDesc.FieldSize };
#endif

            fieldDescriptions.Add(fieldDesc);
            fields.Add(fieldDesc, newField);

            ReallocatePool(newParticleSize, ParticleCapacity, ReallocateForFieldAdded);

            return fields[fieldDesc];
        }

        /// <summary>
        /// Reallocate the particles to a new memory block due to a newly added field
        /// </summary>
        /// <param name="oldPool">Old memory block</param>
        /// <param name="oldCapacity">Old capacity (maximum particle count) of the block</param>
        /// <param name="oldSize">Old size of a single particle</param>
        /// <param name="newPool">New memory block</param>
        /// <param name="newCapacity">New capacity (maximum particle count) of the block</param>
        /// <param name="newSize">New size of a single particle</param>
        private void ReallocateForFieldAdded(IntPtr oldPool, int oldCapacity, int oldSize, IntPtr newPool, int newCapacity, int newSize)
        {
            // Old particle capacity and new particle capacity should be the same when only the size changes.
            // If this is not the case, something went wrong. Reset the particle count and do not copy.
            // Also, since we are adding a field, the new particle size is expected to get bigger.
            if (oldCapacity != newCapacity || newCapacity <= 0 || newSize <= 0 || oldPool == IntPtr.Zero || newPool == IntPtr.Zero || oldSize >= newSize)
            {
                nextFreeIndex = 0;
                return;
            }

#if PARTICLES_SOA
            // Easy case - the new field is added to the end. Copy the existing memory block into the new one
            Utilities.CopyMemory(newPool, oldPool, oldSize * oldCapacity);
            Utilities.ClearMemory(newPool + oldSize * oldCapacity, 0, (newSize - oldSize) * oldCapacity);
#else
            // Clear the memory first instead of once per particle
            Utilities.ClearMemory(newPool, 0, newSize * newCapacity);

            // Complex case - needs to copy the head of each particle
            for (var i = 0; i < oldCapacity; i++)
            {
                Utilities.CopyMemory(newPool + i * newSize, oldPool + i * oldSize, oldSize);
            }
#endif
        }

        /// <summary>
        /// Reallocate the particles to a new memory block due to a change in the pool's capacity
        /// </summary>
        /// <param name="oldPool">Old memory block</param>
        /// <param name="oldCapacity">Old capacity (maximum particle count) of the block</param>
        /// <param name="oldSize">Old size of a single particle</param>
        /// <param name="newPool">New memory block</param>
        /// <param name="newCapacity">New capacity (maximum particle count) of the block</param>
        /// <param name="newSize">New size of a single particle</param>
        private void ReallocateForCapacityChanged(IntPtr oldPool, int oldCapacity, int oldSize, IntPtr newPool, int newCapacity, int newSize)
        {
            // Old particle size and new particle size should be the same when only the capacity changes.
            // If this is not the case, something went wrong. Reset the particle count and do not copy.
            if (oldSize != newSize || newCapacity <= 0 || newSize <= 0 || oldPool == IntPtr.Zero || newPool == IntPtr.Zero)
            {
                nextFreeIndex = 0;
                return;
            }

            if (nextFreeIndex > newCapacity)
                nextFreeIndex = newCapacity;

#if PARTICLES_SOA
            // Clear the memory first instead of once per particle
            Utilities.ClearMemory(newPool, 0, newSize * newCapacity);

            var oldOffset = 0;
            var newOffset = 0;

            // Fields haven't changed so we can iterate them. In case of Add/Remove fields you shouldn't use this
            foreach (var field in fields.Values)
            {                
                var copySize = Math.Min(oldCapacity, newCapacity) * field.Size;
                Utilities.CopyMemory(newPool + newOffset, oldPool + oldOffset, copySize);

                oldOffset += (field.Size * oldCapacity);
                newOffset += (field.Size * newCapacity);
            }
#else
            if (newCapacity > oldCapacity)
            {
                Utilities.CopyMemory(newPool, oldPool, newSize * oldCapacity);
                Utilities.ClearMemory(newPool + newSize * oldCapacity, 0, newSize * (newCapacity - oldCapacity));
            }
            else
            {
                Utilities.CopyMemory(newPool, oldPool, newSize * newCapacity);
            }
#endif
        }

        /// <summary>
        /// Removes a particle field from this pool with the specified description, or gets an existing one
        /// </summary>
        /// <param name="fieldDesc">Description of the field</param>
        /// <returns><c>true</c> if the field was successfully removed, <c>false</c> otherwise</returns>
        public bool RemoveField(ParticleFieldDescription fieldDesc)
        {
            // Fields with a stride of 0 are meaningless and cannot be added or removed
            if (fieldDesc.FieldSize == 0)
            {
                return false;
            }

            // Check if the field exists in this particle pool. If it doesn't, obviously it cannot be removed
            ParticleField existingField;
            if (!fields.TryGetValue(fieldDesc, out existingField))
                return false;

            var newParticleSize = ParticleSize - fieldDesc.FieldSize;

            fieldDescriptions.Remove(fieldDesc);

#if PARTICLES_SOA
            fields[fieldDesc] = new ParticleField(0, IntPtr.Zero);
#else
            fields[fieldDesc] = new ParticleField() { Offset = 0, Size = 0 };
#endif

            // The field is not removed yet. During relocation it will appear as having Size and Offset of 0, and should be ignored for the purpose of copying memory
            ReallocatePool(newParticleSize, ParticleCapacity, ReallocateForFieldRemoved);

            fields.Remove(fieldDesc);

            return true;
        }

        /// <summary>
        /// Reallocate the particles to a new memory block due to a deleted field
        /// </summary>
        /// <param name="oldPool">Old memory block</param>
        /// <param name="oldCapacity">Old capacity (maximum particle count) of the block</param>
        /// <param name="oldSize">Old size of a single particle</param>
        /// <param name="newPool">New memory block</param>
        /// <param name="newCapacity">New capacity (maximum particle count) of the block</param>
        /// <param name="newSize">New size of a single particle</param>
        private void ReallocateForFieldRemoved(IntPtr oldPool, int oldCapacity, int oldSize, IntPtr newPool, int newCapacity, int newSize)
        {
            // Old particle capacity and new particle capacity should be the same when only the size changes.
            // If this is not the case, something went wrong. Reset the particle count and do not copy.
            // Also, since we are re3moving a field, the new particle size is expected to get smaller.
            if (oldCapacity != newCapacity || newCapacity <= 0 || newSize <= 0 || oldPool == IntPtr.Zero || newPool == IntPtr.Zero || oldSize <= newSize)
            {
                nextFreeIndex = 0;
                return;
            }

#if PARTICLES_SOA
            // Copy each field individually except the field which is being removed (we have marked it with a stride of 0 already)

            var fieldOffset = 0;
            foreach (var field in fields.Values)
            {
                // This is the field which we have marked - do not copy it
                if (field.Size == 0 || field.Offset == IntPtr.Zero)
                    continue;

                Utilities.CopyMemory(newPool + fieldOffset, field.Offset, field.Size * ParticleCapacity);

                fieldOffset += field.Size * ParticleCapacity;
            }
#else
            // Clear the memory first instead of once per particle
            Utilities.ClearMemory(newPool, 0, newSize * newCapacity);

            // Complex case - needs to copy up to two parts of each particle
            var firstHalfSize = 0;
            var secondHalfSize = 0;
            var isSecondHalf = false;
            foreach (var field in fields.Values)
            {
                if (field.Size == 0)
                {
                    isSecondHalf = true;
                    continue;
                }

                if (isSecondHalf)
                {
                    secondHalfSize += field.Size;
                }
                else
                {
                    firstHalfSize += field.Size;
                }                
            }

            if (firstHalfSize > 0)
            {
                for (var i = 0; i < oldCapacity; i++)
                {
                    Utilities.CopyMemory(newPool + i * newSize, oldPool + i * oldSize, firstHalfSize);
                }
            }

            if (secondHalfSize > 0)
            {
                var secondHalfOffset = oldSize - secondHalfSize;

                for (var i = 0; i < oldCapacity; i++)
                {
                    Utilities.CopyMemory(newPool + i * newSize + firstHalfSize, oldPool + i * oldSize + secondHalfOffset, secondHalfSize);
                }
            }
#endif

        }

        /// <summary>
        /// Unsafe method for getting a <see cref="ParticleFieldAccessor"/>.
        /// If the field doesn't exist an invalid accessor is returned to the user.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldDesc"></param>
        /// <returns></returns>
        public ParticleFieldAccessor<T> GetField<T>(ParticleFieldDescription<T> fieldDesc) where T : struct
        {
            ParticleField field;
            return fields.TryGetValue(fieldDesc, out field) ?
                new ParticleFieldAccessor<T>(field) :
                ParticleFieldAccessor<T>.Invalid();
        }

        /// <summary>
        /// Gets the particle field with the specified description if the field exists in this pool
        /// </summary>
        /// <typeparam name="T">Type data for the field</typeparam>
        /// <param name="fieldDesc">Field's decription</param>
        /// <param name="accessor">Accessor for the field</param>
        /// <returns></returns>
        public bool TryGetField<T>(ParticleFieldDescription<T> fieldDesc, out ParticleFieldAccessor<T> accessor) where T : struct
        {
            ParticleField field;
            if (!fields.TryGetValue(fieldDesc, out field))
            {
                accessor = ParticleFieldAccessor<T>.Invalid();
                return false;
            }

            accessor = new ParticleFieldAccessor<T>(field);
            return true;
        }

        /// <summary>
        /// Polls if a filed with this description exists in the pool and optionally forces creation of a new field
        /// </summary>
        /// <param name="fieldDesc">Description of the field</param>
        /// <param name="forceCreate">Force the creation of non-existing fields if <c>true</c></param>
        /// <returns></returns>
        public bool FieldExists(ParticleFieldDescription fieldDesc, bool forceCreate = false)
        {
            ParticleField field;
            if (fields.TryGetValue(fieldDesc, out field))
                return true;

            if (!forceCreate)
                return false;

            if (fields.Count >= DefaultMaxFielsPerPool)
                return false;

            AddField(fieldDesc);
            return true;
        }
#endregion

#region Enumerator
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns an <see cref="Enumerator"/> to the particles in this <see cref="ParticlePool"/>
        /// In case of <see cref="ListPolicy.Ring"/> dead particles are returned too, so the calling entity should handle such cases.
        /// </summary>
        /// <returns></returns>
        public Enumerator GetEnumerator()
        {
            return (listPolicy == ListPolicy.Ring) ? 
                new Enumerator(this) :
                new Enumerator(this, 0, nextFreeIndex - 1);
        }

        public struct Enumerator : IEnumerator<Particle>
        {
#if PARTICLES_SOA
#else
            private IntPtr particlePtr;
            private readonly int particleSize;
#endif
            private int index;

            private readonly ParticlePool particlePool;
            private readonly int indexFrom;

            // indexTo can change if particles are removed during iteration
            private int indexTo;

            /// <summary>
            /// Creates an enumarator which iterates over all particles (living and dead) in the particle pool.
            /// </summary>
            /// <param name="particlePool">Particle pool to iterate</param>
            public Enumerator(ParticlePool particlePool)
            {
                this.particlePool = particlePool;
#if PARTICLES_SOA
#else
                particleSize = particlePool.ParticleSize;
                particlePtr = IntPtr.Zero;
#endif
                indexFrom = 0;
                indexTo = particlePool.ParticleCapacity - 1;
                index = indexFrom - 1;
            }

            /// <summary>
            /// <see cref="Enumerator"/> to the particles in this <see cref="ParticlePool"/>
            /// </summary>
            /// <param name="particlePool">Particle pool to iterate</param>
            /// <param name="idxFrom">First valid particle index</param>
            /// <param name="idxTo">Last valid particle index</param>
            public Enumerator(ParticlePool particlePool, int idxFrom, int idxTo)
            {
                this.particlePool = particlePool;
#if PARTICLES_SOA
#else
                particleSize = particlePool.ParticleSize;
                particlePtr = IntPtr.Zero;
#endif
                indexFrom = Math.Max(0, idxFrom);
                indexTo = Math.Min(particlePool.ParticleCapacity - 1, idxTo);
                index = indexFrom - 1;
            }

            /// <inheritdoc />
            public void Dispose()
            {                
            }

            /// <inheritdoc />
            public void Reset()
            {
                index = indexFrom - 1;
#if PARTICLES_SOA
#else
                particlePtr = IntPtr.Zero;
#endif
            }

            /// <inheritdoc />
            public bool MoveNext()
            {
                ++index;
                var hasNext = (index <= indexTo && index >= indexFrom);
#if PARTICLES_SOA
#else
                particlePtr = (hasNext) ? particlePool.ParticleData + index * particleSize : IntPtr.Zero;
#endif
                return hasNext;
            }

            /// <summary>
            /// Removes the current particle. A reference to the particle is required so that the addressing can be updated and prevent illegal access.
            /// </summary>
            /// <param name="particle">Reference to the particle being removed</param>
            public void RemoveCurrent(ref Particle particle)
            {
                // Cannot remove particle which is not current
                if (particle != Current)
                    return;

                particlePool.RemoveCurrent(ref particle, ref index, ref indexTo);
            }

#if PARTICLES_SOA
            /// <inheritdoc />
            object IEnumerator.Current => new Particle(index);

            /// <inheritdoc />
            public Particle Current => new Particle(index);
#else
            /// <inheritdoc />
            object IEnumerator.Current => new Particle(particlePtr);

            /// <inheritdoc />
            public Particle Current => new Particle(particlePtr);

#endif

        }
#endregion


    }

    public struct ParticlePoolFieldsList
    {
        private readonly ParticlePool particlePool;

        public ParticlePoolFieldsList(ParticlePool pool)
        {
            particlePool = pool;
        }

        /// <summary>
        /// Returns a particle field accessor for the contained <see cref="ParticlePool"/>
        /// </summary>
        /// <typeparam name="T">Type data for the field</typeparam>
        /// <param name="fieldDesc">The field description</param>
        /// <returns></returns>
        public ParticleFieldAccessor<T> GetField<T>(ParticleFieldDescription<T> fieldDesc) where T : struct
        {
            return particlePool.GetField<T>(fieldDesc);
        }
    }
}
