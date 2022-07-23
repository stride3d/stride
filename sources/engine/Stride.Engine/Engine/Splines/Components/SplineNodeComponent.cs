//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine.Design;
using Stride.Engine.Splines.Models;
using Stride.Engine.Splines.Processors;

namespace Stride.Engine.Splines.Components
{
    /// <summary>
    /// Component representing a Spline node.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Associate this component to an entity to maintain bezier curves that together form a spline.
    /// </para>
    /// </remarks>

    [DataContract]
    [DefaultEntityComponentProcessor(typeof(SplineNodeTransformProcessor))]
    [Display("Spline node", Expand = ExpandRule.Once)]
    [ComponentCategory("Splines")]
    public sealed class SplineNodeComponent : EntityComponent
    {
        private SplineNode splineNode;
        private Vector3 _previousPosition;

        /// <summary>
        /// Reference to the splinenode class
        /// </summary>
        public SplineNode SplineNode
        {
            get
            {
                splineNode ??= new SplineNode();
                return splineNode;
            }
            set
            {
                splineNode = value;
            }
        }

        public SplineNodeComponent()
        {
        }

        public SplineNodeComponent(int segments)
        {
            SplineNode.Segments = segments;
        }

        public SplineNodeComponent(int segments, Vector3 localTangentOut, Vector3 localTangentIn)
        {
            SplineNode.Segments = segments;
            SplineNode.TangentInLocal = localTangentIn;
            SplineNode.TangentOutLocal = localTangentOut;
        }

        internal void Update(TransformComponent transformComponent)
        {
            CheckDirtyness();

            _previousPosition = Entity.Transform.Position;
        }

        private void CheckDirtyness()
        {
            if (_previousPosition.X != Entity.Transform.Position.X || _previousPosition.Y != Entity.Transform.Position.Y || _previousPosition.Z != Entity.Transform.Position.Z)
            {
                SplineNode.InvokeOnDirty();
            }
        }

        public BezierPoint[] GetBezierCurvePoints()
        {
            return SplineNode.GetBezierPoints();
        }
    }
}
