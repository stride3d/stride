// Copyright (c) Stride contributors (https://Stride.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Splines;
using Stride.Engine.Splines.Components;
using Stride.Engine.Splines.Models.Mesh;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// A gizmo that displays connected spline nodes
    /// </summary>
    [GizmoComponent(typeof(SplineComponent), false)]
    public class SplineGizmo : BillboardingGizmo<SplineComponent>
    {
        private Entity mainGizmoEntity;

        public SplineGizmo(EntityComponent component) : base(component, "Spline gizmo", GizmoResources.SplineGizmo)
        {
        }

        protected override Entity Create()
        {
            mainGizmoEntity = new Entity();

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
            GizmoRootEntity.Transform.Position = translation;
            GizmoRootEntity.Transform.Scale = 1 * scale;
            GizmoRootEntity.Transform.UpdateWorldMatrix();

            if (Component.Spline.Dirty)
            {
                var children = mainGizmoEntity.GetChildren();
                foreach (var child in children)
                {
                    mainGizmoEntity?.RemoveChild(child);
                }
                var splineMaterial = GizmoUniformColorMaterial.Create(GraphicsDevice, Color.Green);
                var splineDebugEntity = Component.Spline.DebugInfo.UpdateSplineDebugInfo(Component.Spline, GraphicsDevice, splineMaterial, mainGizmoEntity.Transform.Position);
                mainGizmoEntity.AddChild(splineDebugEntity);
            }
        }
    }
}
