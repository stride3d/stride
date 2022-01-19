using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine.Splines.Models;

namespace Stride.Engine.Splines
{
    [DataContract]
    public class Spline
    {
        private bool loop;
        private List<SplineNode> splineNodes;

        /// <summary>
        /// Event triggered when the spline has been update
        /// This happens when the entity is translated or rotated, or when a SplineNode is updated
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
                DeregisterSplineNodeDirtyEvents();
                RegisterSplineNodeDirtyEvents();

                Dirty = true;
            }
        }

        [DataMemberIgnore]
        public bool Dirty { get; set; }

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
                Dirty = true;
            }
        }

        public Spline()
        {

        }

        /// <summary>
        /// Updates the splines and its splines nodes
        /// </summary>
        public void UpdateSpline()
        {
            if (Dirty)
            {
                var totalNodesCount = SplineNodes.Count;
                if (SplineNodes.Count > 1)
                {

                    for (int i = 0; i < totalNodesCount; i++)
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
                        }
                        else if (i == totalNodesCount - 1 && Loop)
                        {
                            var firstSplineNode = SplineNodes[0];
                            currentSplineNode.TargetWorldPosition = firstSplineNode.WorldPosition;
                            currentSplineNode.TargetTangentInWorldPosition = firstSplineNode.TangentInWorldPosition;

                            SplineNodes[i].CalculateBezierCurve();
                        }
                    }
                }

                TotalSplineDistance = GetTotalSplineDistance();
                UpdateBoundingBox();

                Dirty = false;
                OnSplineUpdated?.Invoke();
            }
        }

        /// <summary>
        /// Retrieves the total distance of the spline
        /// </summary>
        /// <returns>The total distance of the spline</returns>
        public float GetTotalSplineDistance()
        {
            float distance = 0;
            for (int i = 0; i < SplineNodes.Count; i++)
            {
                var curve = SplineNodes[i];
                if (curve != null)
                {
                    if (Loop || (!Loop && i < SplineNodes.Count - 1))
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

            for (int i = 0; i < SplineNodes.Count; i++)
            {
                var currentSplineNode = SplineNodes[i];
                splinePositionInfo.SplineNodeA = currentSplineNode;

                nextNodeDistance += currentSplineNode.Length;

                if (requiredDistance < nextNodeDistance)
                {
                    var targetIndex = (i == splineNodes.Count - 1) ? 0 : i;
                    splinePositionInfo.SplineNodeB = splineNodes[targetIndex];

                    // Inverse lerp(betweenValue - minHeight) / (maxHeight - minHeight);
                    var percentageInCurve = ((requiredDistance - prevNodeDistance) / (nextNodeDistance - prevNodeDistance)) * 100;

                    splinePositionInfo.Position = currentSplineNode.GetPositionOnCurve(percentageInCurve);
                    return splinePositionInfo;
                }

                prevNodeDistance = nextNodeDistance;
            }

            splinePositionInfo.Position = splineNodes[splineNodes.Count - 2].TargetWorldPosition;

            return splinePositionInfo;
        }


        public ClosestPointInfo GetClosestPointOnSpline(Vector3 originalPosition)
        {
            ClosestPointInfo currentClosestPoint = null;
            for (int i = 0; i < splineNodes.Count; i++)
            {
                if (!Loop && i == splineNodes.Count - 1)
                {
                    break;
                }

                var curNode = splineNodes[i];
                var closestPoint = curNode.GetClosestPointOnCurve(originalPosition);
                closestPoint.SplineNodeA = curNode;

                if(i + 1 <= splineNodes.Count-1)
                {
                    closestPoint.SplineNodeB = splineNodes[i+1];
                }
                else
                {
                    closestPoint.SplineNodeB = splineNodes[0];
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
            for (int i = 0; i < SplineNodes?.Count; i++)
            {
                var splineNode = SplineNodes[i];
                if (splineNode != null)
                {
                    splineNode.OnSplineNodeDirty -= MakeSplineDirty;
                }
            }
        }

        public void RegisterSplineNodeDirtyEvents()
        {
            for (int i = 0; i < SplineNodes?.Count; i++)
            {
                var splineNode = SplineNodes[i];
                if (splineNode != null)
                {
                    splineNode.OnSplineNodeDirty += MakeSplineDirty;
                }
            }
        }

        private void MakeSplineDirty()
        {
            Dirty = true;
        }

        private void UpdateBoundingBox()
        {
            var allCurvePointsPositions = new List<Vector3>();
            for (int i = 0; i < splineNodes.Count; i++)
            {
                if (!Loop && i == splineNodes.Count - 1)
                {
                    break;
                }

                var positions = splineNodes[i].GetBezierPoints();
                if (positions != null)
                {
                    for (int j = 0; j < positions.Length; j++)
                    {
                        if (positions[j] != null)
                            allCurvePointsPositions.Add(positions[j].Position);
                    }
                }
            }
            BoundingBox.FromPoints(allCurvePointsPositions.ToArray(), out BoundingBox NewBoundingBox);
            BoundingBox = NewBoundingBox;
        }
    }
}
