// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;

namespace Stride.Particles.Components
{
    class ParticleSystemSimulationProcessor : EntityProcessor<ParticleSystemComponent, ParticleSystemSimulationProcessor.ParticleSystemComponentState>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParticleSystemSimulationProcessor"/> class.
        /// </summary>
        public ParticleSystemSimulationProcessor()
            : base(typeof(TransformComponent))  //  Only list the additional required components
        {
            ParticleSystems = new List<ParticleSystemComponentState>();
        }

        /// <inheritdoc/>
        protected override void OnSystemAdd()
        {
            // Create or reference systems
            //var game = Services.GetSafeServiceAs<IGame>();
            //game.GameSystems.Add(particleEngine);
            //var graphicsDevice = Services.GetSafeServiceAs<IGraphicsDeviceService>()?.GraphicsDevice;
            //var sceneSystem = Services.GetSafeServiceAs<SceneSystem>();
        }

        /// <inheritdoc />
        protected override void OnSystemRemove()
        {
            // Clean-up particleEngine
        }

        /// <inheritdoc />
        protected override void OnEntityComponentAdding(Entity entity, ParticleSystemComponent component, ParticleSystemComponentState data)
        {
            // Do some particle system initialization
        }

        /// <inheritdoc />
        protected override void OnEntityComponentRemoved(Entity entity, ParticleSystemComponent component, ParticleSystemComponentState data)
        {
            // TODO: Reset low-level data only. This method also gets called when moving the entity in the hierarchy!
            // component.ParticleSystem.Dispose();
            component.ParticleSystem.ResetSimulation();
        }

        public List<ParticleSystemComponentState> ParticleSystems { get; private set; }

        private void UpdateParticleSystem(ParticleSystemComponentState state, float deltaTime)
        {
            var speed = state.ParticleSystemComponent.Speed;
            var transformComponent = state.TransformComponent;
            var particleSystem = state.ParticleSystemComponent.ParticleSystem;

            // We must update the TRS location of the particle system prior to updating the system itself.
            // Particles only handle uniform scale.

            if (transformComponent.Parent == null)
            {
                // The transform doesn't have a parent. Local transform IS world transform

                particleSystem.Translation = transformComponent.Position;   // This is the local position!

                particleSystem.UniformScale = transformComponent.Scale.X;   // This is the local scale!

                particleSystem.Rotation = transformComponent.Rotation;      // This is the local rotation!
            }
            else
            {
                Vector3 dummyVector;
                transformComponent.WorldMatrix.Decompose(out dummyVector, out particleSystem.Rotation, out particleSystem.Translation);

                // Rotation breaks uniform scaling, so only inherit the X-scaling manually
                float xScale = transformComponent.Scale.X;
                var nextParent = transformComponent.Parent;
                while (nextParent != null)
                {
                    xScale *= nextParent.Scale.X;
                    nextParent = nextParent.Parent;
                }
                particleSystem.UniformScale = xScale;
            }

            particleSystem.Update(deltaTime * speed);
        }

        /// <inheritdoc />
        public override void Draw(RenderContext context)
        {
            float deltaTime = (float) context.Time.WarpElapsed.TotalSeconds;

            ParticleSystems.Clear();
            foreach (var particleSystemStateKeyPair in ComponentDatas)
            {
                if (particleSystemStateKeyPair.Value.ParticleSystemComponent.Enabled)
                {
                    // Exposed variables

                    if (!particleSystemStateKeyPair.Value.ParticleSystemComponent.ParticleSystem.Enabled)
                        continue;

                    ParticleSystems.Add(particleSystemStateKeyPair.Value);
                }
            }

            Dispatcher.ForEach(ParticleSystems, state => UpdateParticleSystem(state, deltaTime));
        }

        /// <inheritdoc />
        protected override ParticleSystemComponentState GenerateComponentData(Entity entity, ParticleSystemComponent component)
        {
            return new ParticleSystemComponentState
            {
                ParticleSystemComponent = component,
                TransformComponent = entity.Transform,
            };
        }

        /// <inheritdoc />
        protected override bool IsAssociatedDataValid(Entity entity, ParticleSystemComponent component, ParticleSystemComponentState associatedData)
        {
            return
                component == associatedData.ParticleSystemComponent &&
                entity.Transform == associatedData.TransformComponent;
        }

        /// <summary>
        /// Base component state for this processor. Every particle system requires a locator, so the <see cref="TransformComponent"/> is mandatory
        /// </summary>
        public class ParticleSystemComponentState
        {
            public ParticleSystemComponent ParticleSystemComponent;

            public TransformComponent TransformComponent;
        }
    }
}
