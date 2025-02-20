//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
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
            var transformationInfo = new SplineTraverserTransformationInfo(this, component) { TransformOperation = new SplineTraverserViewHierarchyTransformOperation(component) };

            return transformationInfo;
        }

        protected override bool IsAssociatedDataValid(Entity entity, SplineTraverserComponent component, SplineTraverserTransformationInfo associatedData)
        {
            return component == associatedData.TransformOperation.SplineTraverserComponent;
        }

        protected override void OnEntityComponentAdding(Entity entity, SplineTraverserComponent component, SplineTraverserTransformationInfo data)
        {
            if (component.SplineComponent != null)
            {
                component.SplineComponent.Spline.OnSplineDirty += data.OnSplineDirtyAction;
            }

            splineTraverserComponents.Add(component);
            component.SplineTraverser.Spline = component.SplineComponent?.Spline;
            component.SplineTraverser.Entity = entity;

            entity.Transform.PostOperations.Add(data.TransformOperation);
        }

        protected override void OnEntityComponentRemoved(Entity entity, SplineTraverserComponent component, SplineTraverserTransformationInfo data)
        {
            component.SplineComponent.Spline.OnSplineDirty -= data.OnSplineDirtyAction;
            entity.Transform.PostOperations.Remove(data.TransformOperation);
            splineTraverserComponents.Remove(component);
        }

        public class SplineTraverserTransformationInfo
        {
            public SplineTraverserViewHierarchyTransformOperation TransformOperation;
            public Spline.DirtySplineHandler OnSplineDirtyAction;

            public SplineTraverserTransformationInfo(SplineTraverserProcessor processor, SplineTraverserComponent component)
            {
                OnSplineDirtyAction = () => Update(processor, component);
            }

            private void Update(SplineTraverserProcessor processor, SplineTraverserComponent component)
            {
                processor.splineTraverserComponents.Add(component);
                component.SplineTraverser.AttachedToSpline = false;
            }
        }

        public override void Update(GameTime time)
        {
            foreach (var component in splineTraverserComponents)
            {
                if (!IsValidComponent(component))
                    continue;

                if (!component.SplineTraverser.AttachedToSpline)
                {
                    DetermineOriginAndTarget(component);
                }

                if (!component.IsMoving || component.Speed == 0 || !component.SplineTraverser.AttachedToSpline)
                {
                    continue;
                }

                UpdatePosition(component, time);
                UpdateRotation(component, time);

                //Avoid square root check. Using LengthSquared  
                var distanceSquared = (component.Entity.Transform.WorldMatrix.TranslationVector - component.SplineTraverser.targetBezierPoint.Position).LengthSquared();
                if (distanceSquared < component.SplineTraverser.thresholdDistance * component.SplineTraverser.thresholdDistance)
                {
                    SetNextTarget(component);
                }
            }
        }

        private static bool IsValidComponent(SplineTraverserComponent component)
        {
            if (component is { Entity: not null, SplineComponent.Spline: not null })
            {
                return true;
            }

            Console.WriteLine($"[Warning] Invalid component or missing Spline: {component}");
            return false;
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

        private void UpdateRotation(SplineTraverserComponent component, GameTime time)
        {
            if (!component.SplineTraverser.IsRotating)
            {
                return;
            }

            var entityWorldPosition = component.Entity.Transform.WorldMatrix.TranslationVector;
            var originPosition = component.SplineTraverser.originBezierPoint.Position;
            var targetPosition = component.SplineTraverser.targetBezierPoint.Position;

            var totalDistance = Vector3.Distance(originPosition, targetPosition);
            var currentDistance = Vector3.Distance(originPosition, entityWorldPosition);

            // divide-by-zero
            if (totalDistance < 1e-6f)
                return;
                
            var deltaTime = (float)time.Elapsed.TotalSeconds;
            var rawRatio = currentDistance / totalDistance;
            var clampedRatio = Math.Clamp(rawRatio, 0, 1);
            var easedRatio = clampedRatio * clampedRatio * (3 - 2 * clampedRatio);

              
            var rotationStep = Math.Clamp(deltaTime / totalDistance, 0, 1); 
            easedRatio = Math.Clamp(easedRatio + rotationStep, 0, 1);

            var startRotation = Quaternion.Normalize(component.SplineTraverser.startRotation);
            var targetRotation = Quaternion.Normalize(component.SplineTraverser.targetBezierPoint.Rotation);
                
            component.Entity.Transform.Rotation = Quaternion.Slerp(startRotation, targetRotation, easedRatio);
        }
        

        private void UpdatePosition(SplineTraverserComponent component, GameTime time)
        {
            var entityWorldPosition = component.Entity.Transform.WorldMatrix.TranslationVector;
            var velocity = component.SplineTraverser.targetBezierPoint.Position - entityWorldPosition;
            if (velocity.LengthSquared() > 0)
            {
                velocity.Normalize();
                velocity *= Math.Abs(component.SplineTraverser.Speed) * (float)time.Elapsed.TotalSeconds;
                component.Entity.Transform.Position += velocity;
            }

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
                    traverser.targetSplineNodeIndex += (indexIncrement * -1); //Inverse the increment
                    traverser.targetSplineNode = component.SplineComponent.Spline.SplineNodes[traverser.targetSplineNodeIndex];
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
