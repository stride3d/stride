// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Particles.Spawners;

namespace Xenko.Particles.Initializers
{
    /// <summary>
    /// The <see cref="InitialPositionParent"/> is an initializer which sets the particle's initial position at the time of spawning
    /// </summary>
    [DataContract("InitialPositionParent")]
    [Display("Position (parent)")]
    public class InitialPositionParent : ParticleChildInitializer
    {
        /// <summary>
        /// Default constructor which also registers the fields required by this updater
        /// </summary>
        public InitialPositionParent()
        {
            RequiredFields.Add(ParticleFields.Position);
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
        /// The left bottom back corner of the box
        /// </summary>
        /// <userdoc>
        /// The left bottom back corner of the box
        /// </userdoc>
        [DataMember(30)]
        [Display("Position min")]
        public Vector3 PositionMin { get; set; } = new Vector3(-1, 1, -1);

        /// <summary>
        /// The right upper front corner of the box
        /// </summary>
        /// <userdoc>
        /// The right upper front corner of the box
        /// </userdoc>
        [DataMember(40)]
        [Display("Position max")]
        public Vector3 PositionMax { get; set; } = Vector3.One;


        /// <inheritdoc />
        public unsafe override void Initialize(ParticlePool pool, int startIdx, int endIdx, int maxCapacity)
        {
            if (!pool.FieldExists(ParticleFields.Position) || !pool.FieldExists(ParticleFields.RandomSeed))
                return;

            // Collect the total number of living particles in the parent pool which have a Position field
            var parentPool = Parent?.Pool;
            var parentParticlesCount = parentPool?.LivingParticles ?? 0;
            var posFieldParent = parentPool?.GetField(ParticleFields.Position) ?? ParticleFieldAccessor<Vector3>.Invalid();
            if (!posFieldParent.IsValid())
            {
                parentParticlesCount = 0;
            }

            var oldPosFieldParent = parentPool?.GetField(ParticleFields.OldPosition) ?? ParticleFieldAccessor<Vector3>.Invalid();

            var spawnControlField = GetSpawnControlField();

            var posField = pool.GetField(ParticleFields.Position);
            var rndField = pool.GetField(ParticleFields.RandomSeed);
            var oldField = pool.GetField(ParticleFields.OldPosition);

            var leftCorner = PositionMin * WorldScale;
            var xAxis = new Vector3(PositionMax.X * WorldScale.X - leftCorner.X, 0, 0);
            var yAxis = new Vector3(0, PositionMax.Y * WorldScale.Y - leftCorner.Y, 0);
            var zAxis = new Vector3(0, 0, PositionMax.Z * WorldScale.Z - leftCorner.Z);

            if (!WorldRotation.IsIdentity)
            {
                WorldRotation.Rotate(ref leftCorner);
                WorldRotation.Rotate(ref xAxis);
                WorldRotation.Rotate(ref yAxis);
                WorldRotation.Rotate(ref zAxis);
            }

            // Already inheriting from parent
            if (parentParticlesCount == 0)
                leftCorner += WorldPosition;

            var sequentialParentIndex = 0;
            var sequentialParentParticles = 0;
            var parentIndex = 0;

            // Interpolation - if parent particle has OldPosition field
            var stepF = 0f;
            var stepTotal = 0f;
            var positionDistance = Vector3.Zero;

            var i = startIdx;
            while (i != endIdx)
            {
                var particle = pool.FromIndex(i);
                var randSeed = particle.Get(rndField);

                var particleRandPos = leftCorner;

                particleRandPos += xAxis * randSeed.GetFloat(RandomOffset.Offset3A + SeedOffset);
                particleRandPos += yAxis * randSeed.GetFloat(RandomOffset.Offset3B + SeedOffset);
                particleRandPos += zAxis * randSeed.GetFloat(RandomOffset.Offset3C + SeedOffset);

                if (parentParticlesCount > 0)
                {
                    var parentParticlePosition = Vector3.Zero;

                    // Spawn is fixed - parent particles have spawned a very specific number of children each
                    if (spawnControlField.IsValid())
                    {
                        // Interpolation - if parent particle has OldPosition field

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

                            if (oldPosFieldParent.IsValid())
                            {
                                stepF = (sequentialParentParticles > 0) ? (1f/(float)sequentialParentParticles) : 1;
                                stepTotal = 0f;
                                positionDistance = ((*((Vector3*)tempParentParticle[oldPosFieldParent])) - (*((Vector3*)tempParentParticle[posFieldParent])));
                            }
                        }

                        sequentialParentParticles--;

                        var parentParticle = parentPool.FromIndex(parentIndex);
                        parentParticlePosition = (*((Vector3*)parentParticle[posFieldParent]));
                        parentParticlePosition += positionDistance * stepTotal;
                        stepTotal += stepF;
                    }

                    // Spawn is not fixed - pick a parent at random
                    else
                    {
                        parentIndex = (int)(parentParticlesCount * randSeed.GetFloat(RandomOffset.Offset1A + ParentSeedOffset));
                        var parentParticle = parentPool.FromIndex(parentIndex);

                        parentParticlePosition = (*((Vector3*)parentParticle[posFieldParent]));
                    }


                    // Convert from Local -> World space if needed
                    if (Parent.SimulationSpace == EmitterSimulationSpace.Local)
                    {
                        WorldRotation.Rotate(ref parentParticlePosition);
                        parentParticlePosition *= WorldScale.X;
                        parentParticlePosition += WorldPosition;
                    }

                    particleRandPos += parentParticlePosition;
                }


                (*((Vector3*)particle[posField])) = particleRandPos;

                if (oldField.IsValid())
                {
                    (*((Vector3*)particle[oldField])) = particleRandPos;
                }

                i = (i + 1) % maxCapacity;
            }
        }
        
    }
}
