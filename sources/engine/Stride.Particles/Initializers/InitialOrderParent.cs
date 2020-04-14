// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Particles.Spawners;

namespace Stride.Particles.Initializers
{
    /// <summary>
    /// The <see cref="InitialOrderParent"/> is an initializer which sets the particle's spawn order based on a followed (parent) particle's order
    /// </summary>
    [DataContract("InitialOrderParent")]
    [Display("Spawn Order (parent)")]
    public class InitialOrderParent : ParticleChildInitializer
    {
        // Will loop every so often, but the loop condition should be unreachable for normal games (~800 hours for spawning rate of 100 particles/second)
        private uint spawnOrder;

        /// <inheritdoc />
        public override void ResetSimulation()
        {
            spawnOrder = 0;
        }

        /// <summary>
        /// Default constructor which also registers the fields required by this updater
        /// </summary>
        public InitialOrderParent()
        {
            spawnOrder = 0;

            RequiredFields.Add(ParticleFields.Order);
        }

        /// <inheritdoc />
        public unsafe override void Initialize(ParticlePool pool, int startIdx, int endIdx, int maxCapacity)
        {
            if (!pool.FieldExists(ParticleFields.Order))
                return;

            // Collect the total number of living particles in the parent pool which have a Order field
            var parentPool = Parent?.Pool;
            var parentParticlesCount = parentPool?.LivingParticles ?? 0;
            var orderFieldParent = parentPool?.GetField(ParticleFields.Order) ?? ParticleFieldAccessor<uint>.Invalid();
            var childOrderFieldParent = parentPool?.GetField(ParticleFields.ChildOrder) ?? ParticleFieldAccessor<uint>.Invalid();
            if (!orderFieldParent.IsValid())
            {
                parentParticlesCount = 0;
            }

            var spawnControlField = GetSpawnControlField();

            var orderField = pool.GetField(ParticleFields.Order);
            var randomField = pool.GetField(ParticleFields.RandomSeed);

            var sequentialParentIndex = 0;
            var sequentialParentParticles = 0;
            var parentIndex = 0;

            var i = startIdx;
            while (i != endIdx)
            {
                var particle = pool.FromIndex(i);

                // Will loop every so often, but the loop condition should be unreachable for normal games
                uint particleOrder = spawnOrder++;

                if (parentParticlesCount > 0)
                {
                    uint parentParticleOrder = 0;

                    // Spawn is fixed - parent particles have spawned a very specific number of children each
                    if (spawnControlField.IsValid())
                    {
                        while (sequentialParentParticles == 0)
                        {
                            // Early out - no more fixed number children. Rest of the particles (if any) are skipped intentionally
                            if (sequentialParentIndex >= parentParticlesCount)
                                return;

                            parentIndex = sequentialParentIndex;
                            var tempParentParticle = parentPool.FromIndex(parentIndex);
                            sequentialParentIndex++;

                            var childrenAttribute = (*((ParticleChildrenAttribute*)tempParentParticle[spawnControlField]));

                            sequentialParentParticles = (int)childrenAttribute.ParticlesToEmit;
                        }

                        sequentialParentParticles--;

                        var parentParticle = parentPool.FromIndex(parentIndex);
                        parentParticleOrder = (*((uint*)parentParticle[orderFieldParent]));

                        if (childOrderFieldParent.IsValid())
                        {
                            particleOrder = (*((uint*)parentParticle[childOrderFieldParent]));
                            (*((uint*)parentParticle[childOrderFieldParent])) = (particleOrder + 1);

                            particleOrder = (particleOrder & SpawnOrderConst.AuxiliaryBitMask) | ((parentParticleOrder << SpawnOrderConst.GroupBitOffset) & SpawnOrderConst.GroupBitMask);
                        }
                        else
                        {
                            particleOrder = (particleOrder & SpawnOrderConst.LargeAuxiliaryBitMask) | ((parentParticleOrder << SpawnOrderConst.LargeGroupBitOffset) & SpawnOrderConst.LargeGroupBitMask);
                        }
                    }

                    // Spawn is not fixed - pick a parent at random
                    else
                    {
                        var randSeed = particle.Get(randomField);

                        parentIndex = (int)(parentParticlesCount * randSeed.GetFloat(RandomOffset.Offset1A + ParentSeedOffset));

                        var parentParticle = parentPool.FromIndex(parentIndex);
                        parentParticleOrder = (*((uint*)parentParticle[orderFieldParent]));

                        if (childOrderFieldParent.IsValid())
                        {
                            particleOrder = (*((uint*)parentParticle[childOrderFieldParent]));
                            (*((uint*)parentParticle[childOrderFieldParent])) = (particleOrder + 1);

                            particleOrder = (particleOrder & SpawnOrderConst.AuxiliaryBitMask) | ((parentParticleOrder << SpawnOrderConst.GroupBitOffset) & SpawnOrderConst.GroupBitMask);
                        }
                        else
                        {
                            particleOrder = (particleOrder & SpawnOrderConst.LargeAuxiliaryBitMask) | ((parentParticleOrder << SpawnOrderConst.LargeGroupBitOffset) & SpawnOrderConst.LargeGroupBitMask);
                        }
                    }
                }

                (*((uint*)particle[orderField])) = particleOrder;

                i = (i + 1) % maxCapacity;
            }
        }

        /// <inheritdoc />
        protected override void RemoveControlGroup()
        {
            Parent?.RemoveRequiredField(ParticleFields.ChildOrder);
        }

        /// <inheritdoc />
        protected override void AddControlGroup()
        {
            Parent?.AddRequiredField(ParticleFields.ChildOrder);
        }

    }
}
