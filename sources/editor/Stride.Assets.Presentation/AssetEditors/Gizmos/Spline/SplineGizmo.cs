using System.Collections.Generic;
using Stride.Engine;
using Stride.Engine.Splines.Components;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos.Spline
{
    [GizmoComponent(typeof(SplineComponent), true)]
    public class SplineGizmo : BillboardingGizmo<SplineComponent>
    {
        public SplineGizmo(EntityComponent component)
            : base(component, "Spline", GizmoResources.SplineGizmo)
        {
        }
    }
}
