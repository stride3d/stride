// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Particles.Initializers
{
    [DataContract("InitialSizeSeed")]
    [Display("Size")]
    public class InitialSizeSeed : ParticleInitializer
    {
        public InitialSizeSeed()
        {
            RequiredFields.Add(ParticleFields.Size);
            RequiredFields.Add(ParticleFields.RandomSeed);

            DisplayParticleScaleUniform = true;
        }

        public unsafe override void Initialize(ParticlePool pool, int startIdx, int endIdx, int maxCapacity)
        {
            if (!pool.FieldExists(ParticleFields.Size) || !pool.FieldExists(ParticleFields.RandomSeed))
                return;

            var sizeField = pool.GetField(ParticleFields.Size);
            var rndField = pool.GetField(ParticleFields.RandomSeed);
            
            var minSize = WorldScale.X * RandomSize.X;
            var sizeGap = WorldScale.X * RandomSize.Y - minSize;

            var i = startIdx;
            while (i != endIdx)
            {
                var particle = pool.FromIndex(i);
                var randSeed = particle.Get(rndField);

                (*((float*)particle[sizeField])) = minSize + sizeGap * randSeed.GetFloat(RandomOffset.Offset1A + SeedOffset);

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
        /// Minimum and maximum values for the size field
        /// </summary>
        /// <userdoc>
        /// Minimum and maximum values for the size field
        /// </userdoc>
        [DataMember(30)]
        [Display("Random size")]
        public Vector2 RandomSize { get; set; } = new Vector2(0.5f, 1);
        
    }
}
