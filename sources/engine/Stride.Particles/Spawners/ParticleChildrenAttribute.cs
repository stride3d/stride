// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Particles.Spawners
{
    public struct ParticleChildrenAttribute
    {
        public static ParticleChildrenAttribute Empty = new ParticleChildrenAttribute { flags = 0 };

        private uint flags;

        // Maybe encode it in 0-255 value and put it in flags?
        private float carryOver;

        public ParticleChildrenAttribute(ParticleChildrenAttribute other)
        {
            flags = other.flags;
            carryOver = other.carryOver;
        }

        private const uint MaskParticlesToEmit = 0xFF << 0;

        public uint ParticlesToEmit
        {
            get { return (flags & MaskParticlesToEmit); }
            set
            {
                flags = (flags & ~MaskParticlesToEmit) + Math.Min(value, MaskParticlesToEmit);
            }
        }

        public float CarryOver
        {
            get { return carryOver; }
            set { carryOver = value; }
        }
    }
}
