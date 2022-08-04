//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Engine.Splines.Models
{
    [DataContract]
    public class SplineNode
    {
        private int bezierPointCount = 3;
        private int baseBezierPointCount = 100;
        private Vector3 p0;
        private Vector3 p1;
        private Vector3 p2;
        private Vector3 p3;

        [DataMemberIgnore]
        public Vector3 WorldPosition { get; set; }

        [DataMemberIgnore]
        public Vector3 TangentOutWorldPosition { get; set; }

        [DataMemberIgnore]
        public Vector3 TangentInWorldPosition { get; set; }

        [DataMemberIgnore]

        public Vector3 TargetTangentInWorldPosition { get; set; }

        [DataMemberIgnore]
        public Vector3 TargetWorldPosition { get; set; }

        #region Segments
        private int segments = 2;
        /// <summary>
        /// A minimum of 2
        /// </summary>
        /// <userdoc>The amount of segments the curve exists out of</userdoc>
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

                bezierPointCount = segments + 1;
                baseBezierPointCount = bezierPointCount > baseBezierPointCount ? baseBezierPointCount + 10 : baseBezierPointCount;

                InvokeOnDirty();
            }
        }
        #endregion

        #region Out
        private Vector3 tangentOut { get; set; }
        [Display(2, "Tangent out")]
        public Vector3 TangentOutLocal
        {
            get { return tangentOut; }
            set
            {
                tangentOut = value;
                InvokeOnDirty();
            }
        }
        #endregion

        #region In
        private Vector3 tangentIn { get; set; }
        [Display(3, "Tangent in")]
        public Vector3 TangentInLocal

        {
            get { return tangentIn; }
            set
            {
                tangentIn = value;
                InvokeOnDirty();
            }
        }
        #endregion

        private BezierPoint[] baseBezierPoints;
        private BezierPoint[] parameterizedBezierPoints;

        public BoundingBox BoundingBox { get; private set; }

        public float Length { get; private set; } = 0;

        public delegate void BezierCurveDirtyEventHandler();
        public event BezierCurveDirtyEventHandler OnSplineNodeDirty;

        public SplineNode()
        {
        }

        public void InvokeOnDirty()
        {
            OnSplineNodeDirty?.Invoke();
        }

        public BezierPoint[] GetBezierPoints()
        {
            return parameterizedBezierPoints;
        }

        /// <summary>
        /// Calculates the bezier curve, parameterizes the curve, updates the bounding boxes 
        /// </summary>
        public void CalculateBezierCurve()
        {
            Length = 0;
            baseBezierPoints = new BezierPoint[baseBezierPointCount];
            parameterizedBezierPoints = new BezierPoint[bezierPointCount];

            p0 = WorldPosition;
            p1 = TangentOutWorldPosition;
            p2 = TargetTangentInWorldPosition;
            p3 = TargetWorldPosition;

            // 2 methods of arc length parameterization: Use a larger amount of pre calculated points or devide segments per distance
            // We create a base spline that contains a large amount of segments.
            // Later on we can distill this as a way of determining arc length parameterization
            var t = 1.0f / (baseBezierPointCount - 1);
            for (var i = 0; i < baseBezierPointCount; i++)
            {
                baseBezierPoints[i] = new BezierPoint { Position = CalculateBezierPoint(t * i) };

                if (i == 0)
                {
                    baseBezierPoints[i].DistanceToPreviousPoint = 0;
                    baseBezierPoints[i].TotalLengthOnCurve = 0;
                }
                else
                {
                    var distance = Vector3.Distance(baseBezierPoints[i].Position, baseBezierPoints[i - 1].Position);
                    baseBezierPoints[i].DistanceToPreviousPoint = distance;
                    baseBezierPoints[i].TotalLengthOnCurve = baseBezierPoints[i - 1].TotalLengthOnCurve + distance;


                    //Point previous to current bezierPoint and store rotation
                    var normalDif = Vector3.Normalize(baseBezierPoints[i].Position - baseBezierPoints[i-1].Position); 
                    baseBezierPoints[i-1].Rotation = Quaternion.LookRotation(normalDif, Vector3.UnitY);
                }
            }

            Length += baseBezierPoints[baseBezierPointCount - 1].TotalLengthOnCurve;

            ArcLengthParameterization();
            UpdateBoundingBox();

            baseBezierPoints = null;
        }

        /// <summary>
        /// Returns the World position by a given percentage
        /// </summary>
        /// <param name="percentage"></param>
        /// <returns></returns>
        public Vector3 GetPositionOnCurve(float percentage)
        {
            var distance = Length / 100 * Math.Clamp(percentage, 0, 100);
            return GetBezierPointForDistance(distance).Position;
        }

        /// <summary>
        /// Retrieves information about the closest point on the spline in relation to the given world position
        /// </summary>
        /// <param name="originPosition">A Vector3 world position </param>
        /// <returns></returns>
        public SplinePositionInfo GetClosestPointOnCurve(Vector3 originPosition)
        {
            SplinePositionInfo info = null;
            for (var i = 0; i < bezierPointCount; i++)
            {
                var currentBezierPoint = GetBezierPoints()[i];
                var curSplinePointDistance = Vector3.Distance(currentBezierPoint.Position, originPosition);

                if (info == null || curSplinePointDistance < info.DistanceToOrigin)
                {
                    info ??= new SplinePositionInfo();
                    info.ClosestBezierPoint = currentBezierPoint;
                    info.Position = currentBezierPoint.Position;
                    info.ClosestBezierPointIndex = i;
                    info.DistanceToOrigin = curSplinePointDistance;
                    info.LengthOnCurve = currentBezierPoint.TotalLengthOnCurve;
                }
            }
            return info;
        }

        /// <summary>
        /// Polynominal curve has incorrect arc length parameterization. Use approximated estimated positions
        /// </summary>
        private void ArcLengthParameterization()
        {
            parameterizedBezierPoints = new BezierPoint[bezierPointCount];

            if (Length <= 0)
                return;

            for (var i = 0; i < bezierPointCount; i++)
            {
                var estimatedExptedDistance = Length / (bezierPointCount - 1) * i;
                parameterizedBezierPoints[i] = GetBezierPointForDistance(estimatedExptedDistance);
            }

            parameterizedBezierPoints[bezierPointCount - 1] = baseBezierPoints[baseBezierPointCount - 1];
        }

        private BezierPoint GetBezierPointForDistance(float distance)
        {
            for (var j = 0; j < baseBezierPointCount; j++)
            {
                var curPoint = baseBezierPoints[j];
                if (curPoint.TotalLengthOnCurve >= distance)
                {
                    return curPoint;
                }
            }
            return baseBezierPoints[baseBezierPoints.Length - 1];
        }

        private Vector3 CalculateBezierPoint(float t)
        {
            var tPower3 = t * t * t;
            var tPower2 = t * t;
            var oneMinusT = 1 - t;
            var oneMinusTPower3 = oneMinusT * oneMinusT * oneMinusT;
            var oneMinusTPower2 = oneMinusT * oneMinusT;
            var x = oneMinusTPower3 * p0.X + 3 * oneMinusTPower2 * t * p1.X + 3 * oneMinusT * tPower2 * p2.X + tPower3 * p3.X;
            var y = oneMinusTPower3 * p0.Y + 3 * oneMinusTPower2 * t * p1.Y + 3 * oneMinusT * tPower2 * p2.Y + tPower3 * p3.Y;
            var z = oneMinusTPower3 * p0.Z + 3 * oneMinusTPower2 * t * p1.Z + 3 * oneMinusT * tPower2 * p2.Z + tPower3 * p3.Z;
            return new Vector3(x, y, z);
        }

        private void UpdateBoundingBox()
        {
            var curvePointsPositions = new Vector3[parameterizedBezierPoints.Length];
            for (var j = 0; j < parameterizedBezierPoints.Length; j++)
            {
                if (parameterizedBezierPoints[j] == null)
                    break;
                curvePointsPositions[j] = parameterizedBezierPoints[j].Position;
            }
            BoundingBox.FromPoints(curvePointsPositions, out var NewBoundingBox);
            BoundingBox = NewBoundingBox;
        }
    }
}
