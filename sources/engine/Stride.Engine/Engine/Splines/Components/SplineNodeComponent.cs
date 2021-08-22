using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using static Stride.Engine.Splines.BezierCurve;

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
        #region Segments
        private int segments = 2;
        [Display(1, "Segments")]
        public int Segments
        {
            get { return segments; }
            set
            {
                if (value < 2)
                {
                    segments = 2;
                }
                else
                {
                    segments = value;
                }

                MakeDirty();
            }
        }
        #endregion

        #region Out
        private Vector3 tangentOut { get; set; }
        [Display(2, "Tangent outgoing")]
        public Vector3 TangentOut
        {
            get { return tangentOut; }
            set
            {
                tangentOut = value;
                MakeDirty();
            }
        }
        #endregion

        #region In
        private Vector3 tangentIn { get; set; }
        [Display(3, "Tangent outgoing")]
        public Vector3 TangentIn
        {
            get { return tangentIn; }
            set
            {
                tangentIn = value;
                MakeDirty();
            }
        }
        #endregion

        private BezierCurve bezierCurve;
        private Vector3 previousPosition;

        internal void Update(TransformComponent transformComponent)
        {
            CheckDirtyness();

            previousPosition = Entity.Transform.Position;
        }

        private void CheckDirtyness()
        {
            if (previousPosition.X != Entity.Transform.Position.X || previousPosition.Y != Entity.Transform.Position.Y || previousPosition.Z != Entity.Transform.Position.Z)
            {
                MakeDirty();
            }
        }

        public delegate void BezierCurveDirtyEventHandler();
        public event BezierCurveDirtyEventHandler OnDirty;


        public void MakeDirty()
        {
            OnDirty?.Invoke();
        }

        public void UpdateBezierCurve(SplineNodeComponent nextNode)
        {
            if (nextNode != null)
            {
                Vector3 scale;
                Quaternion rotation;
                Vector3 entityWorldPos;
                Vector3 nextWorldPos;

                Entity.Transform.WorldMatrix.Decompose(out scale, out rotation, out entityWorldPos);
                nextNode.Entity.Transform.WorldMatrix.Decompose(out scale, out rotation, out nextWorldPos);
                Vector3 TangentOutWorld = entityWorldPos + TangentOut;
                Vector3 TangentInWorld = nextWorldPos + nextNode.TangentIn;

                bezierCurve = new BezierCurve(Segments, entityWorldPos, TangentOutWorld, nextWorldPos, TangentInWorld);
            }
        }

        public BezierCurve GetBezierCurve()
        {
            return bezierCurve;
        }

        public BezierPoint[] GetBezierCurvePoints()
        {
            return bezierCurve.GetBezierPoints();
        }
    }
}
