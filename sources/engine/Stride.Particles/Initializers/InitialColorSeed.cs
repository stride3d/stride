// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Particles.Initializers
{
    [DataContract("InitialColorSeed")]
    [Display("Color")]
    public class InitialColorSeed : ParticleInitializer
    {
        public InitialColorSeed()
        {
            RequiredFields.Add(ParticleFields.Color4);
            RequiredFields.Add(ParticleFields.RandomSeed);
        }

        public unsafe override void Initialize(ParticlePool pool, int startIdx, int endIdx, int maxCapacity)
        {
            if (!pool.FieldExists(ParticleFields.Color4) || !pool.FieldExists(ParticleFields.RandomSeed))
                return;

            var colField = pool.GetField(ParticleFields.Color4);
            var rndField = pool.GetField(ParticleFields.RandomSeed);
            
            var i = startIdx;
            while (i != endIdx)
            {
                var particle = pool.FromIndex(i);
                var randSeed = particle.Get(rndField);

                var color = Color4.Lerp(ColorMin, ColorMax, randSeed.GetFloat(RandomOffset.Offset1A + SeedOffset));

                // Premultiply alpha
                // This can't be done in advance for ColorMin and ColorMax because it will change the math
                color.R *= color.A;
                color.G *= color.A;
                color.B *= color.A;

                (*((Color4*)particle[colField])) = color;

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
        /// The first color to interpolate from
        /// </summary>
        /// <userdoc>
        /// The first color to interpolate from
        /// </userdoc>
        [DataMember(30)]
        [Display("Color A")]
        public Color4 ColorMin { get; set; } = new Color4(1, 1, 1, 1);

        /// <summary>
        /// The second color to interpolate to
        /// </summary>
        /// <userdoc>
        /// The second color to interpolate to
        /// </userdoc>
        [DataMember(40)]
        [Display("Color B")]
        public Color4 ColorMax { get; set; } = new Color4(1, 1, 1, 1);    
    }
}
