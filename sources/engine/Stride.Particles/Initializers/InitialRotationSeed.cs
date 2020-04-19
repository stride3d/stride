// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Particles.Initializers
{
    /// <summary>
    /// The <see cref="InitialRotationSeed"/> is an initializer which sets the particle's rotation around the Z axis in clip space (camera-facing)
    /// </summary>
    [DataContract("InitialRotationSeed")]
    [Display("Rotation")]
    public class InitialRotationSeed : ParticleInitializer
    {
        private Vector2 angularRotation = new Vector2(-60f, 60f);
        private float angularRotationStart = MathUtil.DegreesToRadians(-60f);
        private float angularRotationStep = MathUtil.DegreesToRadians(120);


        /// <summary>
        /// Default constructor which also registers the fields required by this updater
        /// </summary>
        public InitialRotationSeed()
        {
            RequiredFields.Add(ParticleFields.Angle);
            RequiredFields.Add(ParticleFields.RandomSeed);
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
        /// Angular rotation in degrees, positive value means clockwise
        /// </summary>
        /// <userdoc>
        /// Angular rotation in degrees, positive value means clockwise
        /// </userdoc>
        [DataMember(30)]
        [Display("Angle (degrees) min")]
        public Vector2 AngularRotation
        {
            get { return angularRotation; }
            set
            {
                angularRotation = value;
                angularRotationStart = MathUtil.DegreesToRadians(angularRotation.X);
                angularRotationStep = MathUtil.DegreesToRadians(angularRotation.Y - angularRotation.X);
            }
        }



        /// <inheritdoc />
        public unsafe override void Initialize(ParticlePool pool, int startIdx, int endIdx, int maxCapacity)
        {
            if (!pool.FieldExists(ParticleFields.Angle) || !pool.FieldExists(ParticleFields.RandomSeed))
                return;

            var rotField = pool.GetField(ParticleFields.Angle);
            var rndField = pool.GetField(ParticleFields.RandomSeed);

            var i = startIdx;
            while (i != endIdx)
            {
                var particle = pool.FromIndex(i);
                var randSeed = particle.Get(rndField);

                (*((float*)particle[rotField])) = angularRotationStart + angularRotationStep * randSeed.GetFloat(RandomOffset.Offset1A + SeedOffset);

                i = (i + 1) % maxCapacity;
            }
        }
        
        
    }
}
