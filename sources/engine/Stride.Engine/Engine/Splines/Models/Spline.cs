//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Engine.Splines.Models
{
    [DataContract]
    public class Spline
    {
        private bool loop;
        private List<SplineNode> splineNodes;

        /// <summary>
        /// Event triggered when the spline has become dirty
        /// </summary>
        public delegate void DirtySplineHandler();
        public event DirtySplineHandler OnSplineDirty;

        /// <summary>
        /// Event triggered when the spline has been updated
        /// This happens when the entity is translated, rotated, a SplineNode is updated or when the OnSplineEnqueueUpdate event is called
        /// </summary>
        public delegate void SplineUpdatedHandler();
        public event SplineUpdatedHandler OnSplineUpdated;

        [DataMemberIgnore]
        public BoundingBox BoundingBox { get; private set; }

        [DataMemberIgnore]
        public List<SplineNode> SplineNodes
        {
            get
            {
                splineNodes ??= new List<SplineNode>();
                return splineNodes;
            }
            set
            {
                splineNodes = value;
                EnqueueSplineUpdate();
            }
        }

        [DataMemberIgnore]
        public float TotalSplineDistance { get; internal set; }

        /// <summary>
        /// The last spline node reconnects to the first spline node. This still requires a minimum of 2 spline nodes.
        /// </summary>
        [DataMemberIgnore]
        public bool Loop
        {
            get
            {
                return loop;
            }
            set
            {
                loop = value;
                EnqueueSplineUpdate();
            }
        }

        public Spline()
        {

        }

        /// <summary>
        /// Calculates the spline using its splines nodes
        /// </summary>
        public void CalculateSpline()
        {
            var totalNodesCount = SplineNodes.Count;
            if (SplineNodes.Count > 1)
            {
                for (var i = 0; i < totalNodesCount; i++)
                {
                    var currentSplineNode = SplineNodes[i];
                    if (SplineNodes[i] == null)
                        break;

                    if (i < totalNodesCount - 1)
                    {
                        var nextSplineNode = SplineNodes[i + 1];
                        if (nextSplineNode == null)
                            break;

                        currentSplineNode.TargetWorldPosition = nextSplineNode.WorldPosition;
                        currentSplineNode.TargetTangentInWorldPosition = nextSplineNode.TangentInWorldPosition;

                        SplineNodes[i].CalculateBezierCurve();

                        // Update the rotation of the previous curve last bezierpoint to the rotation of the first bezier point in the current curve
                        if (i > 0)
                            SplineNodes[i - 1].UpdateLastBezierPointRotation(SplineNodes[i].GetBezierPoints()[0].Rotation);

                    }
                    else if (i == totalNodesCount - 1 && Loop)
                    {
                        var firstSplineNode = SplineNodes[0];
                        currentSplineNode.TargetWorldPosition = firstSplineNode.WorldPosition;
                        currentSplineNode.TargetTangentInWorldPosition = firstSplineNode.TangentInWorldPosition;

                        SplineNodes[i].CalculateBezierCurve();
  
                        // Update the rotation of the previous curve last bezierpoint to the rotation of the first bezier point in the current curve
                        SplineNodes[i - 1].UpdateLastBezierPointRotation(SplineNodes[i].GetBezierPoints()[0].Rotation);

                        // Update the rotation of the last bezier point in the current curve to the rotation of the first bezier point of the first curve
                        SplineNodes[i].UpdateLastBezierPointRotation(SplineNodes[0].GetBezierPoints()[0].Rotation);
                    }
                }
            }

            TotalSplineDistance = GetTotalSplineDistance();
            UpdateBoundingBox();

            OnSplineUpdated?.Invoke();
        }

        /// <summary>
        /// Retrieves the total distance of the spline
        /// </summary>
        /// <returns>The total distance of the spline</returns>
        public float GetTotalSplineDistance()
        {
            float distance = 0;
            for (var i = 0; i < SplineNodes.Count; i++)
            {
                var curve = SplineNodes[i];
                if (curve != null)
                {
                    if (Loop || !Loop && i < SplineNodes.Count - 1)
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
        public SplinePositionInfo GetPositionOnSpline(float percentage)
        {
            var splinePositionInfo = new SplinePositionInfo();
            var totalSplineDistance = GetTotalSplineDistance();
            if (totalSplineDistance <= 0)
                return splinePositionInfo;

            var requiredDistance = totalSplineDistance * (percentage / 100);
            var nextNodeDistance = 0.0f;
            var prevNodeDistance = 0.0f;

            for (var i = 0; i < SplineNodes.Count; i++)
            {
                var currentSplineNode = SplineNodes[i];
                splinePositionInfo.SplineNodeA = currentSplineNode;

                nextNodeDistance += currentSplineNode.Length;

                if (requiredDistance < nextNodeDistance)
                {
                    var targetIndex = i == splineNodes.Count - 1 ? 0 : i;
                    splinePositionInfo.SplineNodeB = splineNodes[targetIndex];

                    // Inverse lerp(betweenValue - minHeight) / (maxHeight - minHeight);
                    var percentageInCurve = (requiredDistance - prevNodeDistance) / (nextNodeDistance - prevNodeDistance) * 100;

                    splinePositionInfo.Position = currentSplineNode.GetPositionOnCurve(percentageInCurve);
                    return splinePositionInfo;
                }

                prevNodeDistance = nextNodeDistance;
            }

            splinePositionInfo.Position = splineNodes[splineNodes.Count - 2].TargetWorldPosition;

            return splinePositionInfo;
        }

        /// <summary>
        /// Retrieves the closest point on the entire spline
        /// </summary>
        /// <param name="originalPosition"></param>
        /// <returns></returns>
        public SplinePositionInfo GetClosestPointOnSpline(Vector3 originalPosition)
        {
            SplinePositionInfo currentClosestPoint = null;
            for (var i = 0; i < splineNodes.Count; i++)
            {
                if (!Loop && i == splineNodes.Count - 1)
                {
                    break;
                }

                var curNode = splineNodes[i];
                var closestPoint = curNode.GetClosestPointOnCurve(originalPosition);
                closestPoint.SplineNodeA = curNode;
                closestPoint.SplineNodeAIndex = i;

                if (i + 1 <= splineNodes.Count - 1)
                {
                    closestPoint.SplineNodeB = splineNodes[i + 1];
                    closestPoint.SplineNodeBIndex = i + 1;
                }
                else
                {
                    closestPoint.SplineNodeB = splineNodes[0];
                    closestPoint.SplineNodeBIndex = 0;
                }

                if (currentClosestPoint == null || closestPoint.DistanceToOrigin < currentClosestPoint.DistanceToOrigin)
                {
                    currentClosestPoint = closestPoint;
                }
            }

            return currentClosestPoint;
        }

        public void DeregisterSplineNodeDirtyEvents()
        {
            for (var i = 0; i < SplineNodes?.Count; i++)
            {
                var splineNode = SplineNodes[i];
                if (splineNode != null)
                {
                    splineNode.OnSplineNodeDirty -= EnqueueSplineUpdate;
                }
            }
        }

        /// <summary>
        /// First unsubscribes all splineNodeEvents and then registers all spline node events to invoke that the spline is dirty.
        /// </summary>
        public void RegisterSplineNodeDirtyEvents()
        {
            DeregisterSplineNodeDirtyEvents();

            for (var i = 0; i < SplineNodes?.Count; i++)
            {
                var splineNode = SplineNodes[i];
                if (splineNode != null)
                {
                    splineNode.OnSplineNodeDirty += EnqueueSplineUpdate;
                }
            }
        }

        /// <summary>
        /// Triggers that the current spline should enqueue for an update
        /// </summary>
        public void EnqueueSplineUpdate()
        {
            OnSplineDirty?.Invoke();
        }

        private void UpdateBoundingBox()
        {
            var allCurvePointsPositions = new List<Vector3>();
            for (var i = 0; i < splineNodes.Count; i++)
            {
                if (!Loop && i == splineNodes.Count - 1)
                {
                    break;
                }

                var positions = splineNodes[i].GetBezierPoints();
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
            BoundingBox = NewBoundingBox;
        }
    }
}
