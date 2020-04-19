// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;

namespace Stride.Particles.Initializers
{
    /// <summary>
    /// The <see cref="InitialSpawnOrder"/> is an initializer which assigns all particles an increasing number based on the order of their spawning
    /// </summary>
    [DataContract("InitialSpawnOrder")]
    [Display("Spawn Order")]
    public class InitialSpawnOrder : ParticleInitializer
    {
        // Will loop every so often, but the loop condition should be unreachable for normal games (~800 hours for spawning rate of 100 particles/second)
        private uint spawnOrder = 0;

        /// <inheritdoc />
        public override void ResetSimulation()
        {
            spawnOrder = 0;
        }

        /// <summary>
        /// Default constructor which also registers the fields required by this updater
        /// </summary>
        public InitialSpawnOrder()
        {
            spawnOrder = 0;

            RequiredFields.Add(ParticleFields.Order);
        }

        /// <inheritdoc />
        public unsafe override void Initialize(ParticlePool pool, int startIdx, int endIdx, int maxCapacity)
        {
            if (!pool.FieldExists(ParticleFields.Order))
                return;

            var orderField = pool.GetField(ParticleFields.Order);
            var childOrderField = pool.GetField(ParticleFields.ChildOrder);

            var i = startIdx;
            while (i != endIdx)
            {
                var particle = pool.FromIndex(i);

                (*((uint*)particle[orderField])) = spawnOrder++; // Will loop every so often, but the loop condition should be unreachable for normal games

                if (childOrderField.IsValid())
                    (*((uint*)particle[childOrderField])) = 0;

                i = (i + 1) % maxCapacity;
            }
        }
    }
}
