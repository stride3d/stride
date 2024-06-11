using System.Collections.Generic;
using Stride.Engine;
using Stride.Engine.Splines.Components;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos.Spline
{
    [GizmoComponent(typeof(SplineNodeComponent), true)]
    public class SplineNodeGizmo : BillboardingGizmo<SplineNodeComponent>
    {
        public SplineNodeGizmo(EntityComponent component)
            : base(component, "SplineNode", GizmoResources.SplineNodeGizmo)
        {
        }
    }
}
