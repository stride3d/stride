using Stride.Core.Mathematics;
using Stride.Engine.Splines;
using Stride.Engine.Splines.Components;
using Stride.Engine.Splines.Models;
using Stride.Games;

namespace Stride.Engine.Processors
{
    /// <summary>
    /// The processor for <see cref="SplineTraverserComponent"/>.
    /// </summary>
    public class SplineTraverserTransformProcessor : EntityProcessor<SplineTraverserComponent, SplineTraverserTransformProcessor.SplineTraverserTransformationInfo>
    {
        private SplineTraverserComponent splineTraverserComponent;
        private Entity entity;

        private SplineNodeComponent currentSplineNodeComponent { get; set; }
        private SplineNodeComponent targetSplineNodeComponent { get; set; }
        private Vector3 targetSplinePointWorldPosition { get; set; } = new Vector3(0);
        private bool attachedToSpline = false;
        private int targetSplineNodeIndex = 0;
        private int currentSplinePointIndex = 0;
        private BezierPoint[] currentSplinePoints = null;

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
            currentSplineNodeComponent = null;
            targetSplineNodeComponent = null;
            targetSplinePointWorldPosition = new Vector3(0);
            targetSplineNodeIndex = 0;
            currentSplinePointIndex = 0;
            currentSplinePoints = null;
        }

        public class SplineTraverserTransformationInfo
        {
            public SplineTraverserViewHierarchyTransformOperation TransformOperation;
        }

        private void SetInitialTargets()
        {
            if (entity != null && splineTraverserComponent.SplineComponent?.Nodes.Count > 1)
            {
                // A spline traverser always starts at the first node and targets the next node

                //var splinePositionInfo = splineTraverserComponent.SplineComponent.GetPositionOnSpline(splineTraverserComponent.Percentage);
                currentSplineNodeComponent = splineTraverserComponent.SplineComponent.Nodes[0];
                targetSplineNodeComponent = splineTraverserComponent.SplineComponent.Nodes[1];

                entity.Transform.Position = EntityTransformExtensions.WorldToLocal(splineTraverserComponent.SplineComponent.Entity.Transform, currentSplineNodeComponent.SplineNode.WorldPosition);
                //entity.Transform.Position += splineTraverserComponent.SplineComponent.Entity.Transform.Position;
                entity.Transform.UpdateLocalMatrix();
                entity.Transform.UpdateWorldMatrix();

                currentSplinePoints = currentSplineNodeComponent.GetBezierCurvePoints();

                if(currentSplinePoints == null)
                {
                    return;
                }

                //if (splineTraverserComponent.SplineComponent.Nodes.Count == splinePositionInfo.CurrentSplineNodeIndex + 1)
                //{
                //    targetCurveIndex = splinePositionInfo.CurrentSplineNodeIndex + 1;
                //}
                //else
                //{
                //    targetCurveIndex = 0;
                //}

                targetSplineNodeIndex = 1;
                currentSplinePointIndex = 1;

                targetSplinePointWorldPosition = currentSplinePoints[currentSplinePointIndex].Position;

                attachedToSpline = true;
            }
        }

        public override void Update(GameTime time)
        {
            if (splineTraverserComponent?.SplineComponent == null)
                return;

            if (splineTraverserComponent.Dirty && !attachedToSpline)
            {
                SetInitialTargets();
                splineTraverserComponent.Dirty = false;
            }

            if (splineTraverserComponent.IsMoving && attachedToSpline)
            {
                var entityWorldPosition = entity.Transform.WorldMatrix.TranslationVector;

                var velocity = (targetSplinePointWorldPosition - entityWorldPosition);
                velocity.Normalize();
                velocity *= splineTraverserComponent.Speed * (float)time.Elapsed.TotalSeconds;

                entity.Transform.Position += velocity;
                entity.Transform.UpdateWorldMatrix();

                var distance = Vector3.Distance(entity.Transform.WorldMatrix.TranslationVector, targetSplinePointWorldPosition);

                if (distance < 0.25)
                {
                    SetNextTarget();
                }
            }
        }

        private void SetNextTarget()
        {
            var nodesCount = splineTraverserComponent.SplineComponent.Nodes.Count;

            // is there a next curve point?
            if (currentSplinePointIndex + 1 < currentSplinePoints.Length)
            {
                targetSplinePointWorldPosition = currentSplinePoints[currentSplinePointIndex].Position;
                currentSplinePointIndex++;
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

        private void GoToNextSplineNode(int nodesCount)
        {
            currentSplinePointIndex = 0;
            currentSplineNodeComponent = targetSplineNodeComponent;
            currentSplinePoints = currentSplineNodeComponent.GetBezierCurvePoints();

            targetSplineNodeIndex++;
            if (targetSplineNodeIndex < nodesCount)
            {
                targetSplineNodeComponent = splineTraverserComponent.SplineComponent.Nodes[targetSplineNodeIndex];
            }
            else if (targetSplineNodeIndex == nodesCount && splineTraverserComponent.SplineComponent.Spline.Loop)
            {
                targetSplineNodeComponent = splineTraverserComponent.SplineComponent.Nodes[0];
                targetSplineNodeIndex = 0;
            }

            SetNextTarget();
        }
    }
}
