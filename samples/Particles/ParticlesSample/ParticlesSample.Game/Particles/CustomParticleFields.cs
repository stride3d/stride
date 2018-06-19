// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;
using Xenko.Particles;

namespace ParticlesSample.Particles
{
    public static class CustomParticleFields
    {
        /// <summary>
        /// Custom field for our particle, which defines non-uniform dimensions in 2D space
        /// </summary>
        public static readonly ParticleFieldDescription<Vector2> RectangleXY = new ParticleFieldDescription<Vector2>("RectangleXY", new Vector2(1, 1));
    }
}
