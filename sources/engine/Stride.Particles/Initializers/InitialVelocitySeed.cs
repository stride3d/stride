// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Particles.DebugDraw;

namespace Stride.Particles.Initializers
{
    /// <summary>
    /// Initializer which sets the initial velocity for particles based on RandomSeed information
    /// </summary>
    [DataContract("InitialVelocitySeed")]
    [Display("Velocity")]
    public class InitialVelocitySeed : ParticleInitializer
    {
        public InitialVelocitySeed()
        {
            RequiredFields.Add(ParticleFields.Velocity);
            RequiredFields.Add(ParticleFields.RandomSeed);

            DisplayParticleRotation = true;
            DisplayParticleScaleUniform = true;
        }

        public unsafe override void Initialize(ParticlePool pool, int startIdx, int endIdx, int maxCapacity)
        {
            if (!pool.FieldExists(ParticleFields.Velocity) || !pool.FieldExists(ParticleFields.RandomSeed))
                return;

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

            var i = startIdx;
            while (i != endIdx)
            {
                var particle = pool.FromIndex(i);
                var randSeed = particle.Get(rndField);

                var particleRandVel = leftCorner;
                particleRandVel += xAxis * randSeed.GetFloat(RandomOffset.Offset3A + SeedOffset);
                particleRandVel += yAxis * randSeed.GetFloat(RandomOffset.Offset3B + SeedOffset);
                particleRandVel += zAxis * randSeed.GetFloat(RandomOffset.Offset3C + SeedOffset);

                (*((Vector3*)particle[velField])) = particleRandVel;

                i = (i + 1) % maxCapacity;
            }
        }

        /// <summary>
        /// The seed offset used to match or separate random values
        /// </summary>
        /// <userdoc>
        /// The seed offset used to match or separate random values
        /// </userdoc>
        [DataMember(8)]
        [Display("Random Seed")]
        public uint SeedOffset { get; set; } = 0;

        /// <summary>
        /// Lower velocity value
        /// </summary>
        /// <userdoc>
        /// Lower velocity value
        /// </userdoc>
        [DataMember(30)]
        [Display("Velocity min")]
        public Vector3 VelocityMin { get; set; } = new Vector3(-1, 1, -1);

        /// <summary>
        /// Upper velocity value
        /// </summary>
        /// <userdoc>
        /// Upper velocity value
        /// </userdoc>
        [DataMember(40)]
        [Display("Velocity max")]
        public Vector3 VelocityMax { get; set; } = Vector3.One;

        /// <summary>
        /// Should this Particle Module's bounds be displayed as a debug draw
        /// </summary>
        /// <userdoc>
        /// Display the Particle Module's bounds as a wireframe debug shape. Temporary feature (will be removed later)!
        /// </userdoc>
        [DataMember(-1)]
        [DefaultValue(false)]
        public bool DebugDraw { get; set; } = false;

        public override bool TryGetDebugDrawShape(out DebugDrawShape debugDrawShape, out Vector3 translation, out Quaternion rotation, out Vector3 scale)
        {
            if (!DebugDraw)
                return base.TryGetDebugDrawShape(out debugDrawShape, out translation, out rotation, out scale);

            debugDrawShape = DebugDrawShape.Cube;

            rotation = WorldRotation;

            scale = (VelocityMax - VelocityMin);
            translation = (VelocityMax + VelocityMin) * 0.5f * WorldScale;

            scale *= WorldScale;
            rotation.Rotate(ref translation);
            translation += WorldPosition;

            return true;
        }

    }
}
