// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Particles.Spawners;

namespace Stride.Particles.Initializers
{
    /// <summary>
    /// The <see cref="InitialSizeParent"/> is an initializer which sets the particle's size based on a followed (parent) particle's size
    /// </summary>
    [DataContract("InitialSizeParent")]
    [Display("Size (parent)")]
    public class InitialSizeParent : ParticleChildInitializer
    {
        /// <summary>
        /// Default constructor which also registers the fields required by this updater
        /// </summary>
        public InitialSizeParent()
        {
            RequiredFields.Add(ParticleFields.Size);
            RequiredFields.Add(ParticleFields.RandomSeed);

            // DisplayPosition = true; // Always inherit the position and don't allow to opt out
            DisplayParticleScaleUniform = true;
        }


        /// <summary>
        /// The seed offset used to match or separate random values
        /// </summary>
        /// <userdoc>
        /// The seed offset used to match or separate random values
        /// </userdoc>
        [DataMember(20)]
        [Display("Random Seed")]
        public uint SeedOffset { get; set; } = 0;

        /// <summary>
        /// Minimum and maximum values for the size field
        /// </summary>
        /// <userdoc>
        /// Minimum and maximum values for the size field
        /// </userdoc>
        [DataMember(30)]
        [Display("Random size")]
        public Vector2 RandomSize { get; set; } = new Vector2(0.5f, 1);


        /// <inheritdoc />
        public unsafe override void Initialize(ParticlePool pool, int startIdx, int endIdx, int maxCapacity)
        {
            if (!pool.FieldExists(ParticleFields.Size) || !pool.FieldExists(ParticleFields.RandomSeed))
                return;

            var parentPool = Parent?.Pool;
            var parentParticlesCount = parentPool?.LivingParticles ?? 0;
            var sizeFieldParent = parentPool?.GetField(ParticleFields.Size) ?? ParticleFieldAccessor<float>.Invalid();
            if (!sizeFieldParent.IsValid())
            {
                parentParticlesCount = 0;
            }

            var spawnControlField = GetSpawnControlField();

            var sizeField = pool.GetField(ParticleFields.Size);
            var rndField = pool.GetField(ParticleFields.RandomSeed);

            var minSize = WorldScale.X * RandomSize.X;
            var sizeGap = WorldScale.X * RandomSize.Y - minSize;

            var sequentialParentIndex = 0;
            var sequentialParentParticles = 0;
            var parentIndex = 0;

            var i = startIdx;
            while (i != endIdx)
            {
                var particle = pool.FromIndex(i);
                var randSeed = particle.Get(rndField);

                var particleRandomSize = minSize + sizeGap * randSeed.GetFloat(RandomOffset.Offset1A + SeedOffset);

                if (parentParticlesCount > 0)
                {
                    var parentParticleSize = 1f;

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
                        parentParticleSize = (*((float*)parentParticle[sizeFieldParent]));
                    }

                    // Spawn is not fixed - pick a parent at random
                    else
                    {
                        parentIndex = (int)(parentParticlesCount * randSeed.GetFloat(RandomOffset.Offset1A + ParentSeedOffset));
                        var parentParticle = parentPool.FromIndex(parentIndex);

                        parentParticleSize = (*((float*)parentParticle[sizeFieldParent]));
                    }


                    // Convert from Local -> World space if needed
                    if (Parent.SimulationSpace == EmitterSimulationSpace.Local)
                    {
                        parentParticleSize *= WorldScale.X;
                    }

                    particleRandomSize *= parentParticleSize;
                }


                (*((float*)particle[sizeField])) = particleRandomSize;

                i = (i + 1) % maxCapacity;
            }
        }


    }
}
