// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Engine.Design;
using Xenko.Engine;
using Xenko.Particles.Rendering;
using Xenko.Rendering;

namespace Xenko.Particles.Components
{
    /// <summary>
    /// Add a <see cref="ParticleSystem"/> to an <see cref="Entity"/>
    /// </summary>
    [DataContract("ParticleSystemComponent")]
    [Display("Particle system", Expand = ExpandRule.Once)]
    [DefaultEntityComponentProcessor(typeof(ParticleSystemSimulationProcessor))]
    [DefaultEntityComponentRenderer(typeof(ParticleSystemRenderProcessor))]
    [ComponentOrder(10200)]
    [ComponentCategory("Particles")]
    public sealed class ParticleSystemComponent : ActivableEntityComponent
    {        
        private ParticleSystem particleSystem;

        /// <summary>
        /// The particle system associated with this component
        /// </summary>
        /// <userdoc>
        /// The Particle System associated with this component
        /// </userdoc>
        [DataMember(10)]
        [Display("Source")]
        public ParticleSystem ParticleSystem => particleSystem;

        ~ParticleSystemComponent()
        {
            particleSystem = null;
        }

        [DataMember(1)]
        [Display("Editor control")]
        public ParticleSystemControl Control = new ParticleSystemControl();

        /// <summary>
        /// The color shade will be applied to all particles (via their materials) during rendering.
        /// The shade acts as a color scale multiplication, making the color darker. White shade is neutral.
        /// </summary>
        /// <userdoc>
        /// Color shade (RGBA) will be multiplied to all particles' color in this particle system
        /// </userdoc>
        [DataMember(4)]
        [Display("Color shade")]
        public Color4 Color = Color4.White;

        /// <summary>
        /// The speed scale at which the particle simulation runs. Increasing the scale increases the simulation speed,
        /// while setting it to 0 effectively pauses the simulation.
        /// </summary>
        /// <userdoc>
        /// The speed scale at which this particle system runs the simulation. Set it to 0 to pause it
        /// </userdoc>
        [DataMember(5)]
        [DataMemberRange(0, 10, 0.01, 1, 3)]
        [Display("Speed scale")]
        public float Speed { get; set; } = 1f;

        /// <summary>
        /// The render group for this component.
        /// </summary>
        [DataMember(50)]
        [Display("Render group")]
        [DefaultValue(RenderGroup.Group0)]
        public RenderGroup RenderGroup { get; set; }


        public ParticleSystemComponent()
        {
            particleSystem = new ParticleSystem();
        }
    }
}
