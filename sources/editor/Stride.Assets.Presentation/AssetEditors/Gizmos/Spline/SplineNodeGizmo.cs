using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Splines.Components;
using Stride.Engine.Splines.Models;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos.Spline
{
    [GizmoComponent(typeof(SplineNodeComponent), false)]
    public class SplineNodeGizmo : EntityGizmo<SplineNodeComponent>
    {
        private Entity mainGizmoEntity;
        private Entity gizmoTangentOut;
        private Entity gizmoTangentIn;

        // Each time we create a spline node, we allocate memory that won't be garbage collected without calling Dispose,
        // so we need a global cache of those materials.
        private static Dictionary<Graphics.GraphicsDevice, Dictionary<Color, Material>> MaterialCache = new Dictionary<Graphics.GraphicsDevice, Dictionary<Color, Material>>();
        private static Dictionary<Graphics.GraphicsDevice, GeometricPrimitive> SphereCache = new Dictionary<Graphics.GraphicsDevice, GeometricPrimitive>(1);
        
        private Material outMaterial;
        private Material inMaterial;
        
        private const float TangentSphereRadius = 0.05f;
        private const int TangentSphereTessellation = 16;
        
        private Vector3 previousInTangent;
        private Vector3 previousOutTangent;
        private Vector3 previousNodePosition;

        public SplineNodeGizmo(EntityComponent component) : base(component)
        {
        }

        protected override Entity Create()
        {
            inMaterial = GetMaterial(GraphicsDevice, Color.LightYellow); 
            outMaterial = GetMaterial(GraphicsDevice, Color.LightSalmon); 

            RenderGroup = RenderGroup.Group4;

            mainGizmoEntity = new Entity();

            var sphereMeshDraw = GetSphere(GraphicsDevice, TangentSphereRadius, TangentSphereTessellation).ToMeshDraw();
            gizmoTangentOut = new Entity("TangentSphereOut") { new ModelComponent { Model = new Model { outMaterial, new Mesh { Draw = sphereMeshDraw } }, RenderGroup = RenderGroup } };
            gizmoTangentIn = new Entity("TangentSphereIn") { new ModelComponent { Model = new Model { inMaterial, new Mesh { Draw = sphereMeshDraw } }, RenderGroup = RenderGroup } };

            mainGizmoEntity.AddChild(gizmoTangentOut);
            mainGizmoEntity.AddChild(gizmoTangentIn);

            Update();

            return mainGizmoEntity;
        }

        public override void Initialize(IServiceRegistry services, Scene editorScene)
        {
            base.Initialize(services, editorScene);
        }

        public override void Update()
        {
            if (ContentEntity == null || GizmoRootEntity == null)
                return;

            ContentEntity.Transform.WorldMatrix.Decompose(out var scale, out Quaternion rotation, out var translation);

            //Only update the tangents mesh if tangent positions have changed
            if (Component.SplineNode.TangentInLocal == previousInTangent && Component.SplineNode.TangentOutLocal == previousOutTangent
                                                                         && translation == previousNodePosition)
                return;

            GizmoRootEntity.Transform.Position = translation;
            GizmoRootEntity.Transform.Scale = new Vector3(1);
            GizmoRootEntity.Transform.UpdateWorldMatrix();

            gizmoTangentOut.Transform.Position = Component.SplineNode.TangentOutLocal;
            gizmoTangentIn.Transform.Position = Component.SplineNode.TangentInLocal;

            ClearChildren(gizmoTangentOut);
            ClearChildren(gizmoTangentIn);

            var meshData = new SplineMeshData();
            var tangentLineOutGoing = new Entity()
            {
                new ModelComponent
                {
                    Model = new Model { outMaterial, new Mesh { Draw = meshData.Build(new Vector3[] { new Vector3(0), -Component.SplineNode.TangentOutLocal }, GraphicsDevice) } },
                    RenderGroup = RenderGroup
                }
            };
            gizmoTangentOut.AddChild(tangentLineOutGoing);
            
            var tangentLineInwards = new Entity()
            {
                new ModelComponent
                {
                    Model = new Model { inMaterial, new Mesh { Draw = meshData.Build(new Vector3[] { new Vector3(0), -Component.SplineNode.TangentInLocal }, GraphicsDevice) } },
                    RenderGroup = RenderGroup
                }
            };
            gizmoTangentIn.AddChild(tangentLineInwards);

            previousInTangent = Component.SplineNode.TangentInLocal;
            previousOutTangent = Component.SplineNode.TangentOutLocal;
            previousNodePosition = translation;
            meshData.Dispose();
        }

        private void ClearChildren(Entity entity)
        {
            var children = entity.GetChildren();
            foreach (var child in children)
            {
                entity.RemoveChild(child);
            }
        }
        
        private static Material GetMaterial(GraphicsDevice device, Color color)
        {
            if (!MaterialCache.TryGetValue(device, out var cache))
            {
                cache = new Dictionary<Color, Material>();
                MaterialCache.Add(device, cache);
            }

            if (cache.TryGetValue(color, out var material))
            {
                return material;
            }

            material = GizmoEmissiveColorMaterial.Create(device, color, color.A == byte.MaxValue ? 0.85f : 0.5f);
            material.Descriptor.Attributes.CullMode = CullMode.None;
            cache.Add(color, material);

            return material;
        }
        
        private static GeometricPrimitive GetSphere(GraphicsDevice device, float centerSphereRadius, int tessellation)
        {
            if (SphereCache.TryGetValue(device, out var sphere))
            {
                return sphere;
            }

            sphere = GeometricPrimitive.Sphere.New(device, centerSphereRadius, tessellation);
            SphereCache.Add(device, sphere);

            return sphere;
        }
    }
}
