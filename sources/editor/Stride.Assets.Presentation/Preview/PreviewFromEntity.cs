// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Threading.Tasks;

using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.Engine;
using Stride.Editor.Preview;
using Stride.Input;
using Stride.Rendering.Lights;
using Stride.Engine;
using Stride.Rendering.Compositing;
using Stride.Particles.Rendering;
using Stride.Rendering;
using Stride.Rendering.Sprites;
using Stride.Rendering.UI;
using Stride.SpriteStudio.Runtime;

namespace Stride.Assets.Presentation.Preview
{
    /// <summary>
    /// An implementation of the <see cref="AssetPreview"/> class that simply build an asset and create an entity for it
    /// during the initialization and updates. This class can be inherited to make preview class for assets that does not
    /// require more that a compilation and an entity creation.
    /// </summary>
    /// <typeparam name="T">The type of asset this preview can display.</typeparam>
    public abstract class PreviewFromEntity<T> : BuildAssetPreview<T> where T : Asset
    {
        protected PreviewEntity PreviewEntity { get; set; }

        protected string modelEffectName;

        private readonly Scene entityScene;

        private readonly Entity camera;

        /// <summary>
        /// The script updating the position of the preview camera
        /// </summary>
        protected CameraUpdateScript CameraScript { get; }

        protected PreviewFromEntity(string modelEffectName = EditorGraphicsCompositorHelper.EditorForwardShadingEffect)
        {
            this.modelEffectName = modelEffectName;
            var cameraComponent = new CameraComponent();

            // create the entity preview scene
            entityScene = new Scene();

            // setup the camera
            CameraScript = new CameraUpdateScript();
            camera = new Entity("Preview Camera") { cameraComponent, CameraScript };
            entityScene.Entities.Add(camera);
        }

        protected override async Task Initialize()
        {
            await base.Initialize();

            SetupLighting(camera);

            CameraScript.ResetViewAngle();
        }

        protected virtual void SetupLighting(Entity camera)
        {
            // Depending on RenderingMode, use HDR or LDR settings for the lights
            var factor = 1.0f;
            if (RenderingMode == RenderingMode.HDR)
            {
                factor = 2.0f/0.9f;
            }

            var ambientLight = new Entity("Preview Ambient Light1") { new LightComponent { Type = new LightAmbient(), Intensity = 0.02f*factor } };
            var frontDirectionalLight = new Entity("Preview Directional Front") { new LightComponent { Intensity = 0.07f*factor } };
            var topDirectionalLight = new Entity("Preview Directional Top") { new LightComponent { Intensity = 0.8f*factor } };
            topDirectionalLight.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(-80));
            camera.AddChild(ambientLight);
            camera.AddChild(frontDirectionalLight);
            camera.AddChild(topDirectionalLight);
        }

        protected override Scene CreatePreviewScene()
        {
            return entityScene;
        }

        protected override GraphicsCompositor GetGraphicsCompositor()
        {
            var graphicsCompositor = GraphicsCompositorHelper.CreateDefault(RenderingMode == RenderingMode.HDR, modelEffectName, 
                camera.Get<CameraComponent>(), RenderingMode == RenderingMode.HDR ? EditorServiceGame.EditorBackgroundColorHdr : EditorServiceGame.EditorBackgroundColorLdr);

            var opaqueStage = graphicsCompositor.RenderStages.First(x => x.Name.Equals("Opaque"));
            var transparentStage = graphicsCompositor.RenderStages.First(x => x.Name.Equals("Transparent"));

            // Add particles, UI and SpriteStudio renderers
            graphicsCompositor.RenderFeatures.Add(
                new ParticleEmitterRenderFeature()
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

            graphicsCompositor.RenderFeatures.Add(
                new SpriteStudioRenderFeature()
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

            return graphicsCompositor;
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            PreviewEntity = CreatePreviewEntity();

            if (PreviewEntity?.Entity != null)
            {
                PrepareLoadedEntity();
                entityScene.Entities.Add(PreviewEntity.Entity);

                // Start script manually since ScriptProcessor is not enabled (in case some entity has script we don't want to run)
                Game.Script.Add(CameraScript);
            }
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();

            if (PreviewEntity == null)
                return;

            // Start script manually since ScriptProcessor is not enabled (in case some entity has script we don't want to run)
            Game.Script.Remove(CameraScript);

            // remove the entity from the scene.
            entityScene.Entities.Remove(PreviewEntity.Entity);

            PreviewEntity.Disposed?.Invoke();

            PreviewEntity = null;
        }

        protected virtual void PrepareLoadedEntity()
        {
            CameraScript.AdjustViewTarget(PreviewEntity?.Entity);
        }

        protected class CameraUpdateScript : SyncScript
        {
            // ReSharper disable once StaticFieldInGenericType
            private static readonly BoundingSphere InvalidBoundingSphere = new BoundingSphere(Vector3.Zero, MathUtil.ZeroTolerance);

            public float DefaultYaw;
            public float DefaultPitch;

            private float yaw;
            private float pitch;
            private float distance;
            private Vector3 target;

            private CameraComponent cameraComponent;
            private BoundingSphere previousBoundingSphere = InvalidBoundingSphere;
            
            public override void Start()
            {
                cameraComponent = Entity.Get<CameraComponent>();
            }

            public void ResetViewAngle()
            {
                yaw = DefaultYaw;
                pitch = DefaultPitch;

                UpdateComponents();
            }

            public void ResetViewTarget(Entity targetEntity)
            {
                target = Vector3.Zero;
                previousBoundingSphere = InvalidBoundingSphere;
                AdjustViewTarget(targetEntity);
            }

            public void AdjustViewTarget(Entity targetEntity)
            {
                if (targetEntity == null)
                    return;

                cameraComponent = Entity.Get<CameraComponent>();

                // reset the target and the distance only if the bounding sphere has changed.
                var boundingSphere = targetEntity.CalculateBoundSphere();
                if (boundingSphere != previousBoundingSphere)
                {
                    var radius = boundingSphere.Radius;

                    // calculate the distance to the target needed in order to see it fully
                    // Note: we want the front face of the element to be fully visible (not only center)
                    distance = radius + radius / (float)Math.Tan(MathUtil.DegreesToRadians(cameraComponent.VerticalFieldOfView / 2));
                    // Make sure the distance is greater than zero
                    distance = Math.Max(distance, 2*MathUtil.ZeroTolerance);

                    target = boundingSphere.Center;
                }
                previousBoundingSphere = boundingSphere;

                UpdateComponents();
            }

            public void SetViewDistance(float distance)
            {
                this.distance = Math.Max(0, distance);
                UpdateComponents();
            }

            public override void Update()
            {
                // if the user is pressing or releasing a new mouse button the action change and we reset the mouse origin position
                if (Input.HasReleasedMouseButtons || Input.HasPressedMouseButtons)
                {
                    if (Input.HasDownMouseButtons)
                    {
                        Input.LockMousePosition();
                        Game.IsMouseVisible = false;
                    }
                    else
                    {
                        Input.UnlockMousePosition();
                        Game.IsMouseVisible = true;
                    }
                }

                // if the user is not pushing any mouse button the camera does not change
                if (!Input.HasDownMouseButtons && Math.Abs(Input.MouseWheelDelta) < MathUtil.ZeroTolerance)
                    return;

                var translation = Input.MouseDelta;
                if (Input.IsMouseButtonDown(MouseButton.Right)) // translation
                {
                    var viewMatrixInv = Entity.Transform.WorldMatrix;
                    target -= distance * (translation.X * viewMatrixInv.Row1.XYZ() - translation.Y * viewMatrixInv.Row2.XYZ());
                }
                else if (Input.IsMouseButtonDown(MouseButton.Left)) // orbital rotation 
                {
                    yaw -= 4 * translation.X;
                    pitch -= 3 * translation.Y;
                }

                distance *= 1 / (1 + (Input.MouseWheelDelta / 8));

                UpdateComponents();
            }

            public void UpdateComponents()
            {
                cameraComponent = Entity.Get<CameraComponent>();
                if (cameraComponent != null)
                {
                    var maxPitch = MathUtil.PiOverTwo - 0.01f;
                    pitch = MathUtil.Clamp(pitch, -maxPitch, maxPitch);

                    var offset = new Vector3(0, 0, distance);
                    var cameraPosition = target + Vector3.Transform(offset, Quaternion.RotationYawPitchRoll(yaw, pitch, 0));

                    Entity.Transform.UseTRS = false;
                    Entity.Transform.LocalMatrix = Matrix.Invert(Matrix.LookAtRH(cameraPosition, target, Vector3.UnitY));
                    cameraComponent.FarClipPlane = 2.5f * Math.Max(distance, previousBoundingSphere.Radius);
                    cameraComponent.NearClipPlane = cameraComponent.FarClipPlane / 1000f;
                }
            }
        }

        /// <summary>
        /// Reset the camera to its default state.
        /// </summary>
        public async void ResetCamera()
        {
            await IsInitialized();

            CameraScript.ResetViewAngle();
            CameraScript.ResetViewTarget(PreviewEntity?.Entity);
        }

        /// <summary>
        /// Generates a string that can be used as a name for the entity that will be created for this preview.
        /// </summary>
        /// <returns></returns>
        protected string BuildName()
        {
            var description = DisplayAttribute.GetDisplay(typeof(T));
            string typeDisplayName = description != null && !string.IsNullOrEmpty(description.Name) ? description.Name : typeof(T).Name;
            return string.Format("Preview entity for {0} '{1}'", typeDisplayName, AssetItem.Location);
        }

        /// <summary>
        /// Creates an entity that will be used for the preview.
        /// </summary>
        /// <returns>An instance of the <see cref="Editor.Preview.PreviewEntity"/> class</returns>
        protected abstract PreviewEntity CreatePreviewEntity();
    }
}
