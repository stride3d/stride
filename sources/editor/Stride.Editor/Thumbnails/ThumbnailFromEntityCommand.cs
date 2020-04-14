// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Assets;
using Stride.Editor.Engine;
using Stride.Engine;
using Stride.Graphics;
using Stride.Particles.Rendering;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Lights;
using Stride.SpriteStudio.Runtime;

namespace Stride.Editor.Thumbnails
{
    /// <summary>
    /// A command that creates the thumbnail by rendering an entity.
    /// </summary>
    /// <typeparam name="TRuntimeAsset">The runtime type of the asset</typeparam>
    public abstract class ThumbnailFromEntityCommand<TRuntimeAsset> : StrideThumbnailCommand<TRuntimeAsset>
        where TRuntimeAsset : class
    {
        private static readonly string ThumbnailEntityGraphicsCompositorKey = nameof(ThumbnailEntityGraphicsCompositorKey);
        public const string EditorForwardShadingEffect = "StrideEditorForwardShadingEffect";

        /// <summary>
        /// The root entity used for the thumbnail.
        /// </summary>
        protected Entity Entity;

        protected ThumbnailFromEntityCommand(ThumbnailCompilerContext context, AssetItem assetItem, IAssetFinder assetFinder, string url, ThumbnailCommandParameters parameters)
            : base(context, assetItem, assetFinder, url, parameters)
        {
        }

        /// <inheritdoc/>
        protected override string GraphicsCompositorKey => ThumbnailEntityGraphicsCompositorKey + "_" + ModelEffectName;

        protected virtual string ModelEffectName => EditorForwardShadingEffect;

        /// <inheritdoc/>
        protected override GraphicsCompositor CreateSharedGraphicsCompositor(GraphicsDevice device)
        {
            var result = GraphicsCompositorHelper.CreateDefault(false, ModelEffectName, null, ThumbnailBackgroundColor);

            var opaqueStage = result.RenderStages.First(x => x.Name.Equals("Opaque"));
            var transparentStage = result.RenderStages.First(x => x.Name.Equals("Transparent"));

            // Add particles, UI and SpriteStudio renderers
            result.RenderFeatures.Add(
                new ParticleEmitterRenderFeature
                {
                    RenderStageSelectors =
                    {
                        new ParticleEmitterTransparentRenderStageSelector
                        {
                            EffectName = "Particles",
                            OpaqueRenderStage = opaqueStage,
                            TransparentRenderStage = transparentStage,
                        }
                    },
                });

            result.RenderFeatures.Add(
                new SpriteStudioRenderFeature
                {
                    RenderStageSelectors =
                    {
                        new SimpleGroupToRenderStageSelector()
                        {
                            EffectName = "SpriteStudio",
                            RenderStage = transparentStage,
                        }
                    }
                });

            return result;
        }

        /// <summary>
        /// Create the entity to display in the thumbnail.
        /// </summary>
        protected abstract Entity CreateEntity();

        /// <summary>
        /// Unload and destroy the entity used in the thumbnail.
        /// </summary>
        /// <returns></returns>
        protected Task DestroyEntity()
        {
            return Task.FromResult(true);
        }

        protected override Scene CreateScene(GraphicsCompositor graphicsCompositor)
        {
            // create the entity preview scene
            var entityScene = new Scene();

            Entity = CreateEntity();
            if (Entity == null)
                return null;

            AdjustEntity();

            var camera = CreateCamera(graphicsCompositor);
            entityScene.Entities.Add(camera.Entity);

            SetupLighting(entityScene);

            entityScene.Entities.Add(Entity);

            return entityScene;
        }

        [NotNull]
        protected virtual CameraComponent CreateCamera(GraphicsCompositor graphicsCompositor)
        {
            var cameraComponent = new CameraComponent
            {
                Slot = new SceneCameraSlotId(graphicsCompositor.Cameras[0].Id),
                UseCustomAspectRatio = true,
                AspectRatio = Parameters.ThumbnailSize.X / (float)Parameters.ThumbnailSize.Y,
            };

            // setup the camera
            var cameraEntity = new Entity("Thumbnail Camera") { cameraComponent };
            var cameraToFront = new Vector2(1f / (float)Math.Tan(MathUtil.DegreesToRadians(cameraComponent.VerticalFieldOfView / 2 * cameraComponent.AspectRatio)),
                1f / (float)Math.Tan(MathUtil.DegreesToRadians(cameraComponent.VerticalFieldOfView / 2)));
            var cameraDistanceFromCenter = 1f + Math.Max(cameraToFront.X, cameraToFront.Y); // we want the front face of the element to be fully visible (not only center)
            cameraEntity.Transform.Position = new Vector3(0, 0, cameraDistanceFromCenter);

            // rotate a bit the camera to have a nice viewing angle.
            var rotationQuaternion = Quaternion.RotationX(-MathUtil.Pi / 6) * Quaternion.RotationY(-MathUtil.Pi / 4);
            rotationQuaternion.Rotate(ref cameraEntity.Transform.Position);
            cameraEntity.Transform.Rotation = Quaternion.RotationX(-MathUtil.Pi / 6) * Quaternion.RotationY(-MathUtil.Pi / 4);

            cameraComponent.NearClipPlane = cameraDistanceFromCenter / 50f;
            cameraComponent.FarClipPlane = cameraDistanceFromCenter * 50f;

            return cameraComponent;
        }
        protected virtual void SetupLighting(Scene scene)
        {
            // Depending on RenderingMode, use HDR or LDR settings for the lights
            var factor = 1.0f;
            if (Parameters.RenderingMode == RenderingMode.HDR)
            {
                factor = 5.0f;
            }

            var ambientLight = new Entity("Thumbnail Ambient Light1") { new LightComponent { Type = new LightAmbient(), Intensity = 0.02f } };
            var frontDirectionalLight = new Entity("Thumbnail Directional Front") { new LightComponent { Intensity = 0.07f * factor } };
            var topDirectionalLight = new Entity("Thumbnail Directional Top") { new LightComponent { Intensity = 0.8f * factor } };
            topDirectionalLight.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(-80));
            scene.Entities.Add(ambientLight);
            scene.Entities.Add(frontDirectionalLight);
            scene.Entities.Add(topDirectionalLight);
        }

        protected virtual void AdjustEntity()
        {
            var boundingSphere = Entity.CalculateBoundSphere();

            Entity.Transform.Scale = boundingSphere.Radius > MathUtil.ZeroTolerance ? new Vector3(1f / boundingSphere.Radius) : Vector3.One;
            Entity.Transform.Position = -Entity.Transform.Scale * boundingSphere.Center;
        }

        protected override void DestroyScene(Scene scene)
        {
            base.DestroyScene(scene);

            scene.Entities.Remove(Entity);
            scene.Dispose();
            DestroyEntity();
        }
    }
}
