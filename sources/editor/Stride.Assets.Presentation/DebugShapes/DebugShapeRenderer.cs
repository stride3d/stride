// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.ComponentModel;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Engine;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.Assets.Presentation.DebugShapes
{
    public delegate void RegisterEntityDelegate(Entity entity);

    public enum DebugShapeType
    {
        None        = 0,
        Sphere      = 1,
        Cube        = 2,
        Cone        = 3,
        Cylinder    = 4,
        Torus       = 5,
    }

    public class DebugShapeRenderer
    {
        private Entity debugEntity;

        private Scene scene;

        private GraphicsDevice graphicsDevice;

        private Material material;

        //private RasterizerState rasterizer;

        private DebugShapeType debugShapeType = DebugShapeType.None;

        public DebugShapeRenderer(GraphicsDevice device, Scene scene)
        {
            graphicsDevice = device;
            this.scene = scene;
            material = CreateMaterial(graphicsDevice);
            SetColor(Color.AdjustSaturation(Color.Red, 0.77f), 1);

            //rasterizer = RasterizerState.New(graphicsDevice, new RasterizerStateDescription(CullMode.None) { FillMode = FillMode.Wireframe });

            //material.Parameters.Set(Graphics.Effect.RasterizerStateKey, rasterizer);
        }

        private static Material CreateMaterial(GraphicsDevice device)
        {
            var material = Material.New(device, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor()),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Emissive = new MaterialEmissiveMapFeature(new ComputeColor())
                }
            });

            var color = Color.AdjustSaturation(Color.Red, 0.77f);

            // set the color to the material
            material.Passes[0].Parameters.Set(MaterialKeys.DiffuseValue, new Color4(color).ToColorSpace(device.ColorSpace));
            material.Passes[0].Parameters.Set(MaterialKeys.EmissiveIntensity, 1);
            material.Passes[0].Parameters.Set(MaterialKeys.EmissiveValue, new Color4(color).ToColorSpace(device.ColorSpace));

            return material;
        }

        public void SetColor(Color color, float intensity)
        {
            material.Passes[0].Parameters.Set(MaterialKeys.DiffuseValue, new Color4(color).ToColorSpace(graphicsDevice.ColorSpace));
            material.Passes[0].Parameters.Set(MaterialKeys.EmissiveIntensity, intensity);
            material.Passes[0].Parameters.Set(MaterialKeys.EmissiveValue, new Color4(color).ToColorSpace(graphicsDevice.ColorSpace));

        }

        public void SetTransform(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (debugEntity == null)
                return;

            debugEntity.Transform.Position = position;
            debugEntity.Transform.Rotation = rotation;
            debugEntity.Transform.Scale = scale;
        }

        public void SetDebugShape(DebugShapeType type, RenderGroup renderGroup)
        {
            var needToCreateNew = false;
            needToCreateNew |= (type != debugShapeType && type != DebugShapeType.None);
            debugShapeType = type;

            var needToDeleteOld = (needToCreateNew || (type == DebugShapeType.None));

            // Delete the old implementation if it still persists
            if (needToDeleteOld && debugEntity != null)
            {
                scene.Entities.Remove(debugEntity);
                debugEntity = null;
            }

            if (!needToCreateNew || debugEntity != null)
                return;

            DebugShape debugShape;
            switch (type)
            {
                case DebugShapeType.Cone:
                    debugShape = new DebugShapeCone();
                    break;

                case DebugShapeType.Cube:
                    debugShape = new DebugShapeCube();
                    break;

                case DebugShapeType.Cylinder:
                    debugShape = new DebugShapeCylinder();
                    break;

                case DebugShapeType.Torus:
                    debugShape = new DebugShapeTorus();
                    break;

                default:
                case DebugShapeType.Sphere:
                    debugShape = new DebugShapeSphere();
                    break;
            }

            debugEntity = new Entity
            {
                new ModelComponent
                {
                    Model = new Model
                    {
                        material,
                        new Mesh
                        {
                            Draw = debugShape.CreateDebugPrimitive(graphicsDevice)
                        }
                    },
                    RenderGroup = renderGroup,
                }
            };

            scene.Entities.Add(debugEntity);
            debugEntity.Enable<ModelComponent>();
        }
    }
}
