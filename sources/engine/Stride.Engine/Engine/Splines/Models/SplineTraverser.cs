//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;

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
        public delegate void SplineTraverserEndReachedHandler();
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
                //var currentPositionOfTraverser = entity.Transform.WorldMatrix.TranslationVector;
                entity.Transform.GetWorldTransformation(out Vector3 currentPositionOfTraverser, out Quaternion rotationq, out Vector3 scale);
                var splinePositionInfo = spline.GetClosestPointOnSpline(currentPositionOfTraverser);

                // Are we going backwards?
                if (Speed < 0)
                {
                    originSplineNode = splinePositionInfo.SplineNodeB;
                    originSplineNodeIndex = splinePositionInfo.SplineNodeBIndex;

                    targetSplineNode = splinePositionInfo.SplineNodeA;
                    targetSplineNodeIndex = splinePositionInfo.SplineNodeAIndex;

                    bezierPointsToTraverse = targetSplineNode.GetBezierPoints();

                    if (bezierPointsToTraverse == null)
                    {
                        return;
                    }

                    bezierPointIndex = splinePositionInfo.ClosestBezierPointIndex;
                    targetBezierPoint = bezierPointsToTraverse[bezierPointIndex - 1];
                }
                else // Forwards traversing
                {
                    originSplineNode = splinePositionInfo.SplineNodeAIndex > splinePositionInfo.SplineNodeBIndex ? splinePositionInfo.SplineNodeB : splinePositionInfo.SplineNodeA;
                    originSplineNodeIndex = splinePositionInfo.SplineNodeAIndex > splinePositionInfo.SplineNodeBIndex ? splinePositionInfo.SplineNodeBIndex : splinePositionInfo.SplineNodeAIndex;

                    targetSplineNode = spline.SplineNodes[originSplineNodeIndex + 1];
                    targetSplineNodeIndex = splinePositionInfo.SplineNodeAIndex + 1;

                    bezierPointsToTraverse = originSplineNode.GetBezierPoints();

                    if (bezierPointsToTraverse == null)
                    {
                        return;
                    }

                    bezierPointIndex = bezierPointIndex = splinePositionInfo.ClosestBezierPointIndex;
                    targetBezierPoint = bezierPointsToTraverse[bezierPointIndex + 1];
                }

                attachedToSpline = true;
            }
        }

        public void Update(GameTime time)
        {
            if(entity == null || spline == null)
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
                    SetNextTarget();
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

        private void SetNextTarget()
        {
            var nodesCount = Spline.SplineNodes.Count;

            // Are we going backwards?
            if (Speed < 0)
            {
                // Is there a previous bezier point?
                if (bezierPointIndex - 1 >= 0)
                {
                    bezierPointIndex--;
                    targetBezierPoint = bezierPointsToTraverse[bezierPointIndex];
                }
                else
                {
                    // Is there a next Spline node?
                    if (targetSplineNodeIndex - 1 >= 0 || Spline.Loop)
                    {
                        GoToNextSplineNode(nodesCount);
                    }
                    else
                    {
                        //In the end, its doesn't even matter
                        OnSplineEndReached();
                        IsMoving = false;
                    }
                }

            }
            else // We are going forward
            {

                // Is there a next point on the current spline?
                if (bezierPointIndex + 1 < bezierPointsToTraverse.Length)
                {
                    bezierPointIndex++;
                    targetBezierPoint = bezierPointsToTraverse[bezierPointIndex];
                }
                else
                {
                    OnSplineNodeReached?.Invoke(targetSplineNode);

                    // Is there a next Spline node?
                    if (targetSplineNodeIndex + 1 < nodesCount || Spline.Loop)
                    {
                        GoToNextSplineNode(nodesCount);
                    }
                    else
                    {
                        // In the end, its doesn't even matter
                        OnSplineEndReached?.Invoke();
                        IsMoving = false;
                    }
                }
            }
        }

        private void GoToNextSplineNode(int nodesCount)
        {
            // Are we going backwards?
            if (Speed < 0)
            {
                originSplineNode = targetSplineNode;

                targetSplineNodeIndex--;
                if (targetSplineNodeIndex >= 0)
                {
                    targetSplineNode = Spline.SplineNodes[targetSplineNodeIndex];
                }
                else if (targetSplineNodeIndex < 0 && Spline.Loop)
                {
                    targetSplineNodeIndex = nodesCount - 1;
                    targetSplineNode = Spline.SplineNodes[targetSplineNodeIndex];
                }

                bezierPointsToTraverse = targetSplineNode.GetBezierPoints();
                bezierPointIndex = bezierPointsToTraverse.Length - 1;

            }
            else // We are going forwards?
            {
                bezierPointIndex = 0;
                originSplineNode = targetSplineNode;
                bezierPointsToTraverse = originSplineNode.GetBezierPoints();

                targetSplineNodeIndex++;
                if (targetSplineNodeIndex < nodesCount)
                {
                    targetSplineNode = Spline.SplineNodes[targetSplineNodeIndex];
                }
                else if (targetSplineNodeIndex == nodesCount && Spline.Loop)
                {
                    targetSplineNode = Spline.SplineNodes[0];
                    targetSplineNodeIndex = 0;
                }
            }

            SetNextTarget();
        }
    }
}
