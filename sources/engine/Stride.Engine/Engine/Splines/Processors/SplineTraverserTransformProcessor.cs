//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Mathematics;
using Stride.Engine.Splines.Components;
using Stride.Engine.Splines.HierarchyTransformOperations;
using Stride.Engine.Splines.Models;
using Stride.Games;

namespace Stride.Engine.Splines.Processors
{
    /// <summary>
    /// The processor for <see cref="SplineTraverserComponent"/>.
    /// </summary>
    public class SplineTraverserTransformProcessor : EntityProcessor<SplineTraverserComponent, SplineTraverserTransformProcessor.SplineTraverserTransformationInfo>
    {
        private SplineTraverserComponent splineTraverserComponent;
        private Entity entity;

        private SplineNode originSplineNode { get; set; }
        private int originSplineNodeIndex = 0;

        private SplineNode targetSplineNode { get; set; }
        private int targetSplineNodeIndex = 0;

        private bool attachedToSpline = false;
        private BezierPoint[] bezierPointsToTraverse = null;

        private BezierPoint originBezierPoint { get; set; }
        private BezierPoint targetBezierPoint { get; set; }
        private int bezierPointIndex = 0;


        /// <summary>
        /// Initializes a new instance of the <see cref="SplineTransformProcessor"/> class.
        /// </summary>
        public SplineTraverserTransformProcessor()
            : base(typeof(TransformComponent))
        {
        }

        protected override SplineTraverserTransformationInfo GenerateComponentData(Entity entity, SplineTraverserComponent component)
        {
            return new SplineTraverserTransformationInfo
            {
                TransformOperation = new SplineTraverserViewHierarchyTransformOperation(component),
            };
        }

        protected override bool IsAssociatedDataValid(Entity entity, SplineTraverserComponent component, SplineTraverserTransformationInfo associatedData)
        {
            return component == associatedData.TransformOperation.SplineTraverserComponent;
        }

        protected override void OnEntityComponentAdding(Entity entity, SplineTraverserComponent component, SplineTraverserTransformationInfo data)
        {
            // Register model view hierarchy update
            entity.Transform.PostOperations.Add(data.TransformOperation);
            splineTraverserComponent = component;
            this.entity = entity;
        }

        protected override void OnEntityComponentRemoved(Entity entity, SplineTraverserComponent component, SplineTraverserTransformationInfo data)
        {
            // Unregister model view hierarchy update
            entity.Transform.PostOperations.Remove(data.TransformOperation);

            splineTraverserComponent = null;
            this.entity = null;
            originSplineNode = null;
            targetSplineNode = null;
            targetBezierPoint = null;
            targetSplineNodeIndex = 0;
            bezierPointIndex = 0;
            bezierPointsToTraverse = null;
        }

        public class SplineTraverserTransformationInfo
        {
            public SplineTraverserViewHierarchyTransformOperation TransformOperation;
        }

        private void CalculateTargets()
        {
            if (entity != null && splineTraverserComponent?.SplineComponent?.Nodes?.Count > 1 && splineTraverserComponent?.SplineComponent?.Spline?.SplineNodes?.Count > 1)
            {
                // A spline traverser should target the closes two spline nodes. 
                var currentPositionOfTraverser = splineTraverserComponent.Entity.Transform.WorldMatrix.TranslationVector;
                var splinePositionInfo = splineTraverserComponent.SplineComponent.GetClosestPointOnSpline(currentPositionOfTraverser);

                // Are we going backwards?
                if (splineTraverserComponent.Speed < 0)
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

                    bezierPointIndex = bezierPointsToTraverse.Length - 1;
                    targetBezierPoint = bezierPointsToTraverse[bezierPointIndex]; //Take the last position at the end of the curve
                }
                else // Forwards traversing
                {
                    originSplineNode = splinePositionInfo.SplineNodeAIndex > splinePositionInfo.SplineNodeBIndex ? splinePositionInfo.SplineNodeB : splinePositionInfo.SplineNodeA;
                    originSplineNodeIndex = splinePositionInfo.SplineNodeAIndex > splinePositionInfo.SplineNodeBIndex ? splinePositionInfo.SplineNodeBIndex : splinePositionInfo.SplineNodeAIndex;

                    targetSplineNode = splineTraverserComponent?.SplineComponent?.Nodes[originSplineNodeIndex + 1].SplineNode;
                    targetSplineNodeIndex = splinePositionInfo.SplineNodeAIndex + 1;

                    bezierPointsToTraverse = originSplineNode.GetBezierPoints();

                    if (bezierPointsToTraverse == null)
                    {
                        return;
                    }

                    bezierPointIndex = 0;
                    targetBezierPoint = bezierPointsToTraverse[bezierPointIndex];
                }

                attachedToSpline = true;
                splineTraverserComponent.Dirty = false;

            }
        }

        public override void Update(GameTime time)
        {
            if (splineTraverserComponent?.SplineComponent == null)
                return;

            if (splineTraverserComponent.Dirty || !attachedToSpline)
            {
                CalculateTargets();
            }

            if (splineTraverserComponent.IsMoving && splineTraverserComponent.Speed != 0 && attachedToSpline)
            {
                UpdatePosition(time);

                var distance = Vector3.Distance(entity.Transform.WorldMatrix.TranslationVector, targetBezierPoint.Position);

                if (distance < 0.25)
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
            velocity *= Math.Abs(splineTraverserComponent.Speed) * (float)time.Elapsed.TotalSeconds;

            entity.Transform.Position += velocity;
            entity.Transform.UpdateWorldMatrix();
        }

        private void SetNextTarget()
        {
            var nodesCount = splineTraverserComponent.SplineComponent.Nodes.Count;

            // Are we going backwards?
            if (splineTraverserComponent.Speed < 0)
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
                    if (targetSplineNodeIndex - 1 >= 0 || splineTraverserComponent.SplineComponent.Spline.Loop)
                    {
                        GoToNextSplineNode(nodesCount);
                    }
                    else
                    {
                        //In the end, its doesn't even matter
                        splineTraverserComponent.ActivateOnSplineEndReached();
                        splineTraverserComponent.IsMoving = false;
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
                    //is there a next Spline node?
                    if (targetSplineNodeIndex + 1 < nodesCount || splineTraverserComponent.SplineComponent.Spline.Loop)
                    {
                        GoToNextSplineNode(nodesCount);
                    }
                    else
                    {
                        //In the end, its doesn't even matter
                        splineTraverserComponent.ActivateOnSplineEndReached();
                        splineTraverserComponent.IsMoving = false;
                    }
                }
            }
        }

        private void GoToNextSplineNode(int nodesCount)
        {
            // Are we going backwards?
            if (splineTraverserComponent.Speed < 0)
            {
                originSplineNode = targetSplineNode;

                targetSplineNodeIndex--;
                if (targetSplineNodeIndex >= 0)
                {
                    targetSplineNode = splineTraverserComponent.SplineComponent.Nodes[targetSplineNodeIndex].SplineNode;
                }
                else if (targetSplineNodeIndex < 0 && splineTraverserComponent.SplineComponent.Spline.Loop)
                {
                    targetSplineNodeIndex = nodesCount - 1;
                    targetSplineNode = splineTraverserComponent.SplineComponent.Nodes[targetSplineNodeIndex].SplineNode;
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
                    targetSplineNode = splineTraverserComponent.SplineComponent.Nodes[targetSplineNodeIndex].SplineNode;
                }
                else if (targetSplineNodeIndex == nodesCount && splineTraverserComponent.SplineComponent.Spline.Loop)
                {
                    targetSplineNode = splineTraverserComponent.SplineComponent.Nodes[0].SplineNode;
                    targetSplineNodeIndex = 0;
                }
            }

            SetNextTarget();
        }
    }
}
