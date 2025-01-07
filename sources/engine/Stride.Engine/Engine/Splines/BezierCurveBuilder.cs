using System;
using System.Collections.Generic;
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

        // Use a binary search since the array is sorted by TotalLengthOnCurve.
        private BezierPoint GetBezierPointForDistance(float estimatedExpectedDistance)
        {
            int left = 0, right = baseBezierPointCount - 1;

            while (left <= right)
            {
                int mid = (left + right) / 2;
                if (baseBezierPoints[mid].TotalLengthOnCurve < estimatedExpectedDistance)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            // Ensure 'left' is within bounds
            if (left >= baseBezierPointCount)
            {
                return baseBezierPoints[baseBezierPointCount - 1]; // Return the last point
            }

            return baseBezierPoints[left];
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
            float t2 = t * t;
            float t3 = t2 * t;
            float oneMinusT = 1 - t;
            float oneMinusT2 = oneMinusT * oneMinusT;
            float oneMinusT3 = oneMinusT2 * oneMinusT;

            return (oneMinusT3 * p0) + (3 * oneMinusT2 * t * p1) + (3 * oneMinusT * t2 * p2) + (t3 * p3);
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
