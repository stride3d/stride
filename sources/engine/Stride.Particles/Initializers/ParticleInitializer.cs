// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Particles.Initializers
{
    [DataContract("ParticleInitializer")]
    public abstract class ParticleInitializer : ParticleModule
    {
//        internal List<ParticleFieldDescription> RequiredFields = new List<ParticleFieldDescription>(ParticlePool.DefaultMaxFielsPerPool);

        /// <summary>
        /// Override Initialize if your module acts as an Initializer and change its type to Initializer
        /// </summary>
        /// <param name="pool">Particle pool to target</param>
        /// <param name="startIdx">Starting index (included from the array)</param>
        /// <param name="endIdx">End index (excluded from the array)</param>
        /// <param name="maxCapacity">Max pool capacity (loops after this point) so that it's possible for (endIdx < startIdx)</param>
        public abstract void Initialize(ParticlePool pool, int startIdx, int endIdx, int maxCapacity);
        /*
        {
            // Example - nullify the position's Y coordinate
            if (!pool.FieldExists(ParticleFields.Position))
                return;

            var posField = pool.GetField(ParticleFields.Position);

            var i = startIdx;
            while (i != endIdx)
            {
                var particle = pool.FromIndex(i);

                (*((Vector3*)particle[posField])).Y = 0;

                i = (i + 1) % maxCapacity;
            }
        }
        //*/
    }
}

