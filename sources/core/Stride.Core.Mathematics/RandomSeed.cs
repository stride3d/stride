// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Random numbers which also allow creation of random values in the shaders and are deterministic
// Based on this article:
// http://martindevans.me/game-development/2015/02/22/Random-Gibberish/

using System;

namespace Stride.Core.Mathematics
{
    /// <summary>
    /// The <see cref="RandomSeed"/> is a structure for deterministically acquiring random values.
    /// One <see cref="RandomSeed"/> should be able to reproduce the same pseudo-random value for a fixed offset, but
    /// provide enough random distribution for different offsets or different random seeds
    /// Although other methods exist, the current implementation can easily be replicated in the shaders if required
    /// </summary>
    public struct RandomSeed
    {
        private const double GelfondConst = 23.1406926327792690;            // e to the power of Pi = (-1) to the power of -i
        private const double GelfondSchneiderConst = 2.6651441426902251;    // 2 to the power of sqrt(2)
        private const double Numerator = 123456789;

        // When casting uint to double it works fine, but when casting it to float it might cause underflow errors (loss of precision)
        // We want to limit the maximum settable value to prevent such errors.
        private const uint UnderflowGuard = 0xFFFF;

        private readonly uint seed;

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomSeed"/> struct from a target uint.
        /// </summary>
        /// <param name="seed">The seed value to initialize the deterministic random generator.</param>
        public RandomSeed(uint seed)
        {
            this.seed = (seed & UnderflowGuard);
        }

        /// <summary>
        /// Get a deterministic double value between 0 and 1 based on the seed
        /// </summary>
        /// <returns>Deterministic pseudo-random value between 0 and 1</returns>
        public double GetDouble(uint offset)
        {           
            var dRand = (double)(unchecked(seed + offset)); // We want it to overflow

            var dotProduct = Math.Cos(dRand) * GelfondConst + Math.Sin(dRand) * GelfondSchneiderConst;
            var denominator = 1e-7 + 256 * dotProduct;
            var remainder = Numerator % denominator;

            return (remainder - Math.Floor(remainder));
        }

        /// <summary>
        /// Get a deterministic float value between 0 and 1 based on the seed
        /// The calculations are still made as doubles to prevent underflow errors.
        /// </summary>
        /// <returns>Deterministic pseudo-random value between 0 and 1</returns>
        public float GetFloat(uint offset) => (float)GetDouble(offset);
    }
}
