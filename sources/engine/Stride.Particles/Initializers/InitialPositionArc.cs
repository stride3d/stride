// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.Particles.Initializers
{
    /// <summary>
    /// The <see cref="InitialPositionArc"/> is an initializer which sets the particle's initial position along a line or an arc
    /// </summary>
    [DataContract("InitialPositionArc")]
    [Display("Position (Arc)")]
    public class InitialPositionArc : ParticleInitializer
    {
        /// <summary>
        /// Default constructor which also registers the fields required by this updater
        /// </summary>
        public InitialPositionArc()
        {
            RequiredFields.Add(ParticleFields.Position);
            RequiredFields.Add(ParticleFields.RandomSeed);

            // DisplayPosition = true; // Always inherit the position and don't allow to opt out
            DisplayParticleRotation = true;
        }

        /// <summary>
        /// An arc initializer needs a second point so that it can position the particles in a line or arc between two locators
        /// </summary>
        /// <userdoc>
        /// An arc initializer needs a second point so that it can position the particles in a line or arc between two locators
        /// </userdoc>
        [DataMember(10)]
        [Display("Target")]
        public TransformComponent Target;

        /// <summary>
        /// In case the <see cref="Target"/> is null, the <see cref="FallbackTarget"/> offset will be used
        /// </summary>
        /// <userdoc>
        /// In case the Target is null, the FallbackTarget offset will be used
        /// </userdoc>
        [DataMember(12)]
        [Display("Fallback Target")]
        public Vector3 FallbackTarget = new Vector3(0, 0, -1);

        /// <summary>
        /// The height of the arc in the center, which is also subject to scale and rotation if inherited
        /// </summary>
        /// <userdoc>
        /// The height of the arc in the center, which is also subject to scale and rotation if inherited
        /// </userdoc>
        [DataMember(20)]
        [Display("Arc Height")]
        public float ArcHeight = 1f;

        /// <summary>
        /// If <C>true</C> the particles will appear along the arc path in the order they are added.
        /// Otherwise, their positions will be random along the arc, not preserving the order of appearance.
        /// </summary>
        /// <userdoc>
        /// If true the particles will appear along the arc path in the order they are added.
        /// Otherwise, their positions will be random along the arc, not preserving the order of appearance.
        /// </userdoc>
        [DataMember(30)]
        [Display("Ordered")]
        public bool Sequential = true;

        /// <summary>
        /// If positive, it will limit the number of possible positions where the particles can appear along the arc to the specified count.
        /// More particles will start appearing over from the biginning of the arc path.
        /// </summary>
        /// <userdoc>
        /// If positive, it will limit the number of possible positions where the particles can appear along the arc to the specified count.
        /// More particles will start appearing over from the biginning of the arc path.
        /// </userdoc>
        [DataMember(35)]
        [Display("Fixed Count")]
        public int FixedLength = 0;

        /// <summary>
        /// The left bottom back corner of the box
        /// </summary>
        /// <userdoc>
        /// The left bottom back corner of the box
        /// </userdoc>
        [DataMember(40)]
        [Display("Position min")]
        public Vector3 PositionMin { get; set; } = new Vector3(-0.3f, -0.3f, -0.3f);

        /// <summary>
        /// The right upper front corner of the box
        /// </summary>
        /// <userdoc>
        /// The right upper front corner of the box
        /// </userdoc>
        [DataMember(42)]
        [Display("Position max")]
        public Vector3 PositionMax { get; set; } = new Vector3(0.3f, 0.3f, 0.3f);

        /// <summary>
        /// The seed offset used to match or separate random values
        /// </summary>
        /// <userdoc>
        /// The seed offset used to match or separate random values
        /// </userdoc>
        [DataMember(50)]
        [Display("Random Seed")]
        public uint SeedOffset { get; set; } = 0;

        /// <inheritdoc />
        public unsafe override void Initialize(ParticlePool pool, int startIdx, int endIdx, int maxCapacity)
        {
            if (!pool.FieldExists(ParticleFields.Position) || !pool.FieldExists(ParticleFields.RandomSeed))
                return;

            var posField = pool.GetField(ParticleFields.Position);
            var rndField = pool.GetField(ParticleFields.RandomSeed);

            var arcOffset = new Vector3(0, ArcHeight, 0);
            if (!WorldRotation.IsIdentity)
            {
                WorldRotation.Rotate(ref arcOffset);
            }

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

            var targetCornerAdd = Target?.WorldMatrix.TranslationVector - WorldPosition ?? FallbackTarget;

            var totalCountLessOne = (FixedLength > 0) ? (FixedLength - 1) : (startIdx < endIdx) ? (endIdx - startIdx - 1) : (endIdx - startIdx + maxCapacity - 1);
            var stepF = (totalCountLessOne > 1) ? (1f / totalCountLessOne) : 1f;
            var step = -stepF;

            var i = startIdx;
            while (i != endIdx)
            {
                var particle = pool.FromIndex(i);
                var randSeed = particle.Get(rndField);

                if (Sequential)
                {
                    step += stepF;
                    if (FixedLength > 0) step = (i % FixedLength) * stepF;
                }
                else
                {
                    step = randSeed.GetFloat(RandomOffset.Offset1A + SeedOffset);
                }

                var positionOffsetFactor = (float)Math.Sin(step * Math.PI);
                var particleRandPos = leftCorner * positionOffsetFactor + targetCornerAdd * step + arcOffset * positionOffsetFactor + WorldPosition;

                particleRandPos += xAxis * positionOffsetFactor * randSeed.GetFloat(RandomOffset.Offset3A + SeedOffset);
                particleRandPos += yAxis * positionOffsetFactor * randSeed.GetFloat(RandomOffset.Offset3B + SeedOffset);
                particleRandPos += zAxis * positionOffsetFactor * randSeed.GetFloat(RandomOffset.Offset3C + SeedOffset);

                (*((Vector3*)particle[posField])) = particleRandPos;

                i = (i + 1) % maxCapacity;
            }
        }
    }
}
