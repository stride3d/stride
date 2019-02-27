// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Particles.Components;
using Xenko.Rendering;
using Xenko.Streaming;

namespace Xenko.Particles.Rendering
{
    /// <summary>
    /// The processor in charge of updating and drawing the entities having sprite components.
    /// </summary>
    public class ParticleSystemRenderProcessor : EntityProcessor<ParticleSystemComponent, RenderParticleSystem>, IEntityComponentRenderProcessor
    {
        public VisibilityGroup VisibilityGroup { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParticleSystemRenderProcessor"/> class.
        /// </summary>
        public ParticleSystemRenderProcessor()
            : base(typeof(TransformComponent))
        {
        }

        private void CheckEmitters(RenderParticleSystem renderParticleSystem)
        {
            if (renderParticleSystem == null)
                return;

            var emitters = renderParticleSystem.ParticleSystem.Emitters;

            var emitterCount = 0;
            if (renderParticleSystem.ParticleSystem.Enabled)
            {
                foreach (var particleEmitter in emitters)
                {
                    if (particleEmitter.Enabled)
                        emitterCount++;
                }                
            }

            if (emitterCount == (renderParticleSystem.Emitters?.Length ?? 0))
                return;

            // Remove old emitters
            if (renderParticleSystem.Emitters != null)
            {
                foreach (var renderEmitter in renderParticleSystem.Emitters)
                {
                    VisibilityGroup.RenderObjects.Remove(renderEmitter);
                }
            }

            renderParticleSystem.Emitters = null;

            // Add new emitters
            var enabledEmitterIndex = 0;
            var renderEmitters = new RenderParticleEmitter[emitterCount];
            for (int index = 0; index < emitterCount; index++)
            {
                while (enabledEmitterIndex < emitters.Count && !emitters[enabledEmitterIndex].Enabled)
                    enabledEmitterIndex++;

                if (enabledEmitterIndex >= emitters.Count)
                    continue;

                var renderEmitter = new RenderParticleEmitter
                {
                    ParticleEmitter = emitters[enabledEmitterIndex],
                    RenderParticleSystem = renderParticleSystem,
                };

                renderEmitters[index] = renderEmitter;
                VisibilityGroup.RenderObjects.Add(renderEmitter);

                enabledEmitterIndex++;
            }

            renderParticleSystem.Emitters = renderEmitters;
        }

        public override void Draw(RenderContext context)
        {
            foreach (var componentData in ComponentDatas)
            {
                var particleSystemComponent = componentData.Key;
                var renderSystem = componentData.Value;

                CheckEmitters(renderSystem);

                // Update render objects
                foreach (var emitter in renderSystem.Emitters)
                {
                    if ((emitter.Enabled = particleSystemComponent.Enabled) == true)
                    {
                        var aabb = emitter.RenderParticleSystem.ParticleSystem.GetAABB();
                        emitter.BoundingBox = new BoundingBoxExt(aabb.Minimum, aabb.Maximum);
                        emitter.StateSortKey = ((uint) emitter.ParticleEmitter.DrawPriority) << 16;     // Maybe include the RenderStage precision as well
                        emitter.Color = particleSystemComponent.Color;
                        emitter.RenderGroup = particleSystemComponent.RenderGroup;
                    }
                }
            }
        }

        protected override void OnEntityComponentAdding(Entity entity, ParticleSystemComponent particleSystemComponent, RenderParticleSystem renderParticleSystem)
        {
            var emitters = particleSystemComponent.ParticleSystem.Emitters;
            var emitterCount = emitters.Count;
            var renderEmitters = new RenderParticleEmitter[emitterCount];

            for (int index = 0; index < emitterCount; index++)
            {
                var renderEmitter = new RenderParticleEmitter
                {
                    ParticleEmitter = emitters[index],
                    RenderParticleSystem = renderParticleSystem,
                    RenderGroup = particleSystemComponent.RenderGroup,
                };

                renderEmitters[index] = renderEmitter;
                VisibilityGroup.RenderObjects.Add(renderEmitter);
            }

            renderParticleSystem.Emitters = renderEmitters;
        }

        protected override void OnEntityComponentRemoved(Entity entity, ParticleSystemComponent particleSystemComponent, RenderParticleSystem renderParticleSystem)
        {
            foreach (var emitter in renderParticleSystem.Emitters)
            {
                VisibilityGroup.RenderObjects.Remove(emitter);
            }
        }

        protected override RenderParticleSystem GenerateComponentData(Entity entity, ParticleSystemComponent particleSystemComponent)
        {
            return new RenderParticleSystem
            {
                ParticleSystem = particleSystemComponent.ParticleSystem,
            };
        }

        protected override bool IsAssociatedDataValid(Entity entity, ParticleSystemComponent spriteComponent, RenderParticleSystem associatedData)
        {
            return true;
        }
    }
}
