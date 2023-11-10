using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Engine.Splines.Models;

namespace Stride.Engine.Splines
{
    /// <summary>
    /// A bezier curve builder gets spline node information and builds a bezier curve. The calculated values are then stored back in the SplineNode
    /// </summary>
    public class BezierCurveBuilder
    {
        private int bezierPointCount = 3;
        private int baseBezierPointCount = 100;
        private Vector3 p0;
        private Vector3 p1;
        private Vector3 p2;
        private Vector3 p3;
        private float length;
        private BezierPoint[] baseBezierPoints;
        private BezierPoint[] parameterizedBezierPoints;
        private BoundingBox boundingBox;
            
        /// <summary>
        /// Calculates the bezier curve, parameterizes the curve, updates the bounding boxes 
        /// </summary>
        public void CalculateBezierCurve(SplineNode node)
        {
            bezierPointCount = node.Segments + 1;
            baseBezierPointCount = bezierPointCount > baseBezierPointCount ? baseBezierPointCount + 10 : baseBezierPointCount;

            length = 0;
            baseBezierPoints = new BezierPoint[baseBezierPointCount];
            parameterizedBezierPoints = new BezierPoint[bezierPointCount];

            p0 = node.WorldPosition;
            p1 = node.TangentOutWorldPosition;
            p2 = node.TargetTangentInWorldPosition;
            p3 = node.TargetWorldPosition;

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
                }
            }
            length += baseBezierPoints[baseBezierPointCount - 1].TotalLengthOnCurve;

            ArcLengthParameterization();
            CalculateRotation();
            UpdateBoundingBox();
            
            node.SetCalculatedBezierCurveValues(length, parameterizedBezierPoints, boundingBox);
            baseBezierPoints = null;
            parameterizedBezierPoints = null;
        }


        /// <summary>
        /// Polynomial curve has incorrect arc length parameterization. Use approximated estimated positions
        /// </summary>
        /// <param name="splineNode"></param>
        private void ArcLengthParameterization()
        {
            parameterizedBezierPoints = new BezierPoint[bezierPointCount];

            if (length<= 0)
                return;

            for (var i = 0; i < bezierPointCount; i++)
            {
                var estimatedExpectedDistance = length/ (bezierPointCount - 1) * i;
                parameterizedBezierPoints[i] = GetBezierPointForDistance(estimatedExpectedDistance);

                if (i > 0)
                {

                    parameterizedBezierPoints[i].DistanceToPreviousPoint = parameterizedBezierPoints[i].TotalLengthOnCurve - parameterizedBezierPoints[i - 1].TotalLengthOnCurve;
                    
                    var distance = Vector3.Distance(parameterizedBezierPoints[i].Position, parameterizedBezierPoints[i - 1].Position);
                    parameterizedBezierPoints[i].DistanceToPreviousPoint = distance;
                    parameterizedBezierPoints[i].TotalLengthOnCurve = parameterizedBezierPoints[i - 1].TotalLengthOnCurve + distance;
                }

            }

            parameterizedBezierPoints[bezierPointCount - 1] = baseBezierPoints[baseBezierPointCount - 1];
        }

        private BezierPoint GetBezierPointForDistance(float estimatedExpectedDistance)
        {
            for (var j = 0; j < baseBezierPointCount; j++)
            {
                var curPoint = baseBezierPoints[j];
                if (curPoint.TotalLengthOnCurve >= estimatedExpectedDistance)
                {
                    return curPoint;
                }
            }
            return baseBezierPoints[^1];
        }

        private void CalculateRotation()
        {
            if (length<= 0)
                return;

            for (var i = 0; i < bezierPointCount; i++)
            {
                if (i > 0)
                {
                    // Previous Bezier point looks at current bezier point. Store rotation in previous bezier point
                    var normalDif = Vector3.Normalize(parameterizedBezierPoints[i].Position - parameterizedBezierPoints[i - 1].Position);
                    parameterizedBezierPoints[i - 1].Rotation = Quaternion.LookRotation(normalDif, Vector3.UnitY);
                }
            }

            // Last bezier point in curve gets the same rotation as second last bezier point
            parameterizedBezierPoints[bezierPointCount - 1].Rotation = parameterizedBezierPoints[bezierPointCount - 2].Rotation;
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
            BoundingBox.FromPoints(curvePointsPositions, out var newBoundingBox);
            boundingBox = newBoundingBox;
        }
    }
}
