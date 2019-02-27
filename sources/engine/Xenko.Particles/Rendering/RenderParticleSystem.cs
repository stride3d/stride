// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Engine;
using Xenko.Particles.Components;

namespace Xenko.Particles.Rendering
{
    /// <summary>
    /// Defines a particle system to render.
    /// </summary>
    public class RenderParticleSystem
    {
        public ParticleSystem ParticleSystem;

        public RenderParticleEmitter[] Emitters;
    }
}
