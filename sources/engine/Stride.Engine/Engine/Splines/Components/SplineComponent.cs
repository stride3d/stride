//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core;
using Stride.Engine.Design;
using Stride.Engine.Splines.Processors;
using Stride.Core.Mathematics;
using Stride.Engine.Splines.Models;

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
        private List<SplineNodeComponent> splineNodesComponents;
        private Vector3 previousPosition;
        private SplineRenderer splineRenderer;
        private Spline spline;

        /// <summary>
        /// Reference to the Spline
        /// </summary>
        [DataMemberIgnore]
        public Spline Spline
        {
            get
            {
                spline ??= new Spline();
                return spline;
            }
            set
            {
                spline = value;
                Spline.EnqueueSplineUpdate();
            }
        }

        /// <summary>
        /// The last spline node reconnects to the first spline node. This still requires a minimum of 2 spline nodes.
        /// </summary>
        [Display(10, "Loop")]
        public bool Loop
        {
            get
            {
                return Spline.Loop;
            }
            set
            {
                Spline.Loop = value;
            }
        }

        /// <summary>
        /// Contains a list of spline node components
        /// </summary>
        [Display(20, "Nodes")]
        public List<SplineNodeComponent> Nodes
        {
            get
            {
                splineNodesComponents ??= new List<SplineNodeComponent>();
                return splineNodesComponents;
            }
            set
            {
                splineNodesComponents = value;
                Spline.EnqueueSplineUpdate();
            }
        }

        /// <summary>
        /// A spline renderer is used to visualise the spline
        /// </summary>
        [Display(50, "Spline renderer")]
        public SplineRenderer SplineRenderer
        {
            get
            {
                splineRenderer ??= new SplineRenderer();
                splineRenderer.OnSplineRendererSettingsUpdated += SplineRenderer_OnSplineRendererSettingsUpdated;

                return splineRenderer;
            }
            set
            {
                splineRenderer = value;
            }
        }

        public SplineComponent()
        {

        }

        public void UpdateSpline()
        {
            Spline.EnqueueSplineUpdate();
        }

        internal void Update(TransformComponent transformComponent)
        {
            if (previousPosition.X != Entity.Transform.Position.X || previousPosition.Y != Entity.Transform.Position.Y || previousPosition.Z != Entity.Transform.Position.Z)
            {
                Spline.EnqueueSplineUpdate();
                previousPosition = Entity.Transform.Position;
            }
        }

        private void SplineRenderer_OnSplineRendererSettingsUpdated()
        {
            Spline.EnqueueSplineUpdate();
        }

        public SplinePositionInfo GetPositionOnSpline(float percentage)
        {
            return Spline.GetPositionOnSpline(percentage);
        }

        public SplinePositionInfo GetClosestPointOnSpline(Vector3 originPosition)
        {
            return Spline.GetClosestPointOnSpline(originPosition);
        }
    }
}
