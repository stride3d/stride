// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
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
    /// A script that manages the view gizmo in the scene editor.
    /// </summary>
    /// <remarks>
    /// The view gizmo sets the viewport to the top/right corner of the window before rendering. 
    /// This is done in order to avoid deformations at frustum extremities when rendering the gizmo.
    /// </remarks>
    public class CameraOrientationGizmo : AxialGizmo
    {
        private readonly EditorGameCameraOrientationService service;
        private readonly EntityHierarchyEditorGame game;

        public const RenderGroup ViewGizmoGroup = RenderGroup.Group6;
        private const RenderGroupMask ViewGizmoGroupMask = RenderGroupMask.Group6;

        private static readonly FaceData[] Faces =
        {
            new FaceData("Right", new Vector3(0, MathUtil.PiOverTwo, 0)),
            new FaceData("Left", new Vector3(0, -MathUtil.PiOverTwo, 0)),
            new FaceData("Top", new Vector3(-MathUtil.PiOverTwo, 0, 0)),
            new FaceData("Bottom", new Vector3(MathUtil.PiOverTwo, 0, 0)),
            new FaceData("Back", Vector3.Zero),
            new FaceData("Front", new Vector3(0, MathUtil.Pi, 0))
        };

        /// <summary>
        /// The size of the viewport used to draw the view gizmo is ViewPortFactor * DefaultSize
        /// </summary>
        private const float ViewPortFactor = 4;

        private const float VerticalFieldOfView = 25;

        private const float CameraDistanceFactor = 12.0f;

        /// <summary>
        /// The default size of the view gizmo in pixels.
        /// </summary>
        private const int DefaultSize = 25;

        private const float FontSize = 7;

        private const float TextScale = 0.08f;

        private const float OuterExtent = 0.25f;

        private const float InnerExtent = 0.15f;

        private const float Border = OuterExtent - InnerExtent;

        private const int ViewportSize = (int)(ViewPortFactor * DefaultSize);

        private CameraComponent cameraComponent;

        private GizmoViewportRenderer gizmoViewportRenderer;

        private readonly List<Entity> entities = new List<Entity>();

        private int selectedElementIndex;

        private SpriteBatch spriteBatch;

        private Graphics.SpriteFont defaultFont;

        /// <summary>
        /// The default material
        /// </summary>
        protected MaterialInstance DefaultMaterial;

        /// <summary>
        /// The default material for a selected element
        /// </summary>
        protected MaterialInstance ElementSelectedMaterial;

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraOrientationGizmo"/> class.
        /// </summary>
        /// <param name="service">The service that is using this gizmo.</param>
        /// <param name="game">The scene game.</param>
        internal CameraOrientationGizmo(EditorGameCameraOrientationService service, EntityHierarchyEditorGame game)
        {
            RenderGroup = ViewGizmoGroup;
            this.service = service;
            this.game = game;
        }

        public Int3 SelectedElement { get; private set; }

        public bool HasSelection => selectedElementIndex >= 0;

        public bool IsViewParallelToAxis { get; private set; }

        private void UpdateCamera()
        {
            var view = service.Camera.ViewMatrix;
            view.TranslationVector = new Vector3(0, 0, -OuterExtent * CameraDistanceFactor);
            cameraComponent.ViewMatrix = view;
        }

        public override bool IsUnderMouse(int pickedComponentId)
        {
            return IsUnderMouse();
        }

        public bool IsUnderMouse()
        {
            return selectedElementIndex >= 0;
        }

        public void Update()
        {
            if (!IsEnabled)
                return;

            UpdateCamera();
            UpdateSelection();
            UpdateColors();
        }

        protected static void UpdateSelectionOnCloserIntersection(BoundingBox box, Ray clickRay, Int3 element, ref float minHitDistance, ref Int3 newSelection)
        {
            float hitDistance;
            if (box.Intersects(ref clickRay, out hitDistance) && hitDistance < minHitDistance)
            {
                minHitDistance = hitDistance;
                newSelection = element;
            }
        }

        private void UpdateSelection()
        {
            SelectedElement = Int3.Zero;
            selectedElementIndex = -1;

            // Calculate the ray in the gizmo space
            var viewInverse = Matrix.Invert(cameraComponent.ViewMatrix);
            var viewport = gizmoViewportRenderer.Viewport;
            var mousePosition = (Input.MousePosition * gizmoViewportRenderer.OutputSize - new Vector2(viewport.X, viewport.Y)) / ViewportSize;
            var clickRay = EditorGameHelper.CalculateRayFromMousePosition(cameraComponent, mousePosition, viewInverse);

            var minHitDistance = float.PositiveInfinity;

            // Select the cube whose intersection is the closest
            var i = 0;
            for (var z = -1; z <= 1; z++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    for (var x = -1; x <= 1; x++)
                    {
                        var element = new Int3(x, y, z);
                        if (element == Int3.Zero)
                            continue;

                        var minimum = new Vector3(GetMinimum(x), GetMinimum(y), GetMinimum(z));
                        var maximum = new Vector3(GetMaximum(x), GetMaximum(y), GetMaximum(z));

                        float hitDistance;
                        if (new BoundingBox(minimum, maximum).Intersects(ref clickRay, out hitDistance) && hitDistance < minHitDistance)
                        {
                            minHitDistance = hitDistance;
                            SelectedElement = element;
                            selectedElementIndex = i;
                        }

                        i++;
                    }
                }
            }

            var viewDirection = viewInverse.Forward;
            IsViewParallelToAxis = MathUtil.WithinEpsilon(Math.Abs(viewDirection.X) + Math.Abs(viewDirection.Y) + Math.Abs(viewDirection.Z), 1.0f, 1e-4f);
        }

        private static float GetMaximum(int index)
        {
            return -GetMinimum(-index);
        }

        private static float GetMinimum(int index)
        {
            switch (index)
            {
                case -1:
                    return -(InnerExtent + Border);
                case 0:
                    return -InnerExtent;
                case 1:
                    return InnerExtent;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        protected override void UpdateColors()
        {
            base.UpdateColors();

            for (var i = 0; i < entities.Count; i++)
            {
                entities[i].Get<ModelComponent>().Model.Materials[0] = selectedElementIndex == i ? ElementSelectedMaterial : DefaultMaterial;
            }
        }


        protected override Entity Create()
        {
            base.Create();

            DefaultMaterial = CreateUniformColorMaterial(Color.White);
            ElementSelectedMaterial = CreateUniformColorMaterial(Color.Gold);

            var entity = new Entity("View Gizmo");
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

            // create the default fonts
            var fontItem = OfflineRasterizedSpriteFontFactory.Create();
            fontItem.FontType.Size = FontSize;
            defaultFont = OfflineRasterizedFontCompiler.Compile(Services.GetService<IFontFactory>(), fontItem, GraphicsDevice.ColorSpace == ColorSpace.Linear);

            // create the sprite batch use to draw text
            spriteBatch = new SpriteBatch(GraphicsDevice) { DefaultDepth = 1 };

            // Add a renderer on the top right size
            var cameraOrientationGizmoRenderStage = new RenderStage("CameraOrientationGizmo", "Main");
            game.EditorSceneSystem.GraphicsCompositor.RenderStages.Add(cameraOrientationGizmoRenderStage);

            var meshRenderFeature = game.EditorSceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>().First();
            meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
            {
                EffectName = EditorGraphicsCompositorHelper.EditorForwardShadingEffect,
                RenderGroup = ViewGizmoGroupMask,
                RenderStage = cameraOrientationGizmoRenderStage
            });

            var editorCompositor = (EditorTopLevelCompositor)game.EditorSceneSystem.GraphicsCompositor.Game;
            editorCompositor.PostGizmoCompositors.Add(new ClearRenderer { ClearFlags = ClearRendererFlags.DepthOnly });
            editorCompositor.PostGizmoCompositors.Add(gizmoViewportRenderer = new GizmoViewportRenderer
            {
                Name = "Render Camera Orientation",
                ViewportSize = ViewportSize,
                ViewportPosition = new Vector2(1.0f, 0.0f),
                Camera = cameraComponent,
                Content = new SceneRendererCollection
                {
                    new SingleStageRenderer { RenderStage = cameraOrientationGizmoRenderStage },
                    new DelegateSceneRenderer(RenderFaceNames),
                },
            });

            var cubeMesh = GeometricPrimitive.Cube.New(GraphicsDevice).ToMeshDraw();

            int i = 0;
            for (var z = -1; z <= 1; z++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    for (var x = -1; x <= 1; x++)
                    {
                        if (x == 0 && y == 0 && z == 0)
                            continue;

                        var cubeEntity = new Entity("CubeEntity" + i++) { new ModelComponent { Model = new Model { x == 0 ? ElementSelectedMaterial : DefaultMaterial, new Mesh { Draw = cubeMesh } }, RenderGroup = RenderGroup } };

                        cubeEntity.Transform.Scale = new Vector3(x == 0 ? InnerExtent * 2 : Border, y == 0 ? InnerExtent * 2 : Border, z == 0 ? InnerExtent * 2 : Border);
                        cubeEntity.Transform.Position = new Vector3(x, y, z) * (InnerExtent + Border / 2);

                        entity.AddChild(cubeEntity);
                        entities.Add(cubeEntity);
                    }
                }
            }

            return entity;
        }

        protected override void Destroy()
        {
            defaultFont.Dispose();

            base.Destroy();
        }

        private void RenderFaceNames(RenderDrawContext context)
        {
            var viewPortSize = new Vector2(ViewportSize, ViewportSize);

            cameraComponent.Update();
            var projectionMatrix = cameraComponent.ProjectionMatrix;
            var viewMatrix = cameraComponent.ViewMatrix;

            var textureToWorldSpace = Matrix.RotationX(MathUtil.Pi) * Matrix.Translation(0, 0, OuterExtent);

            foreach (var face in Faces)
            {
                var text = face.Name.ToUpperInvariant();

                spriteBatch.Begin(context.GraphicsContext, textureToWorldSpace * face.Rotation * viewMatrix, projectionMatrix, SpriteSortMode.BackToFront, BlendStates.AlphaBlend, context.GraphicsDevice.SamplerStates.LinearClamp, DepthStencilStates.None);
                var textSize = spriteBatch.MeasureString(defaultFont, text, viewPortSize);
                spriteBatch.DrawString(defaultFont, text, Vector2.One * 0.5f, new Color(0, 0, 0, 0.8f), 0, textSize / 2, Vector2.One / FontSize * TextScale, SpriteEffects.None, 0, TextAlignment.Center);

                spriteBatch.End();
            }
        }

        struct FaceData
        {
            public readonly string Name;

            public readonly Matrix Rotation;

            public FaceData(string name, Vector3 angles)
            {
                Name = name;
                Rotation = Matrix.RotationYawPitchRoll(angles.Y, angles.X, angles.Z);
            }
        }
    }
}
