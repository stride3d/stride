//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Core.Diagnostics;

namespace Stride.Engine.Splines.Models
{
    [DataContract]
    public class SplineTraverser
    {
        private Spline spline;
        private Entity entity;
        private float speed = 1.0f;
        private bool isMoving = false;
        private float thresholdDistance = 0.1f;

        private SplineNode originSplineNode { get; set; }
        private int originSplineNodeIndex = 0;

        private SplineNode targetSplineNode { get; set; }
        private int targetSplineNodeIndex = 0;

        private bool attachedToSpline = false;
        private BezierPoint[] bezierPointsToTraverse = null;

        private BezierPoint targetBezierPoint { get; set; }
        private int bezierPointIndex = 0;

        /// <summary>
        /// The entity that is traversing the spline
        /// </summary>
        public Entity Entity
        {
            get { return entity; }
            set
            {
                entity = value;

                if (entity == null)
                {
                    IsMoving = false;
                }

                EnqueueSplineTraverserUpdate();
            }
        }

        /// <summary>
        /// The spline to traverse
        /// No spline, no movement
        /// </summary>
        public Spline Spline
        {
            get { return spline; }
            set
            {
                spline = value;

                if (spline == null)
                {
                    IsMoving = false;
                }

                EnqueueSplineTraverserUpdate();
            }
        }

        /// <summary>
        /// The speed at which the traverser moves over the spline
        /// Use a negative value, to go in to the opposite direction
        /// Note: Using a high value, can cause jitters. With a higher speed value, it is recommended to reduced the amount of spline points
        /// </summary>
        public float Speed
        {
            get { return speed; }
            set
            {
                speed = value;

                EnqueueSplineTraverserUpdate();
            }
        }

        /// <summary>
        /// For a traverser to work we require a Spline reference, a non-zero speed and IsMoving must be True
        /// </summary>
        public bool IsMoving
        {
            get
            {
                return isMoving;
            }
            set
            {
                isMoving = value;

                EnqueueSplineTraverserUpdate();
            }
        }

        /// <summary>
        /// Event triggered when the last node of the spline has been reached
        /// Does not get triggerd if spline loops
        /// </summary>
        public delegate void SplineTraverserEndReachedHandler(SplineNode splineNode);
        public event SplineTraverserEndReachedHandler OnSplineEndReached;

        /// <summary>
        /// Event triggered when a spline node has been reached. 
        /// Does not get triggered when the last node of the spline has been reached and the spline doesn't loop.
        /// </summary>
        /// <param name="splineNode"></param>
        public delegate void SplineTraverserNodeReachedHandler(SplineNode splineNode);
        public event SplineTraverserNodeReachedHandler OnSplineNodeReached;

        /// <summary>
        /// Event triggered when the splineTraverser has become dirty
        /// </summary>
        public delegate void DirtySplineHandler();
        public event DirtySplineHandler OnSplineTraverserDirty;

        public SplineTraverser()
        {

        }

        /// <summary>
        /// Invokes the Spline is Dirty event
        /// </summary>
        public void EnqueueSplineTraverserUpdate()
        {
            OnSplineTraverserDirty?.Invoke();
        }

        public void DetermineOriginAndTarget()
        {
            if (entity != null && Spline?.SplineNodes?.Count > 1)
            {
                // A spline traverser should target the closest two spline nodes. 
                var currentPositionOfTraverser = entity.Transform.WorldMatrix.TranslationVector;
                //entity.Transform.GetWorldTransformation(out Vector3 currentPositionOfTraverser, out Quaternion rotationq, out Vector3 scale);
                var splinePositionInfo = spline.GetClosestPointOnSpline(currentPositionOfTraverser);
                var forwards = Speed > 0;

                originSplineNodeIndex = forwards ? splinePositionInfo.SplineNodeAIndex : splinePositionInfo.SplineNodeBIndex;
                targetSplineNodeIndex = forwards ? splinePositionInfo.SplineNodeBIndex : splinePositionInfo.SplineNodeAIndex;

                originSplineNode = forwards ? splinePositionInfo.SplineNodeA : splinePositionInfo.SplineNodeB;
                targetSplineNode = forwards ? splinePositionInfo.SplineNodeB : splinePositionInfo.SplineNodeA;

                bezierPointsToTraverse = forwards ? originSplineNode.GetBezierPoints() : targetSplineNode.GetBezierPoints();

                if (bezierPointsToTraverse == null)
                {
                    return;
                }

                bezierPointIndex = splinePositionInfo.ClosestBezierPointIndex;
                targetBezierPoint = bezierPointsToTraverse[bezierPointIndex];
                attachedToSpline = true;
                SetNextTarget(targetSplineNodeIndex);
            }
        }

        public void Update(GameTime time)
        {
            if (entity == null || spline == null)
            {
                return;
            }

            if (!attachedToSpline)
            {
                DetermineOriginAndTarget();
            }

            if (IsMoving && Speed != 0 && attachedToSpline)
            {
                UpdatePosition(time);

                var distance = Vector3.Distance(entity.Transform.WorldMatrix.TranslationVector, targetBezierPoint.Position);

                if (distance < thresholdDistance)
                {
                    SetNextTarget(targetSplineNodeIndex);
                }
            }
        }

        private void UpdatePosition(GameTime time)
        {
            var entityWorldPosition = entity.Transform.WorldMatrix.TranslationVector;
            var velocity = (targetBezierPoint.Position - entityWorldPosition);
            velocity.Normalize();
            velocity *= Math.Abs(Speed) * (float)time.Elapsed.TotalSeconds;

            entity.Transform.Position += velocity;
            entity.Transform.UpdateWorldMatrix();
        }

        private void SetNextTarget(int targetSplineNodeIndex)
        {
            var nodesCount = Spline.SplineNodes.Count;
            var forwards = Speed > 0;
            var backwards = !forwards;
            var indexIncrement = forwards ? 1 : -1;

            // Is there a next/previous bezier point?
            if ((forwards && bezierPointIndex + 1 < bezierPointsToTraverse.Length) || (backwards && bezierPointIndex - 1 >= 0))
            {
                bezierPointIndex += indexIncrement;
                targetBezierPoint = bezierPointsToTraverse[bezierPointIndex];
            }
            else
            {
                OnSplineNodeReached?.Invoke(targetSplineNode);

                // Is there a next Spline node?
                if ((forwards && targetSplineNodeIndex + 1 < nodesCount) || (backwards && targetSplineNodeIndex - 1 == 0))
                {
                    OnSplineEndReached?.Invoke(targetSplineNode);

                    if (!spline.Loop)
                    {
                        IsMoving = false;
                        return;
                    }
                }

                GoToNextSplineNode(nodesCount);
            }
        }

        private void GoToNextSplineNode(int nodesCount)
        {
            var forwards = Speed > 0;
            var backwards = !forwards;
            var indexIncrement = forwards ? 1 : -1;

            originSplineNode = targetSplineNode;
            targetSplineNodeIndex += indexIncrement;

            if ((forwards && targetSplineNodeIndex < nodesCount) || (backwards && targetSplineNodeIndex >= 0))
            {
                targetSplineNode = Spline.SplineNodes[targetSplineNodeIndex];
            }
            else if (Spline.Loop && (forwards && targetSplineNodeIndex == nodesCount) || (backwards && targetSplineNodeIndex < 0))
            {
                targetSplineNodeIndex = forwards ? 0 : nodesCount - 1;
                targetSplineNode = Spline.SplineNodes[targetSplineNodeIndex];
            }

            bezierPointsToTraverse = forwards ? originSplineNode.GetBezierPoints() : targetSplineNode.GetBezierPoints();
            bezierPointIndex = forwards ? bezierPointIndex = 1 : bezierPointsToTraverse.Length - 1;

            SetNextTarget(targetSplineNodeIndex);
        }
    }
}
