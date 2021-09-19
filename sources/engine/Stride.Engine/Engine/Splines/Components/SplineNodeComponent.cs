using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Graphics;
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
        private int _segments = 2;
        [Display(1, "Segments")]
        public int Segments
        {
            get { return _segments; }
            set
            {
                if (value < 2)
                {
                    _segments = 2;
                }
                else
                {
                    _segments = value;
                }

                MakeDirty();
            }
        }
        #endregion

        #region Out
        private Vector3 _tangentOut { get; set; }
        [Display(2, "Tangent outgoing")]
        public Vector3 TangentOut
        {
            get { return _tangentOut; }
            set
            {
                _tangentOut = value;
                MakeDirty();
            }
        }
        #endregion

        #region In
        private Vector3 _tangentIn { get; set; }
        [Display(3, "Tangent incoming")]
        public Vector3 TangentIn

        {
            get { return _tangentIn; }
            set
            {
                _tangentIn = value;
                MakeDirty();
            }
        }
        #endregion

        private BezierCurve _bezierCurve;
        private Vector3 _previousPosition;
        public BoundingBox BoundingBox;


        internal void Update(TransformComponent transformComponent)
        {
            CheckDirtyness();

            _previousPosition = Entity.Transform.Position;
        }

        private void CheckDirtyness()
        {
            if (_previousPosition.X != Entity.Transform.Position.X || _previousPosition.Y != Entity.Transform.Position.Y || _previousPosition.Z != Entity.Transform.Position.Z)
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
            if (nextNode != null && nextNode.Entity != null)
            {
                Vector3 scale;
                Quaternion rotation;
                Vector3 entityWorldPos;
                Vector3 nextWorldPos;

                Entity.Transform.WorldMatrix.Decompose(out scale, out rotation, out entityWorldPos);
                nextNode.Entity.Transform.WorldMatrix.Decompose(out scale, out rotation, out nextWorldPos);
                Vector3 TangentOutWorld = entityWorldPos + TangentOut;
                Vector3 TangentInWorld = nextWorldPos + nextNode.TangentIn;

                _bezierCurve = new BezierCurve(Segments, entityWorldPos, TangentOutWorld, nextWorldPos, TangentInWorld);

                var curvePoints = _bezierCurve.GetBezierPoints();
                var curvePointsPositions = new Vector3[curvePoints.Length];
                for (int j = 0; j < curvePoints.Length; j++)
                {
                    if (curvePoints[j] == null)
                        break;
                    curvePointsPositions[j] = curvePoints[j].Position;
                }
                BoundingBox.FromPoints(curvePointsPositions, out BoundingBox);                
            }
        }

        public BezierCurve GetBezierCurve()
        {
            return _bezierCurve;
        }

        public BezierPoint[] GetBezierCurvePoints()
        {
            return _bezierCurve.GetBezierPoints();
        }
    }
}
