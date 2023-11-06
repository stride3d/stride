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
        public BoundingBox BoundingBox { get; set; }

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

            for (var i = 0; i <  SplineNodes?.Count; i++)
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

        /// <summary>
        /// Triggers that the current spline should enqueue for an update
        /// </summary>
        public void SplineUpdated()
        {
            OnSplineUpdated?.Invoke();
        }
        
        /// <summary>
        /// Retrieves the closest point on the entire spline
        /// </summary>
        /// <param name="originalPosition"></param>
        /// <returns></returns>
        public SplinePositionInfo GetClosestPointOnSpline(Vector3 originalPosition)
        {
            SplinePositionInfo currentClosestPoint = null;
            for (var i = 0; i < SplineNodes.Count; i++)
            {
                if (!Loop && i == SplineNodes.Count - 1)
                {
                    break;
                }

                var curNode = SplineNodes[i];
                var closestPoint = curNode.GetClosestPointOnBezierCurve(originalPosition);
                closestPoint.SplineNodeA = curNode;
                closestPoint.SplineNodeAIndex = i;

                if (i + 1 <=  SplineNodes.Count - 1)
                {
                    closestPoint.SplineNodeB =  SplineNodes[i + 1];
                    closestPoint.SplineNodeBIndex = i + 1;
                }
                else
                {
                    closestPoint.SplineNodeB = SplineNodes[0];
                    closestPoint.SplineNodeBIndex = 0;
                }

                if (currentClosestPoint == null || closestPoint.DistanceToOrigin < currentClosestPoint.DistanceToOrigin)
                {
                    currentClosestPoint = closestPoint;
                }
            }

            return currentClosestPoint;
        }
    }
}
