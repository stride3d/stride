// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Particles.Sorters
{
    /// <summary>
    /// The <see cref="ParticleSorterLiving"/> collects all living particles, rather than sorting them
    /// It is useful for some pool policies, like Ring, which iterate over all particles, not only living ones
    /// </summary>
    public class ParticleSorterLiving : ParticleSorterCustom<float>, IParticleSorter
    {
        public ParticleSorterLiving(ParticlePool pool) : base(pool, ParticleFields.Life) { }

        public ParticleList GetSortedList(Vector3 depth)
        {
            var livingParticles = ParticlePool.LivingParticles;

            var lifeField = ParticlePool.GetField(fieldDesc);

            if (!lifeField.IsValid())
            {
                // Field is not valid - return an unsorted list
                return new ParticleList(ParticlePool, livingParticles);
            }

            SortedParticle[] particleList = ArrayPool.Allocate(ParticlePool.ParticleCapacity);

            var i = 0;
            foreach (var particle in ParticlePool)
            {
                if (particle.Get(lifeField) > 0)
                {
                    particleList[i] = new SortedParticle(particle, 0);
                    i++;
                }
            }

            livingParticles = i;
            return new ParticleList(ParticlePool, livingParticles, particleList);
        }

        /// <summary>
        /// In case an array was used it must be freed back to the pool
        /// </summary>
        /// <param name="sortedList">Reference to the <see cref="ParticleList"/> to be freed</param>
        public void FreeSortedList(ref ParticleList sortedList)
        {
            sortedList.Free(ArrayPool);
        }
    }
}
