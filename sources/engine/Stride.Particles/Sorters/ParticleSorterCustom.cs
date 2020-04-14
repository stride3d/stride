// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace Stride.Particles.Sorters
{
    /// <summary>
    /// The custom sorter uses a user-defined method for generating sort index from a user-defined field
    /// </summary>
    public abstract class ParticleSorterCustom<T> where T : struct
    {
        protected readonly ParticleFieldDescription<T> fieldDesc;

        protected readonly ConcurrentArrayPool<SortedParticle> ArrayPool = new ConcurrentArrayPool<SortedParticle>();

        protected readonly ParticlePool ParticlePool;

        protected ParticleSorterCustom(ParticlePool pool, ParticleFieldDescription<T> fieldDesc)
        {
            ParticlePool = pool;
            this.fieldDesc = fieldDesc;
        }
    }

    public interface ISortValueCalculator<T> where T : struct
    {
        float GetSortValue(T value);
    }

    public struct Enumerator : IEnumerator<Particle>
    {
        private readonly SortedParticle[] sortedList;
        private readonly int listCapacity;

        private int index;

        internal Enumerator(SortedParticle[] list, int capacity)
        {
            sortedList = list;
            listCapacity = capacity;
            index = -1;
        }

        /// <inheritdoc />
        public void Reset()
        {
            index = -1;
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            return (++index < listCapacity);
        }

        /// <inheritdoc />
        public void Dispose()
        {            
        }

        public Particle Current => sortedList[index].Particle;

        object IEnumerator.Current => sortedList[index].Particle;
    }
}
