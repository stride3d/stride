using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Graphics;
using static Stride.Engine.Splines.SplineNode;

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
    [DefaultEntityComponentProcessor(typeof(SplineNodeTransformProcessor), ExecutionMode = ExecutionMode.All)]
    [Display("Spline node", Expand = ExpandRule.Once)]
    [ComponentCategory("Splines")]
    public sealed class SplineNodeComponent : EntityComponent
    {
        public SplineNode SplineNode = new();
        private Vector3 _previousPosition;

        public SplineNodeComponent()
        {
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
