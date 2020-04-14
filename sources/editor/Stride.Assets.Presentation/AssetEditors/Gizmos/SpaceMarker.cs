// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.SpriteFont;
using Stride.Assets.SpriteFont.Compiler;
using Stride.Engine;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.Font;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// A script that manages the space marker in the scene editor.
    /// </summary>
    /// <remarks>
    /// The space marker sets the viewport to the left/bottom corner of the window before rendering. 
    /// This is done in order to avoid deformations at frustum extremities when rendering the marker.
    /// </remarks>
    public class SpaceMarker : AxialGizmo
    {
        private readonly EntityHierarchyEditorGame game;

        public const RenderGroup SpaceMarkerGroup = RenderGroup.Group3;
        private const RenderGroupMask SpaceMarkerGroupMask = RenderGroupMask.Group3;

        /// <summary>
        /// The size of the viewport used to draw the space marker is ViewPortFactor * DefaultSize
        /// </summary>
        private const float ViewPortFactor = 4;

        private const float VerticalFieldOfView = 25;

        private const float CameraDistanceFactor = 9.0f;

        /// <summary>
        /// The default size of the space marker in pixels.
        /// </summary>
        private const int DefaultSize = 30;

        private const float ConeRadius = GizmoExtremitySize * 0.75f;

        private const float ConeHeigth = 2f * ConeRadius;

        private const float BodyRadius = 4f * ConeRadius / 7.5f;

        private const float BodyLength = 1f - ConeHeigth;

        private const int ViewportSize = (int)(ViewPortFactor * DefaultSize);

        /// <summary>
        /// The names of the axes.
        /// </summary>
        private readonly string[] axisNames = { "X", "Y", "Z" };

        /// <summary>
        /// The <see cref="Graphics.SpriteFont"/> instance used to display axis names.
        /// </summary>
        private Graphics.SpriteFont defaultFont;

        /// <summary>
        /// The <see cref="SpriteBatch"/> used to display axis names.
        /// </summary>
        private SpriteBatch spriteBatch;

        /// <summary>
        /// The projection matrix used to render the space marker.
        /// </summary>
        //private Matrix projectionMatrix;

        private CameraComponent cameraComponent;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpaceMarker"/> class.
        /// </summary>
        /// <param name="game">The scene game.</param>
        internal SpaceMarker(EntityHierarchyEditorGame game)
        {
            RenderGroup = SpaceMarkerGroup;
            this.game = game;
        }

        private void UpdateCamera()
        {
            var view = Game.EditorServices.Get<IEditorGameCameraService>().ViewMatrix;
            view.TranslationVector = new Vector3(0, 0, -BodyLength * CameraDistanceFactor);
            cameraComponent.ViewMatrix = view;
        }

        private void RenderSpaceMarkerAxisNames(RenderDrawContext context)
        {
            var viewPortSize = new Vector2(ViewportSize, ViewportSize);

            cameraComponent.Update();
            var projectionMatrix = cameraComponent.ProjectionMatrix;
            var viewMatrix = cameraComponent.ViewMatrix;

            spriteBatch.Begin(context.GraphicsContext, SpriteSortMode.BackToFront, depthStencilState: DepthStencilStates.None);

            for (int i = 0; i < 3; i++)
            {
                var vector = GizmoRootEntity.Transform.Position;
                vector[i] += 1.15f * GizmoRootEntity.Transform.Scale[i];

                var projectedPosition = Vector3.TransformCoordinate(vector, viewMatrix * projectionMatrix);
                var screenPosition = new Vector2(projectedPosition.X / 2 + 0.5f, 0.5f - projectedPosition.Y / 2) * viewPortSize;

                var textSize = spriteBatch.MeasureString(defaultFont, axisNames[i], viewPortSize);
                spriteBatch.DrawString(defaultFont, axisNames[i], screenPosition, Color.White, 0, textSize / 2, Vector2.One, SpriteEffects.None, 0.5f - projectedPosition.Z, TextAlignment.Center);
            }

            spriteBatch.End();
        }

        public void Update()
        {
            UpdateColors();
            UpdateCamera();
        }

        protected override Entity Create()
        {
            base.Create();

            var entity = new Entity("Space Marker");
            cameraComponent = new CameraComponent
            {
                UseCustomAspectRatio = true,
                AspectRatio = 1.0f,
                NearClipPlane = 0.1f,
                FarClipPlane = 1000.0f,
                UseCustomViewMatrix = true,
                VerticalFieldOfView = VerticalFieldOfView
            };
            entity.Add(cameraComponent);

            // Add a renderer on the left bottom size
            var cameraOrientationGizmoRenderStage = new RenderStage("CameraOrientationGizmo", "Main");
            game.EditorSceneSystem.GraphicsCompositor.RenderStages.Add(cameraOrientationGizmoRenderStage);

            var meshRenderFeature = game.EditorSceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>().First();
            meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
            {
                EffectName = EditorGraphicsCompositorHelper.EditorForwardShadingEffect,
                RenderGroup = SpaceMarkerGroupMask,
                RenderStage = cameraOrientationGizmoRenderStage
            });

            var editorCompositor = (EditorTopLevelCompositor)game.EditorSceneSystem.GraphicsCompositor.Game;
            editorCompositor.PostGizmoCompositors.Add(new ClearRenderer { ClearFlags = ClearRendererFlags.DepthOnly });
            editorCompositor.PostGizmoCompositors.Add(new GizmoViewportRenderer
            {
                Name = "Render Spacemarker",
                ViewportSize = ViewportSize,
                ViewportPosition = new Vector2(0.0f, 1.0f),
                Camera = cameraComponent,
                Content = new SceneRendererCollection
                {
                    new SingleStageRenderer { RenderStage = cameraOrientationGizmoRenderStage },
                    new DelegateSceneRenderer(RenderSpaceMarkerAxisNames),
                },
            });

            // create the default fonts
            var fontItem = OfflineRasterizedSpriteFontFactory.Create();
            fontItem.FontType.Size = 8;
            defaultFont = OfflineRasterizedFontCompiler.Compile(Services.GetService<IFontFactory>(), fontItem, GraphicsDevice.ColorSpace == ColorSpace.Linear);

            // create the sprite batch use to draw text
            spriteBatch = new SpriteBatch(GraphicsDevice) { DefaultDepth = 1 };

            var rotations = new[] { Vector3.Zero, new Vector3(0, 0, MathUtil.Pi / 2), new Vector3(0, -MathUtil.Pi / 2f, 0) };
            var coneMesh = GeometricPrimitive.Cone.New(GraphicsDevice, ConeRadius, ConeHeigth, GizmoTessellation).ToMeshDraw();
            var bodyMesh = GeometricPrimitive.Cylinder.New(GraphicsDevice, BodyLength, BodyRadius, GizmoTessellation).ToMeshDraw();

            // create the axis arrows 
            for (int axis = 0; axis < 3; ++axis)
            {
                var material = GetAxisDefaultMaterial(axis);
                var coneEntity = new Entity("ArrowCone" + axis) { new ModelComponent { Model = new Model { material, new Mesh { Draw = coneMesh } }, RenderGroup = RenderGroup } };
                var bodyEntity = new Entity("ArrowBody" + axis) { new ModelComponent { Model = new Model { material, new Mesh { Draw = bodyMesh } }, RenderGroup = RenderGroup } };

                coneEntity.Transform.Position.X = BodyLength + ConeHeigth * 0.5f;
                bodyEntity.Transform.Position.X = BodyLength / 2;
                coneEntity.Transform.Rotation = Quaternion.RotationZ(-MathUtil.Pi / 2);
                bodyEntity.Transform.RotationEulerXYZ = -MathUtil.Pi / 2 * Vector3.UnitZ;

                // create the arrow entity composed of the cone and bode
                var arrowEntity = new Entity("ArrowEntity" + axis);
                arrowEntity.Transform.Children.Add(coneEntity.Transform);
                arrowEntity.Transform.Children.Add(bodyEntity.Transform);
                arrowEntity.Transform.RotationEulerXYZ = rotations[axis];

                // Add the arrow entity to the gizmo entity
                entity.AddChild(arrowEntity);
            }

            return entity;
        }

        protected override void Destroy()
        {
            defaultFont.Dispose();

            base.Destroy();
        }
    }
}
