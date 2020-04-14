// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Particles.DebugDraw;

namespace Xenko.Particles.Initializers
{
    /// <summary>
    /// Initializer which sets the initial velocity for particles based on RandomSeed information
    /// </summary>
    [DataContract("InitialDirectionSeed")]
    [Display("Direction")]
    public class InitialDirectionSeed : ParticleInitializer
    {
        public InitialDirectionSeed()
        {
            RequiredFields.Add(ParticleFields.Direction);
            RequiredFields.Add(ParticleFields.RandomSeed);

            DisplayParticleRotation = true;
            DisplayParticleScaleUniform = true;
        }

        public unsafe override void Initialize(ParticlePool pool, int startIdx, int endIdx, int maxCapacity)
        {
            if (!pool.FieldExists(ParticleFields.Direction) || !pool.FieldExists(ParticleFields.RandomSeed))
                return;

            var dirField = pool.GetField(ParticleFields.Direction);
            var rndField = pool.GetField(ParticleFields.RandomSeed);

            var leftCorner = DirectionMin * WorldScale;
            var xAxis = new Vector3(DirectionMax.X * WorldScale.X - leftCorner.X, 0, 0);
            var yAxis = new Vector3(0, DirectionMax.Y * WorldScale.Y - leftCorner.Y, 0);
            var zAxis = new Vector3(0, 0, DirectionMax.Z * WorldScale.Z - leftCorner.Z);

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

                var particleRandDir = leftCorner;
                particleRandDir += xAxis * randSeed.GetFloat(RandomOffset.Offset3A + SeedOffset);
                particleRandDir += yAxis * randSeed.GetFloat(RandomOffset.Offset3B + SeedOffset);
                particleRandDir += zAxis * randSeed.GetFloat(RandomOffset.Offset3C + SeedOffset);

                (*((Vector3*)particle[dirField])) = particleRandDir;

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
        /// Lower direction value
        /// </summary>
        /// <userdoc>
        /// Lower direction value
        /// </userdoc>
        [DataMember(30)]
        [Display("Direction min")]
        public Vector3 DirectionMin { get; set; } = new Vector3(-1, 1, -1);

        /// <summary>
        /// Upper direction value
        /// </summary>
        /// <userdoc>
        /// Upper direction value
        /// </userdoc>
        [DataMember(40)]
        [Display("Direction max")]
        public Vector3 DirectionMax { get; set; } = Vector3.One;

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

            scale = (DirectionMax - DirectionMin);
            translation = (DirectionMax + DirectionMin) * 0.5f * WorldScale;

            scale *= WorldScale;
            rotation.Rotate(ref translation);
            translation += WorldPosition;

            return true;
        }

    }
}
