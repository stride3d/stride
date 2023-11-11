using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine.Splines.Models;

namespace Stride.Engine.Splines
{
    /// <summary>
    /// A spline builder contains the logic to build a Spline and its accompanied Spline nodes
    /// </summary>
    public class SplineBuilder
    {
        private BezierCurveBuilder bezierCurveBuilder;
        /// <summary>
        /// Calculates the spline curves using its splines nodes
        /// Update the bounding box of the <see cref="Spline"/>
        /// Updates the total length of the <see cref="Spline"/>
        /// Triggers the <see cref="Spline"/> SplineUpdated event  
        /// </summary>
        public void CalculateSpline(Spline spline)
        {
            bezierCurveBuilder = new BezierCurveBuilder();
            
            var totalNodesCount = spline.SplineNodes.Count;
            if (spline.SplineNodes.Count > 1)
            {
                for (var i = 0; i < totalNodesCount; i++)
                {
                    var currentSplineNode = spline.SplineNodes[i];
                    if (spline.SplineNodes[i] == null)
                        break;

                    if (i < totalNodesCount - 1)
                    {
                        var nextSplineNode = spline.SplineNodes[i + 1];
                        if (nextSplineNode == null)
                            break;

                        currentSplineNode.TargetWorldPosition = nextSplineNode.WorldPosition;
                        currentSplineNode.TargetTangentInWorldPosition = nextSplineNode.TangentInWorldPosition;

                        bezierCurveBuilder.CalculateBezierCurve(spline.SplineNodes[i]);
    

                        // Update the rotation of the last bezier point from the previous curve, to the rotation of the first bezier point in the current curve
                        if (i > 0)
                            spline.SplineNodes[i - 1].UpdateLastBezierPointRotation(spline.SplineNodes[i].GetBezierPoints()[0].Rotation);

                    }
                    else if (i == totalNodesCount - 1 && spline.Loop)
                    {
                        var firstSplineNode = spline.SplineNodes[0];
                        currentSplineNode.TargetWorldPosition = firstSplineNode.WorldPosition;
                        currentSplineNode.TargetTangentInWorldPosition = firstSplineNode.TangentInWorldPosition;
                        
                        bezierCurveBuilder.CalculateBezierCurve(spline.SplineNodes[i]);
                        
                        // Update the rotation of the last bezier point from the previous curve, to the rotation of the first bezierpoint in the current curve
                        spline.SplineNodes[i - 1].UpdateLastBezierPointRotation(spline.SplineNodes[i].GetBezierPoints()[0].Rotation);

                        // Update the rotation of the last bezier point in the current curve, to the rotation of the first bezier point of the first curve
                        spline.SplineNodes[i].UpdateLastBezierPointRotation(firstSplineNode.GetBezierPoints()[0].Rotation);
                    }
                }
            }

            spline.TotalSplineDistance = GetTotalSplineLength(spline);
            UpdateBoundingBox(spline);

            //Trigger the Spline Updated event
            spline.SplineUpdated();
        }

        /// <summary>
        /// Retrieves the total distance of the spline
        /// </summary>
        /// <returns>The total distance of the spline</returns>
        public float GetTotalSplineLength(Spline spline)
        {
            float distance = 0;
            for (var i = 0; i < spline.SplineNodes.Count; i++)
            {
                var curve = spline.SplineNodes[i];
                if (curve != null)
                {
                    if (spline.Loop || !spline.Loop && i < spline.SplineNodes.Count - 1)
                    {
                        distance += curve.Length;
                    }
                }
            }

            return distance;
        }

        /// <summary>
        /// Retrieve information of the spline position at give percentage
        /// </summary>
        /// <param name="percentage"></param>
        /// <returns>Various details on the specific part of the spline</returns>
        public SplinePositionInfo GetPositionOnSpline(Spline spline, float percentage)
        {
            var splinePositionInfo = new SplinePositionInfo();
            var totalSplineDistance = GetTotalSplineLength(spline);
            if (totalSplineDistance <= 0)
                return splinePositionInfo;

            var requiredDistance = totalSplineDistance * (percentage / 100);
            var nextNodeDistance = 0.0f;
            var prevNodeDistance = 0.0f;

            for (var i = 0; i <  spline.SplineNodes.Count; i++)
            {
                var currentSplineNode =  spline.SplineNodes[i];
                splinePositionInfo.SplineNodeA = currentSplineNode;

                nextNodeDistance += currentSplineNode.Length;

                if (requiredDistance < nextNodeDistance)
                {
                    var targetIndex = i ==  spline.SplineNodes.Count - 1 ? 0 : i;
                    splinePositionInfo.SplineNodeB =  spline.SplineNodes[targetIndex];

                    // Inverse lerp(betweenValue - minHeight) / (maxHeight - minHeight);
                    var percentageInCurve = (requiredDistance - prevNodeDistance) / (nextNodeDistance - prevNodeDistance) * 100;

                    splinePositionInfo.Position = currentSplineNode.GetPositionOnBezierCurve(percentageInCurve);
                    return splinePositionInfo;
                }

                prevNodeDistance = nextNodeDistance;
            }

            splinePositionInfo.Position =  spline.SplineNodes[spline.SplineNodes.Count - 2].TargetWorldPosition;

            return splinePositionInfo;
        }
        
        private void UpdateBoundingBox(Spline spline)
        {
            var allCurvePointsPositions = new List<Vector3>();
            for (var i = 0; i <  spline.SplineNodes.Count; i++)
            {
                if (!spline.Loop && i ==  spline.SplineNodes.Count - 1)
                {
                    break;
                }

                var positions =  spline.SplineNodes[i].GetBezierPoints();
                if (positions != null)
                {
                    for (var j = 0; j < positions.Length; j++)
                    {
                        if (positions[j] != null)
                            allCurvePointsPositions.Add(positions[j].Position);
                    }
                }
            }
            BoundingBox.FromPoints(allCurvePointsPositions.ToArray(), out var NewBoundingBox);
            spline.BoundingBox = NewBoundingBox;
        }
    }
}
