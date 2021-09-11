using Stride.Engine.Splines;
using Stride.Engine.Splines.Components;
using Stride.Games;
using Stride.Rendering;
using Stride.Core.Mathematics;

namespace Stride.Engine.Processors
{
    /// <summary>
    /// The processor for <see cref="SplineFollowerComponent"/>.
    /// </summary>
    public class SplineTravellerTransformProcessor : EntityProcessor<SplineTravellerComponent, SplineTravellerTransformProcessor.SplineTravellerTransformationInfo>
    {
        private SplineTravellerComponent splineTravellerComponent;
        private Entity entity;

        private SplineNodeComponent currentSplineNodeComponent { get; set; }
        private SplineNodeComponent targetSplineNodeComponent { get; set; }
        private Vector3 currentTargetPos { get; set; } = new Vector3(0);
        private int currentCurveIndex = 0;
        private int currentCurvePointIndex = 0;
        private Vector3 velocity { get; set; } = new Vector3(0);
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
            currentTargetPos = new Vector3(0);
            currentCurveIndex = 0;
            currentCurvePointIndex = 0;
            velocity = new Vector3(0);
            currentSplinePoints = null;
        }

        public class SplineTravellerTransformationInfo
        {
            public SplineTravellerViewHierarchyTransformOperation TransformOperation;
        }

        public override void Draw(RenderContext context)
        {
            if (splineTravellerComponent?.SplineComponent == null)
                return;

            if (splineTravellerComponent.SplineComponent.GetTotalSplineDistance() > 0 && currentSplinePoints == null)
            {
                SetInitialTargets();
            }

            if (splineTravellerComponent.IsMoving)
            {
                var oriPos = entity.Transform.WorldMatrix.TranslationVector;
                velocity = (currentTargetPos - oriPos);
                velocity.Normalize();
                velocity *= splineTravellerComponent.Speed;
                velocity *= (float)context.Time.WarpElapsed.TotalSeconds;

                var movePos = oriPos + velocity;

                entity.Transform.Position += velocity;

                if (Vector3.Distance(movePos, currentTargetPos) < 0.2)
                {
                    SetNextTarget();
                }
            }
        }

        private void SetInitialTargets()
        {
            var splinePositionInfo = splineTravellerComponent.SplineComponent.GetPositionOnSpline(splineTravellerComponent.Percentage);
            if (entity != null)
            {
                entity.Transform.Position = splinePositionInfo.Position;
                entity.Transform.UpdateWorldMatrix();

                currentSplineNodeComponent = splinePositionInfo.CurrentSplineNode;
                targetSplineNodeComponent = splinePositionInfo.TargetSplineNode;

                currentSplinePoints = currentSplineNodeComponent.GetBezierCurvePoints();
                SetNewTargetPosition();
            }
        }

        private void SetNextTarget()
        {
            if (currentCurvePointIndex + 1 < currentSplinePoints.Length)// is there a next curve point?
            {
                SetNewTargetPosition();
                currentCurvePointIndex++;
            }
            else
            {
                //is there a next Spline node?
                if (currentCurveIndex + 2 < splineTravellerComponent.SplineComponent.Nodes.Count)
                {
                    GoToNextSplineNode();
                }
                else
                {
                    //In the end, its doesn't even matter
                    splineTravellerComponent.ActivateOnSplineEndReached();
                }
            }

            //currentSplineNodeComponent = currentSplinePoints[currentCurvePointIndex + 1];
            //currentCurveIndex++;
            //currentCurvePointIndex = 0;

            ////Get next Node
            //var nextNode = SplineComponent.Nodes _currentSplinePointIndex.script.node.nextNode;
            //if (targetSplineNodeComponent == null)
            //{
            //    IsMoving = false;
            //    //GoToNextSplineNode(nextNode);
            //}
        }

        private void GoToNextSplineNode()
        {
            currentCurvePointIndex = 0;
            currentSplineNodeComponent = targetSplineNodeComponent;
            currentSplinePoints = currentSplineNodeComponent.GetBezierCurvePoints();

            currentCurveIndex++;
            targetSplineNodeComponent = splineTravellerComponent.SplineComponent.Nodes[currentCurveIndex + 1];

            SetNextTarget();
        }

        private void SetNewTargetPosition()
        {
            currentTargetPos = currentSplinePoints[currentCurvePointIndex].Position;
        }
    }
}
