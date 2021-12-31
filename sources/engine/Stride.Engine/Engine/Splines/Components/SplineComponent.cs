using System.Collections.Generic;
using Stride.Core;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Core.Mathematics;
using Stride.Rendering;

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
        private List<SplineNodeComponent> splineNodesComponents = new List<SplineNodeComponent>();
        private Vector3 previousPosition;

        public Spline Spline = new Spline();

        private SplineRenderer splineRenderer;


        [Display(100, "Nodes")]
        public List<SplineNodeComponent> SplineNodesComponents
        {
            get
            {
                splineNodesComponents ??= new List<SplineNodeComponent>();
                return splineNodesComponents;
            }
            set
            {
                splineNodesComponents = value;

                Spline.Dirty = true;
            }
        }

        [Display(80, "Spline renderer")]
        public SplineRenderer SplineRenderer
        {
            get
            {
                if (splineRenderer == null)
                {
                    splineRenderer ??= new SplineRenderer();
                    
                }
                return splineRenderer;
            }
            set
            {
                splineRenderer = value;
                Spline.Dirty = true;
            }
        }

        public SplineComponent()
        {
            SplineNodesComponents ??= new List<SplineNodeComponent>();
            Spline.Dirty = true;
        }

        internal void Update(TransformComponent transformComponent)
        {
            if (previousPosition.X != Entity.Transform.Position.X || previousPosition.Y != Entity.Transform.Position.Y || previousPosition.Z != Entity.Transform.Position.Z)
            {
                Spline.Dirty = true;
                previousPosition = Entity.Transform.Position;
            }
        }

        public SplinePositionInfo GetPositionOnSpline(float percentage)
        {
            return Spline.GetPositionOnSpline(percentage);
        }
    }
}
