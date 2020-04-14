// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Mathematics;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.Game;
using Xenko.Engine;
using Xenko.Extensions;
using Xenko.Graphics;
using Xenko.Graphics.GeometricPrimitives;
using Xenko.Rendering;
using Xenko.Rendering.Materials;
using Xenko.Rendering.Materials.ComputeColors;

namespace Xenko.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// A script that manages the grid in the scene editor.
    /// </summary>
    public class ViewportGridGizmo : GridGizmoBase
    {
        private const float MaximumViewAngle = 25f * MathUtil.Pi / 180f;
        private static readonly Vector3 DefaultUpVector = Vector3.UnitY;
        private const float GridVerticalOffset = 0.0175f;
        private const int GridTextureTopSize = 256;

        private List<Entity> originAxes = new List<Entity>();
        private Entity grid;
        private Entity originAxis;

        private delegate Image ImageBuilder();

        /// <summary>
        /// The material used by the grid.
        /// </summary>
        private Material GridMaterial { get; set; }
        
        protected override Entity Create()
        {
            var rootEntity =  base.Create();
            
            // Create the grid
            var gridTexture = CreateTexture(GenerateGridImage);
            GridMaterial = CreateColoredTextureMaterial(gridTexture, GridSize * GridSize);
            GridMaterial.Passes[0].Parameters.Set(TexturingKeys.Texture0, gridTexture);
            GridMaterial.Passes[0].Parameters.Set(MaterialKeys.Sampler.ComposeWith("i0"), GraphicsDevice.SamplerStates.AnisotropicWrap);
            grid = new Entity("Scene grid mesh")
            {
                new ModelComponent
                {
                    Model = new Model
                    {
                        GridMaterial,
                        new Mesh { Draw = GeometricPrimitive.Plane.New(GraphicsDevice, GridBase * GridSize, GridBase * GridSize).ToMeshDraw() }
                    },
                    RenderGroup = RenderGroup,
                }
            };

            // Create the grid origin
            originAxis = new Entity("The grid origin axes");
            originAxes.Add(CreateOriginAxis(Color.Red, 0));
            originAxes.Add(CreateOriginAxis(Color.Green, 1));
            originAxes.Add(CreateOriginAxis(Color.Blue, 2));
            for (int i = 0; i < 3; i++)
                originAxis.AddChild(originAxes[i]);

            // create the hierarchy of grid elements
            rootEntity.AddChild(grid);
            rootEntity.AddChild(originAxis);

            return rootEntity;
        }

        private Entity CreateOriginAxis(Color color, int axis)
        {
            var axisColor = color.ToColor3();
            if (GraphicsDevice != null)
                axisColor = color.ToColor3().ToColorSpace(GraphicsDevice.ColorSpace);

            var gridTexture = CreateTexture(GenerateAxisImage);
            var axisMaterial = CreateColoredTextureMaterial(gridTexture, 1);
            axisMaterial.Passes[0].Parameters.Set(TexturingKeys.Texture0, gridTexture);
            axisMaterial.Passes[0].Parameters.Set(MaterialKeys.Sampler.ComposeWith("i0"), GraphicsDevice.SamplerStates.AnisotropicWrap);
            axisMaterial.Passes[0].Parameters.Set(GridColorKey, Color4.PremultiplyAlpha(new Color4(axisColor, 1f)));

            var axisEntity = new Entity("Scene grid origin axis")
            {
                new ModelComponent
                {
                    Model = new Model
                    {
                        axisMaterial,
                        new Mesh { Draw = GeometricPrimitive.Plane.New(GraphicsDevice, GridBase, GridBase).ToMeshDraw() }
                    },
                    RenderGroup = RenderGroup,
                }
            };

            axisEntity.TransformValue.Scale = new Vector3(GridSize, 1f / GridBase, 1f / GridBase);
            if (axis != 0)
                axisEntity.TransformValue.Rotation = Quaternion.RotationX(MathUtil.PiOverTwo) * Quaternion.RotationAxis(new Vector3 { [1 + (axis%2)] = 1f}, MathUtil.PiOverTwo);

            var axisEntityRoot = new Entity("Scene grid origin axis root");
            axisEntityRoot.AddChild(axisEntity);

            return axisEntityRoot;
        }

        private Texture CreateTexture(ImageBuilder imageBuilder)
        {
            var gridImage = imageBuilder();
            var gridTexture = Texture.New(GraphicsDevice, gridImage);
            gridImage.Dispose();

            gridTexture.Reload += @base =>
            {
                var newImage = imageBuilder();
                gridTexture.Recreate(newImage.ToDataBox());
                newImage.Dispose();
            };

            return gridTexture;
        }

        private Material CreateColoredTextureMaterial(Texture texture, float textureScale)
        {
            var gridComputeTexture = new ComputeTextureColor
            {
                Key = TexturingKeys.Texture0,
                Texture = texture,
                Scale = new Vector2(textureScale)
            };
            return Material.New(GraphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Emissive = new MaterialEmissiveMapFeature(new ComputeBinaryColor(new ComputeColor() { Key = GridColorKey }, gridComputeTexture, BinaryOperator.Multiply))
                    {
                        UseAlpha = true
                    },
                    Transparency = new MaterialTransparencyBlendFeature(),
                    CullMode = CullMode.None
                }
            });
        }

        protected override void UpdateBase(Color3 gridColor, float sceneUnit)
        {
            var cameraService = Game.EditorServices.Get<IEditorGameCameraService>();
            if (cameraService == null)
                return;

            // update the grid color
            GridMaterial.Passes[0].Parameters.Set(GridColorKey, Color4.PremultiplyAlpha(new Color4(gridColor, 0.35f)));
            
            // Determine the up vector depending on view matrix and projection mode
            // -> When orthographic, if we are looking along a coordinate axis, place the grid perpendicular to that axis.
            // -> Fall back to the default plane otherwise.
            var viewAxisIndex = 1;
            var upVector = DefaultUpVector;
            var viewInvert = Matrix.Invert(cameraService.ViewMatrix);
            if (cameraService.IsOrthographic)
            {
                for (var i = 0; i < 3; i++)
                {
                    var coordinateAxis = new Vector3 { [i] = 1.0f };
                    var dotProduct = Vector3.Dot(viewInvert.Forward, coordinateAxis);

                    if (Math.Abs(dotProduct) > 0.99f)
                    {
                        upVector = coordinateAxis;
                        viewAxisIndex = i;
                    }
                }
            }

            // Check if the inverted View Matrix is valid (since it will be use for mouse picking, check the translation vector only)
            if (float.IsNaN(viewInvert.TranslationVector.X)
                || float.IsNaN(viewInvert.TranslationVector.Y)
                || float.IsNaN(viewInvert.TranslationVector.Z))
            {
                return;
            }

            // The position of the grid and the origin in the scene
            var snappedPosition = Vector3.Zero;
            var originPosition = Vector3.Zero;

            // Add a small offset along the Up axis to avoid depth-fight with objects positioned at height=0
            snappedPosition[viewAxisIndex] = Math.Sign(viewInvert[3, viewAxisIndex]) * GridVerticalOffset * sceneUnit;

            // Move the grid origin in slightly in front the grid to have it in the foreground
            originPosition[viewAxisIndex] = snappedPosition[viewAxisIndex] + Math.Sign(viewInvert[3, viewAxisIndex]) * 0.001f * sceneUnit;

            // In orthographic mode, put the grid and origin in the very background of scene
            if (cameraService.IsOrthographic) 
            {
                var dotProduct = Vector3.Dot(viewInvert.Forward, upVector);
                var cameraOffset = viewInvert.TranslationVector[viewAxisIndex];
                originPosition[viewAxisIndex] = cameraOffset + Math.Sign(dotProduct) * cameraService.FarPlane * 0.90f;
                snappedPosition[viewAxisIndex] = cameraOffset + Math.Sign(dotProduct) * cameraService.FarPlane * 0.95f;
            }
            
            // Determine the intersection point of the center of the vieport with the grid plane
            var ray = EditorGameHelper.CalculateRayFromMousePosition(cameraService.Component, new Vector2(0.5f), viewInvert);
            var plane = new Plane(Vector3.Zero, upVector);
            var intersection = EditorGameHelper.ProjectOnPlaneWithLimitAngle(ray, plane, MaximumViewAngle);

            // Detemine the scale of the grid depending of the distance of the camera to the grid plane
            // For orthographic projections, use a distance close to the one, at which the perspective projection would map to the viewport area.
            var gridScale = sceneUnit;
            var distanceToGrid = cameraService.IsOrthographic ? cameraService.Component.OrthographicSize * 1.5f : (viewInvert.TranslationVector - intersection).Length();
            if (distanceToGrid < 1.5f * sceneUnit)
                gridScale = 0.1f * sceneUnit;
            if (distanceToGrid > 40f * sceneUnit)
                gridScale = 10f * sceneUnit;
            if (distanceToGrid > 400f * sceneUnit)
                gridScale = 100f * sceneUnit;

            // Snap the grid the closest possible to the intersection point
            var gridStringLineUnit = gridScale;
            for (var i = 0; i < 3; i++)
            {
                if (viewAxisIndex != i)
                    snappedPosition[i] += (float)Math.Round(intersection[i] / gridStringLineUnit) * gridStringLineUnit;
            }

            // Apply positions
            grid.Transform.Position = snappedPosition;
            originAxis.TransformValue.Position = originPosition;
            for (int axis = 0; axis < 3; axis++)
                originAxes[axis].TransformValue.Position[axis] = snappedPosition[axis];

            // Apply the scale (Note: scale cannot be applied at root or sub-position is scaled too)
            grid.Transform.Scale = new Vector3(gridScale);
            for (int axis = 0; axis < 3; axis++)
                originAxes[axis].TransformValue.Scale = new Vector3(gridScale);

            // Determine and apply the rotation to the grid and origin axis entities
            SetPlaneEntityRotation(2, upVector, grid);
            for (var axis = 0; axis < 3; axis++)
                SetPlaneEntityRotation((axis + 2) % 3, upVector, originAxes[axis]);

            // Hide the vertical axis of the origin 
            for (int axis = 0; axis < 3; axis++)
                originAxes[axis].GetChild(0).Get<ModelComponent>().Enabled = axis != viewAxisIndex;
        }

        private static void SetPlaneEntityRotation(int modelUpAxis, Vector3 upVector, Entity entity)
        {
            var axisModelUp = new Vector3 { [modelUpAxis] = 1f };
            var axisRotationVector = Vector3.Cross(axisModelUp, upVector);
            var axisRotationAngle = (float)Math.Acos(Vector3.Dot(axisModelUp, upVector));
            entity.Transform.Rotation = Quaternion.RotationAxis(axisRotationVector, axisRotationAngle);
        }

        private static Image GenerateGridImage()
        {
            var image = Image.New2D(GridTextureTopSize, GridTextureTopSize, true, PixelFormat.R8G8B8A8_UNorm_SRgb);
            image.Clear();

            var average = CalculateAverageLuminanceGrid(GridTextureTopSize);
            // set the data of the mipmaps
            for (var i = 0; i < image.PixelBuffer.Count; i++)
            {
                var pixelBuffer = image.PixelBuffer[i];

                // Calculate the intensity of the color based on the average bright pixels on the surface
                // This average intensity of a mipmap must be constant and independent of texture size
                // Apply a bump on the curve to avoid getting too dark at small mips
                // Also make the mipmaps having a stronger transparency at smaller mips so that the grid fades away when it is far
                var lumBase = (float)(average / CalculateAverageLuminanceGrid(pixelBuffer.Width));
                var colorBase = (float)Math.Pow(lumBase, 1 / 3.0);
                var alpha = (float)Math.Pow(lumBase, 1 / 3.0);
                var color = (Color)(new Color4(colorBase * alpha, colorBase * alpha, colorBase * alpha, alpha).ToSRgb());

                for (var x = 0; x < pixelBuffer.Width; x++)
                {
                    pixelBuffer.SetPixel(x, 0, color);
                    pixelBuffer.SetPixel(0, x, color);
                    pixelBuffer.SetPixel(x, pixelBuffer.Height - 1, color);
                    pixelBuffer.SetPixel(pixelBuffer.Width - 1, x, color);
                }
            }
            return image;
        }

        private static Image GenerateAxisImage()
        {
            var image = Image.New2D(GridTextureTopSize, GridTextureTopSize, true, PixelFormat.R8G8B8A8_UNorm_SRgb);
            image.Clear();

            var average = CalculateAverageLuminanceOriginAxis(GridTextureTopSize);
            // set the data of the mipmaps
            for (var i = 0; i < image.PixelBuffer.Count; i++)
            {
                var pixelBuffer = image.PixelBuffer[i];

                // Calculate the intensity of the color based on the average bright pixels on the surface
                // This average intensity of a mipmap must be constant and independent of texture size
                // Apply a bump on the curve to avoid getting too dark at small mips
                // Also make the mipmaps having a stronger transparency at smaller mips so that the grid fades away when it is far
                var lumBase = (float)(average / CalculateAverageLuminanceOriginAxis(pixelBuffer.Width));
                var colorBase = (float)Math.Pow(lumBase, 1 / 3.0);
                var alpha = (float)Math.Pow(lumBase, 1 / 3.0);
                var color = (Color)(new Color4(colorBase * alpha, colorBase * alpha, colorBase * alpha, alpha).ToSRgb());

                for (var x = 0; x < pixelBuffer.Width; x++)
                {
                    pixelBuffer.SetPixel(x, Math.Max(0, pixelBuffer.Height / 2 - 1), color);
                    pixelBuffer.SetPixel(x, pixelBuffer.Height / 2, color);
                }
            }
            return image;
        }

        private static double CalculateAverageLuminanceGrid(int size)
        {
            // It calculates the number of bright pixels vs the number total of pixels
            return 4.0 * (size - 1) / (size * size);
        }

        private static double CalculateAverageLuminanceOriginAxis(int size)
        {
            // It calculates the number of bright pixels vs the number total of pixels
            return  2.0 * size / (size * size);
        }
    }
}
