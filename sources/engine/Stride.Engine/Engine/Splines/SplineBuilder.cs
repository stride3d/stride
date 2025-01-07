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
            int limit = spline.Loop ? spline.SplineNodes.Count : spline.SplineNodes.Count - 1;
            for (int i = 0; i < limit; i++)
            {
                var node = spline.SplineNodes[i];
                if (node != null)
                    distance += node.Length;
            }

            return distance;
        }

        private void UpdateBoundingBox(Spline spline)
        {
            var allCurvePointsPositions = new List<Vector3>();

            foreach (var node in spline.SplineNodes)
            {
                var positions = node?.GetBezierPoints();
                if (positions != null)
                {
                    foreach (var point in positions)
                    {
                        if (point != null)
                            allCurvePointsPositions.Add(point.Position);
                    }
                }
            }
            
            BoundingBox.FromPoints(allCurvePointsPositions.ToArray(), out var newBoundingBox);
            spline.BoundingBox = newBoundingBox;
        }
    }
}
