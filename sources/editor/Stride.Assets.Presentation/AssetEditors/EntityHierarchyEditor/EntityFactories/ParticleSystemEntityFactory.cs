// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Animations;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Engine;
using Stride.Particles;
using Stride.Particles.Components;
using Stride.Particles.Initializers;
using Stride.Particles.Modules;
using Stride.Particles.ShapeBuilders;
using Stride.Particles.Spawners;
using Stride.Particles.Updaters;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories
{

    [Display(10, "Empty particle system", "Particle system")]
    public class EmptyParticleSystemEntityFactory : EntityFactory
    {
        [ModuleInitializer]
        internal static void RegisterCategory()
        {
            EntityFactoryCategory.RegisterCategory(50, "Particle system");
        }

        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "Empty particle system");
            var component = new ParticleSystemComponent();
            return CreateEntityWithComponent(name, component);
        }
    }

    [Display(20, "Simple particle system", "Particle system")]
    public class SimpleParticleSystemEntityFactory : EntityFactory
    {
        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "Simple particle system");

            var component = new ParticleSystemComponent();
            var emitter = new ParticleEmitter { ParticleLifetime = new Vector2(1, 2) };

            // 100 Particles per second
            var spawner = new SpawnerPerSecond();
            emitter.Spawners.Add(spawner);

            // Size
            var randSize = new InitialSizeSeed { RandomSize = new Vector2(0.05f, 0.1f) };
            emitter.Initializers.Add(randSize);

            // Position
            emitter.Initializers.Add(new InitialPositionSeed());

            // Velocity
            var randVel = new InitialVelocitySeed
            {
                VelocityMin = new Vector3(-0.5f, 1f, -0.5f),
                VelocityMax = new Vector3(0.5f, 3, 0.5f)
            };
            emitter.Initializers.Add(randVel);

            // Gravity
            var gravity = new UpdaterForceField
            {
                EnergyConservation = 1f,
                FieldShape = null
            };
            gravity.FieldFalloff.StrengthInside = gravity.FieldFalloff.StrengthOutside = 1f;
            gravity.ForceFixed = new Vector3(0, -9.81f, 0);
            gravity.ForceDirected = gravity.ForceRepulsive = gravity.ForceVortex = 0f;
            emitter.Updaters.Add(gravity);

            component.ParticleSystem.Emitters.Add(emitter);

            return CreateEntityWithComponent(name, component);
        }
    }


    [Display(30, "Fountain particle system", "Particle system")]
    public class SmokeParticleSystemEntityFactory : EntityFactory
    {
        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "Smoke particle system");

            var component = new ParticleSystemComponent();
            var emitter = new ParticleEmitter { ParticleLifetime = new Vector2(1, 2) };


            // 20 Particle per second
            var spawner = new SpawnerPerSecond { SpawnCount = 20 };
            emitter.Spawners.Add(spawner);

            // Size
            var randSize = new InitialSizeSeed { RandomSize = new Vector2(0.35f, 0.55f) };
            emitter.Initializers.Add(randSize);

            // Position
            var randPos = new InitialPositionSeed
            {
                PositionMin = new Vector3(-0.2f, 0, -0.2f),
                PositionMax = new Vector3(0.2f, 0, 0.2f)
            };
            emitter.Initializers.Add(randPos);

            // Velocity
            var randVel = new InitialVelocitySeed
            {
                VelocityMin = new Vector3(-0.5f, 1f, -0.5f),
                VelocityMax = new Vector3(0.5f, 3, 0.5f)
            };
            emitter.Initializers.Add(randVel);

            component.ParticleSystem.Emitters.Add(emitter);

            return CreateEntityWithComponent(name, component);
        }
    }


    [Display(40, "Ribbon particle system", "Particle system")]
    public class RibbonParticleSystemEntityFactory : EntityFactory
    {
        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "Ribbon particle system");

            var component = new ParticleSystemComponent();
            var emitter = new ParticleEmitter { ParticleLifetime = new Vector2(2, 2) };


            // 30 Particles per second
            var spawner = new SpawnerPerSecond { SpawnCount = 30 };
            emitter.Spawners.Add(spawner);

            // Ribbon
            var ribbonShape = new ShapeBuilderRibbon
            {
                SmoothingPolicy = SmoothingPolicy.Best,
                Segments = 15,
                TextureCoordinatePolicy = TextureCoordinatePolicy.Stretched,
                TexCoordsFactor = 1f
            };
            emitter.ShapeBuilder = ribbonShape;

            // Velocity
            var randVel = new InitialVelocitySeed
            {
                VelocityMin = new Vector3(-0.15f, 3f, -0.15f),
                VelocityMax = new Vector3(0.15f, 3, 0.15f)
            };
            emitter.Initializers.Add(randVel);

            // Spawn Order
            var initialOrder = new InitialSpawnOrder();
            emitter.Initializers.Add(initialOrder);

            // Size by Lifetime
            var sizeCurve = new ComputeAnimationCurveFloat();
            var key0 = new AnimationKeyFrame<float>
            {
                Key = 0,
                Value = 0.1f
            };
            var key1 = new AnimationKeyFrame<float>
            {
                Key = 0.9f,
                Value = 0f
            };
            sizeCurve.KeyFrames.Add(key0);
            sizeCurve.KeyFrames.Add(key1);

            var sizeAnimation = new UpdaterSizeOverTime { SamplerMain = { Curve = sizeCurve } };
            emitter.Updaters.Add(sizeAnimation);

            emitter.SortingPolicy = EmitterSortingPolicy.ByOrder;

            component.ParticleSystem.Emitters.Add(emitter);

            return CreateEntityWithComponent(name, component);
        }
    }
}
