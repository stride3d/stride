using System.Collections.Generic;
using Stride.Core;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Core.Mathematics;
using System.Diagnostics;
using Stride.Engine;
using static Stride.Engine.Splines.Spline;

namespace Stride.Engine.Splines.Components
{
    /// <summary>
    /// Component representing an Spline.
    /// </summary>
    [DataContract("SplineComponent")]
    [Display("Spline", Expand = ExpandRule.Once)]
    [DefaultEntityComponentProcessor(typeof(SplineTransformProcessor))]
    [ComponentCategory("Splines")]
    public sealed class SplineComponent : EntityComponent
    {
        private List<SplineNodeComponent> _splineNodesComponents = new List<SplineNodeComponent>();
        private Vector3 previousPosition;

        public Spline Spline = new Spline();


        [Display(100, "Nodes")]
        public List<SplineNodeComponent> SplineNodesComponents
        {
            get
            {
                _splineNodesComponents ??= new List<SplineNodeComponent>();
                return _splineNodesComponents;
            }
            set
            {
                _splineNodesComponents = value;

                Spline.Dirty = true;
            }
        }

        public SplineComponent()
        {
            SplineNodesComponents ??= new List<SplineNodeComponent>();
        }

        internal void Initialize()
        {
            Spline.Dirty = true;
        }

        internal void Update(TransformComponent transformComponent)
        {
            if (previousPosition.X != Entity.Transform.Position.X || previousPosition.Y != Entity.Transform.Position.Y || previousPosition.Z != Entity.Transform.Position.Z)
            {
                Spline.Dirty = true;
                previousPosition = Entity.Transform.Position;
            }

            if (Spline.Dirty)
            {
              UpdateSpline();
            }
        }

        private void UpdateSpline()
        {
            var updatedSplineNodes = new List<SplineNode>();
            var totalNodesCount = SplineNodesComponents.Count;

            if (totalNodesCount > 1)
            {
                for (int i = 0; i < totalNodesCount; i++)
                {
                    var currentSplineNodeComponent = SplineNodesComponents[i];

                    if (currentSplineNodeComponent == null)
                        break;

                    currentSplineNodeComponent.Entity.Transform.WorldMatrix.Decompose(out var scale, out Quaternion rotation, out var startTangentOutWorldPosition);
                    currentSplineNodeComponent.SplineNode.WorldPosition = startTangentOutWorldPosition;
                    currentSplineNodeComponent.SplineNode.TangentOutWorldPosition = startTangentOutWorldPosition + currentSplineNodeComponent.SplineNode.TangentOutLocal;
                    currentSplineNodeComponent.SplineNode.TangentInWorldPosition = startTangentOutWorldPosition + currentSplineNodeComponent.SplineNode.TangentInLocal;
                    updatedSplineNodes.Add(currentSplineNodeComponent.SplineNode);
                }
            }

            Spline.splineNodes = updatedSplineNodes;
            Spline.UpdateSpline();
        }

        //public SplinePositionInfo GetPositionOnSpline(float percentage)
        //{
        //    return Spline.GetPositionOnSpline(percentage);
        //}
    }
}
