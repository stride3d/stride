using Stride.Core.Mathematics;
using Stride.Engine.Splines;
using Stride.Engine.Splines.Components;
using Stride.Games;

namespace Stride.Engine.Processors
{
    /// <summary>
    /// The processor for <see cref="SplineTravellerComponent"/>.
    /// </summary>
    public class SplineTravellerTransformProcessor : EntityProcessor<SplineTravellerComponent, SplineTravellerTransformProcessor.SplineTravellerTransformationInfo>
    {
        private SplineTravellerComponent splineTravellerComponent;
        private Entity entity;

        private SplineNodeComponent currentSplineNodeComponent { get; set; }
        private SplineNodeComponent targetSplineNodeComponent { get; set; }
        private Vector3 targetCurvePointWorldPosition { get; set; } = new Vector3(0);
        private int targetCurveIndex = 0;
        private int currentCurvePointIndex = 0;
        private BezierCurve.BezierPoint[] currentSplinePoints = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="SplineTransformProcessor"/> class.
        /// </summary>
        public SplineTravellerTransformProcessor()
            : base(typeof(TransformComponent))
        {
        }

        protected override SplineTravellerTransformationInfo GenerateComponentData(Entity entity, SplineTravellerComponent component)
        {
            return new SplineTravellerTransformationInfo
            {
                TransformOperation = new SplineTravellerViewHierarchyTransformOperation(component),
            };
        }

        protected override bool IsAssociatedDataValid(Entity entity, SplineTravellerComponent component, SplineTravellerTransformationInfo associatedData)
        {
            return component == associatedData.TransformOperation.SplineTravellerComponent;
        }

        protected override void OnEntityComponentAdding(Entity entity, SplineTravellerComponent component, SplineTravellerTransformationInfo data)
        {
            // Register model view hierarchy update
            entity.Transform.PostOperations.Add(data.TransformOperation);
            splineTravellerComponent = component;
            this.entity = entity;
        }

        protected override void OnEntityComponentRemoved(Entity entity, SplineTravellerComponent component, SplineTravellerTransformationInfo data)
        {
            // Unregister model view hierarchy update
            entity.Transform.PostOperations.Remove(data.TransformOperation);

            splineTravellerComponent = null;
            this.entity = null;
            currentSplineNodeComponent = null;
            targetSplineNodeComponent = null;
            targetCurvePointWorldPosition = new Vector3(0);
            targetCurveIndex = 0;
            currentCurvePointIndex = 0;
            currentSplinePoints = null;
        }

        public class SplineTravellerTransformationInfo
        {
            public SplineTravellerViewHierarchyTransformOperation TransformOperation;
        }

        private void SetInitialTargets()
        {
            var splinePositionInfo = splineTravellerComponent.SplineComponent.GetPositionOnSpline(splineTravellerComponent.Percentage);
            if (entity != null)
            {
                var firstNode = splineTravellerComponent.SplineComponent.Nodes[0];
                entity.Transform.Position = EntityTransformExtensions.WorldToLocal(splineTravellerComponent.SplineComponent.Entity.Transform, splinePositionInfo.Position);
                entity.Transform.Position += splineTravellerComponent.SplineComponent.Entity.Transform.Position;
                entity.Transform.UpdateLocalMatrix();

                currentSplineNodeComponent = splinePositionInfo.CurrentSplineNode;
                targetSplineNodeComponent = splinePositionInfo.TargetSplineNode;

                currentSplinePoints = currentSplineNodeComponent.GetBezierCurvePoints();

                if (splineTravellerComponent.SplineComponent.Nodes.Count == splinePositionInfo.CurrentSplineNodeIndex + 1)
                {
                    targetCurveIndex = splinePositionInfo.CurrentSplineNodeIndex + 1;
                }
                else
                {
                    targetCurveIndex = 0;
                }

                targetCurvePointWorldPosition = currentSplinePoints[currentCurvePointIndex].Position;
            }
        }

        public override void Update(GameTime time)
        {
            if (splineTravellerComponent?.SplineComponent == null)
                return;

            if (splineTravellerComponent.Dirty)//&& splineTravellerComponent.SplineComponent.TotalSplineDistance > 0
            {
                SetInitialTargets();
                splineTravellerComponent.Dirty = false;
            }

            if (splineTravellerComponent.IsMoving)
            {
                var entityWorldPosition = entity.Transform.WorldMatrix.TranslationVector;

                var velocity = (targetCurvePointWorldPosition - entityWorldPosition);
                velocity.Normalize();
                velocity *= splineTravellerComponent.Speed * (float)time.Elapsed.TotalSeconds;

                entity.Transform.Position += velocity;
                entity.Transform.UpdateWorldMatrix();

                var distance = Vector3.Distance(entity.Transform.WorldMatrix.TranslationVector, targetCurvePointWorldPosition);

                if (distance < 0.25)
                {
                    SetNextTarget();
                }
            }
        }

        private void SetNextTarget()
        {
            var nodesCount = splineTravellerComponent.SplineComponent.Nodes.Count;

            // is there a next curve point?
            if (currentCurvePointIndex + 1 < currentSplinePoints.Length)
            {
                targetCurvePointWorldPosition = currentSplinePoints[currentCurvePointIndex].Position;
                currentCurvePointIndex++;
            }
            else
            {
                //is there a next Spline node?
                if (targetCurveIndex + 1 < nodesCount || splineTravellerComponent.SplineComponent.Loop)
                {
                    GoToNextSplineNode(nodesCount);
                }
                else
                {
                    //In the end, its doesn't even matter
                    splineTravellerComponent.ActivateOnSplineEndReached();
                    splineTravellerComponent.IsMoving = false;
                }
            }
        }

        private void GoToNextSplineNode(int nodesCount)
        {
            currentCurvePointIndex = 0;
            currentSplineNodeComponent = targetSplineNodeComponent;
            currentSplinePoints = currentSplineNodeComponent.GetBezierCurvePoints();

            targetCurveIndex++;
            if (targetCurveIndex < nodesCount)
            {
                targetSplineNodeComponent = splineTravellerComponent.SplineComponent.Nodes[targetCurveIndex];
            }
            else if (targetCurveIndex == nodesCount && splineTravellerComponent.SplineComponent.Loop)
            {
                targetSplineNodeComponent = splineTravellerComponent.SplineComponent.Nodes[0];
                targetCurveIndex = 0;
            }

            SetNextTarget();
        }
    }
}
