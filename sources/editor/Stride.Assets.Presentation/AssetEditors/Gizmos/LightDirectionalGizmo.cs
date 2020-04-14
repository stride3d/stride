// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Extensions;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// The gizmo for the ambient light component
    /// </summary>
    public class LightDirectionalGizmo : LightGizmo
    {
        protected const int GizmoTessellation = 64;
        private const float BodyLength = 0.4f;
        private const float ConeHeight = BodyLength / 5;
        private const float BodyRadius = ConeHeight / 6;
        private const float ConeRadius = ConeHeight / 3;

        private Entity lightRay;
        private Material rayMaterial;

        public LightDirectionalGizmo(EntityComponent component)
            : base(component, "Directional", GizmoResources.SunLightGizmo)
        {
        }

        protected override Entity Create()
        {
            var root = base.Create();
            
            lightRay = new Entity($"Light ray for light gizmo {root.Id}");
            rayMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, (Color)new Color4(GetLightColor(GraphicsDevice), 1f));

            // build the ray mesh
            var coneMesh = GeometricPrimitive.Cone.New(GraphicsDevice, ConeRadius, ConeHeight, GizmoTessellation).ToMeshDraw();
            var bodyMesh = GeometricPrimitive.Cylinder.New(GraphicsDevice, BodyLength, BodyRadius, GizmoTessellation).ToMeshDraw();
            
            var coneEntity = new Entity($"Light ray for light gizmo {root.Id}") { new ModelComponent { Model = new Model { rayMaterial, new Mesh { Draw = coneMesh } }, RenderGroup = RenderGroup } };
            coneEntity.Transform.Rotation = Quaternion.RotationX(-MathUtil.PiOverTwo);
            coneEntity.Transform.Position.Z = -BodyLength - ConeHeight * 0.5f;
            lightRay.AddChild(coneEntity);

            var bodyEntity = new Entity($"Light ray body for light gizmo {root.Id}") { new ModelComponent { Model = new Model { rayMaterial, new Mesh { Draw = bodyMesh } }, RenderGroup = RenderGroup } };
            bodyEntity.Transform.Position.Z = -BodyLength / 2;
            bodyEntity.Transform.Rotation = Quaternion.RotationX(-MathUtil.PiOverTwo);
            lightRay.AddChild(bodyEntity);

            return root;
        }

        public override void Update()
        {
            base.Update();

            // update the color of the ray
            GizmoUniformColorMaterial.UpdateColor(GraphicsDevice, rayMaterial, (Color)new Color4(GetLightColor(GraphicsDevice), 1f));
        }

        public override bool IsSelected
        {
            set
            {
                bool hasChanged = IsSelected != value;
                base.IsSelected = value;

                if (hasChanged)
                {
                    if (IsSelected)
                        GizmoRootEntity.AddChild(lightRay);
                    else
                        GizmoRootEntity.RemoveChild(lightRay);
                }
            }
        }
    }
}
