// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Particles.Spawners;

namespace Xenko.Particles.Initializers
{
    /// <summary>
    /// The <see cref="InitialVelocityParent"/> is an initializer which sets the particle's velocity based on a followed (parent) particle's velocity
    /// </summary>
    [DataContract("InitialVelocityParent")]
    [Display("Velocity (parent)")]
    public class InitialVelocityParent : ParticleChildInitializer
    {
        /// <summary>
        /// Default constructor which also registers the fields required by this updater
        /// </summary>
        public InitialVelocityParent()
        {
            RequiredFields.Add(ParticleFields.Velocity);
            RequiredFields.Add(ParticleFields.RandomSeed);

            // DisplayPosition = true; // Always inherit the position and don't allow to opt out
            DisplayParticleRotation = true;
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
        /// The factor (percentage) for parent's velocity inheritance
        /// </summary>
        /// <userdoc>
        /// The factor (percentage) for parent's velocity inheritance
        /// </userdoc>
        [DataMember(25)]
        [Display("Velocity factor")]
        public float ParentVelocityFactor { get; set; } = 0.5f;

        /// <summary>
        /// The left bottom back corner of the box
        /// </summary>
        /// <userdoc>
        /// The left bottom back corner of the box
        /// </userdoc>
        [DataMember(30)]
        [Display("Velocity min")]
        public Vector3 VelocityMin { get; set; } = new Vector3(-1, 1, -1);

        /// <summary>
        /// The right upper front corner of the box
        /// </summary>
        /// <userdoc>
        /// The right upper front corner of the box
        /// </userdoc>
        [DataMember(40)]
        [Display("Velocity max")]
        public Vector3 VelocityMax { get; set; } = Vector3.One;


        /// <inheritdoc />
        public unsafe override void Initialize(ParticlePool pool, int startIdx, int endIdx, int maxCapacity)
        {
            if (!pool.FieldExists(ParticleFields.Velocity) || !pool.FieldExists(ParticleFields.RandomSeed))
                return;

            // Collect the total number of living particles in the parent pool which have a Velocity field
            var parentPool = Parent?.Pool;
            var parentParticlesCount = parentPool?.LivingParticles ?? 0;
            var velFieldParent = parentPool?.GetField(ParticleFields.Velocity) ?? ParticleFieldAccessor<Vector3>.Invalid();
            if (!velFieldParent.IsValid())
            {
                parentParticlesCount = 0;
            }

            var spawnControlField = GetSpawnControlField();

            var velField = pool.GetField(ParticleFields.Velocity);
            var rndField = pool.GetField(ParticleFields.RandomSeed);

            var leftCorner = VelocityMin * WorldScale;
            var xAxis = new Vector3(VelocityMax.X * WorldScale.X - leftCorner.X, 0, 0);
            var yAxis = new Vector3(0, VelocityMax.Y * WorldScale.Y - leftCorner.Y, 0);
            var zAxis = new Vector3(0, 0, VelocityMax.Z * WorldScale.Z - leftCorner.Z);

            if (!WorldRotation.IsIdentity)
            {
                WorldRotation.Rotate(ref leftCorner);
                WorldRotation.Rotate(ref xAxis);
                WorldRotation.Rotate(ref yAxis);
                WorldRotation.Rotate(ref zAxis);
            }

            var sequentialParentIndex = 0;
            var sequentialParentParticles = 0;
            var parentIndex = 0;

            var i = startIdx;
            while (i != endIdx)
            {
                var particle = pool.FromIndex(i);
                var randSeed = particle.Get(rndField);

                var particleRandVel = leftCorner;

                particleRandVel += xAxis * randSeed.GetFloat(RandomOffset.Offset3A + SeedOffset);
                particleRandVel += yAxis * randSeed.GetFloat(RandomOffset.Offset3B + SeedOffset);
                particleRandVel += zAxis * randSeed.GetFloat(RandomOffset.Offset3C + SeedOffset);

                if (parentParticlesCount > 0)
                {
                    var parentParticleVelocity = Vector3.Zero;

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
                        parentParticleVelocity = (*((Vector3*)parentParticle[velFieldParent]));
                    }

                    // Spawn is not fixed - pick a parent at random
                    else
                    {
                        parentIndex = (int)(parentParticlesCount * randSeed.GetFloat(RandomOffset.Offset1A + ParentSeedOffset));
                        var parentParticle = parentPool.FromIndex(parentIndex);

                        parentParticleVelocity = (*((Vector3*)parentParticle[velFieldParent]));
                    }


                    // Convert from Local -> World space if needed
                    if (Parent.SimulationSpace == EmitterSimulationSpace.Local)
                    {
                        WorldRotation.Rotate(ref parentParticleVelocity);
                        parentParticleVelocity *= WorldScale.X;
                    }

                    particleRandVel += parentParticleVelocity * ParentVelocityFactor;
                }


                (*((Vector3*)particle[velField])) = particleRandVel;

                i = (i + 1) % maxCapacity;
            }
        }
        

    }
}
