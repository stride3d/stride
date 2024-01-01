//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine.Splines.Components;
using Stride.Engine.Splines.HierarchyTransformOperations;
using Stride.Games;

namespace Stride.Engine.Splines.Processors
{
    /// <summary>
    /// The processor for <see cref="SplineTraverserComponent"/>.
    /// </summary>
    public class SplineTraverserProcessor : EntityProcessor<SplineTraverserComponent, SplineTraverserProcessor.SplineTraverserTransformationInfo>
    {
        private HashSet<SplineTraverserComponent> splineTraverserComponents = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SplineProcessor"/> class.
        /// </summary>
        public SplineTraverserProcessor()
            : base(typeof(TransformComponent))
        {
        }

        protected override SplineTraverserTransformationInfo GenerateComponentData(Entity entity, SplineTraverserComponent component)
        {
            return new SplineTraverserTransformationInfo { TransformOperation = new SplineTraverserViewHierarchyTransformOperation(component), };
        }

        protected override bool IsAssociatedDataValid(Entity entity, SplineTraverserComponent component, SplineTraverserTransformationInfo associatedData)
        {
            return component == associatedData.TransformOperation.SplineTraverserComponent;
        }

        protected override void OnEntityComponentAdding(Entity entity, SplineTraverserComponent component, SplineTraverserTransformationInfo data)
        {

            splineTraverserComponents.Add(component);

            //component.SplineTraverser.Spline = component.SplineComponent?.Spline;
            //component.SplineTraverser.Entity = entity;

            //component.SplineTraverser.OnSplineTraverserDirty += () => component.SplineTraverser.DetermineOriginAndTarget();
            component.SplineTraverser.OnSplineTraverserDirty += () => data.Update(component);

            // Register model view hierarchy update
            entity.Transform.PostOperations.Add(data.TransformOperation);
        }

        protected override void OnEntityComponentRemoved(Entity entity, SplineTraverserComponent component, SplineTraverserTransformationInfo data)
        {
            component.SplineTraverser.OnSplineTraverserDirty -= () => data.Update(component);

            // Unregister model view hierarchy update
            entity.Transform.PostOperations.Remove(data.TransformOperation);

            splineTraverserComponents.Remove(component);
        }

        public class SplineTraverserTransformationInfo
        {
            public SplineTraverserViewHierarchyTransformOperation TransformOperation;

            public void Update(SplineTraverserComponent component)
            {
                component.SplineTraverser.AttachedToSpline = false;
            }
        }

        public override void Update(GameTime time)
        {
            foreach (var component in splineTraverserComponents)
            {
                if (component.Entity == null || component.SplineComponent?.Spline == null)
                {
                    return;
                }

                if (!component.SplineTraverser.AttachedToSpline)
                {
                    DetermineOriginAndTarget(component);
                }

                if (!component.IsMoving || component.Speed == 0 || !component.SplineTraverser.AttachedToSpline)
                {
                    continue;
                }

                UpdatePosition(component, time);
                UpdateRotation(component);

                var distance = Vector3.Distance(component.Entity.Transform.WorldMatrix.TranslationVector, component.SplineTraverser.targetBezierPoint.Position);

                if (distance < component.SplineTraverser.thresholdDistance)
                {
                    SetNextTarget(component);
                }
            }
        }


        private void DetermineOriginAndTarget(SplineTraverserComponent component)
        {
            if (component.Entity == null || !(component.SplineComponent.Spline.SplineNodes?.Count > 1))
            {
                return;
            }

            var currentPositionOfTraverser = component.Entity.Transform.WorldMatrix.TranslationVector;
            var splinePositionInfo = component.SplineComponent.Spline.GetClosestPointOnSpline(currentPositionOfTraverser);
            var forwards = component.Speed > 0;

            component.SplineTraverser.targetSplineNodeIndex = forwards ? splinePositionInfo.SplineNodeBIndex : splinePositionInfo.SplineNodeAIndex;

            component.SplineTraverser.originSplineNode = forwards ? splinePositionInfo.SplineNodeA : splinePositionInfo.SplineNodeB;
            component.SplineTraverser.targetSplineNode = forwards ? splinePositionInfo.SplineNodeB : splinePositionInfo.SplineNodeA;

            component.SplineTraverser.bezierPointsToTraverse = forwards
                ? component.SplineTraverser.originSplineNode.GetBezierPoints()
                : component.SplineTraverser.targetSplineNode.GetBezierPoints();

            if (component.SplineTraverser.bezierPointsToTraverse == null)
            {
                return;
            }

            component.SplineTraverser.bezierPointIndex = splinePositionInfo.ClosestBezierPointIndex;
            component.SplineTraverser.originBezierPoint = component.SplineTraverser.bezierPointsToTraverse[component.SplineTraverser.bezierPointIndex];
            component.SplineTraverser.targetBezierPoint = component.SplineTraverser.bezierPointsToTraverse[component.SplineTraverser.bezierPointIndex];
            component.SplineTraverser.AttachedToSpline = true;
            component.SplineTraverser.startRotation = component.Entity.Transform.Rotation;
            SetNextTarget(component);
        }

        private void UpdateRotation(SplineTraverserComponent component)
        {
            if (component.SplineTraverser.IsRotating)
            {
                var entityWorldPosition = component.Entity.Transform.WorldMatrix.TranslationVector;
                var distanceBetweenBezierPoints = Vector3.Distance(component.SplineTraverser.originBezierPoint.Position, component.SplineTraverser.targetBezierPoint.Position);
                var currentDistance = Vector3.Distance(component.SplineTraverser.originBezierPoint.Position, entityWorldPosition);
                var ratio = currentDistance / distanceBetweenBezierPoints;
                try
                {
                    component.Entity.Transform.Rotation = Quaternion.Slerp(component.SplineTraverser.startRotation, component.SplineTraverser.targetBezierPoint.Rotation, ratio);
                }
                catch (Exception e)
                {
                    throw;
                }
            }
        }

        private void UpdatePosition(SplineTraverserComponent component, GameTime time)
        {
            var entityWorldPosition = component.Entity.Transform.WorldMatrix.TranslationVector;
            var velocity = (component.SplineTraverser.targetBezierPoint.Position - entityWorldPosition);
            velocity.Normalize();
            velocity *= Math.Abs(component.SplineTraverser.Speed) * (float)time.Elapsed.TotalSeconds;
            component.Entity.Transform.Position += velocity;
            component.Entity.Transform.UpdateWorldMatrix();
        }

        private void SetNextTarget(SplineTraverserComponent component)
        {
            var traverser = component.SplineTraverser;
            var nodesCount = component.SplineComponent.Spline.SplineNodes.Count;
            var forwards = traverser.Speed > 0;
            var backwards = !forwards;
            var indexIncrement = forwards ? 1 : -1;

            // Is there a next/previous bezier point?
            if ((forwards && traverser.bezierPointIndex + 1 < traverser.bezierPointsToTraverse.Length) || (backwards && traverser.bezierPointIndex - 1 >= 0))
            {
                traverser.originBezierPoint = traverser.bezierPointsToTraverse[traverser.bezierPointIndex];

                traverser.bezierPointIndex += indexIncrement;
                traverser.targetBezierPoint = traverser.bezierPointsToTraverse[traverser.bezierPointIndex];
                traverser.startRotation = component.Entity.Transform.Rotation;
            }
            else
            {
                traverser.SplineNodeReached(traverser.targetSplineNode);

                // Is there a next/previous Spline node?
                if (component.SplineComponent.Spline.Loop || (forwards && traverser.targetSplineNodeIndex + 1 < nodesCount) || (backwards && traverser.targetSplineNodeIndex - 1 == 0))
                {
                    SetNextSplineNode(component, nodesCount, forwards, backwards, indexIncrement);
                }
                else
                {
                    traverser.isMoving = false;
                    traverser.SplineEndReached(traverser.targetSplineNode);
                }
            }
        }

        private void SetNextSplineNode(SplineTraverserComponent component, int nodesCount, bool forwards, bool backwards, int indexIncrement)
        {
            var traverser = component.SplineTraverser;
            traverser.originSplineNode = traverser.targetSplineNode;
            traverser.targetSplineNodeIndex += indexIncrement;

            if ((forwards && traverser.targetSplineNodeIndex < nodesCount) || (backwards && traverser.targetSplineNodeIndex >= 0))
            {
                traverser.targetSplineNode = component.SplineComponent.Spline.SplineNodes[traverser.targetSplineNodeIndex];
            }
            else if (component.SplineComponent.Spline.Loop && ((forwards && traverser.targetSplineNodeIndex == nodesCount) || (backwards && traverser.targetSplineNodeIndex < 0)))
            {
                traverser.SplineEndReached(traverser.targetSplineNode);
                traverser.targetSplineNodeIndex = forwards ? 0 : nodesCount - 1;
                traverser.targetSplineNode = component.SplineComponent.Spline.SplineNodes[traverser.targetSplineNodeIndex];
            }

            traverser.bezierPointsToTraverse = forwards ? traverser.originSplineNode.GetBezierPoints() : traverser.targetSplineNode.GetBezierPoints();
            traverser.bezierPointIndex = forwards ? traverser.bezierPointIndex = 0 : traverser.bezierPointsToTraverse.Length - 1;

            SetNextTarget(component);
        }
    }
}
