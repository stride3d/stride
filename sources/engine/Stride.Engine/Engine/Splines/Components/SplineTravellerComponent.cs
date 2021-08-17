using Stride.Core;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Core.Mathematics;
using Stride.Core.Annotations;
using System;

namespace Stride.Engine.Splines.Components
{
    /// <summary>
    /// Component representing a Spline traveller.
    /// </summary>
    [DataContract("SplineTravellerComponent")]
    [Display("SplineTraveller", Expand = ExpandRule.Once)]
    [DefaultEntityComponentProcessor(typeof(SplineTravellerTransformProcessor))]
    [ComponentCategory("Splines")]
    public sealed class SplineTravellerComponent : EntityComponent
    {
        public SplineComponent SplineComponent { get; set; }


        public float Speed { get; set; } = 1;

        private Vector3 velocity { get; set; } = new Vector3(0);

        public bool IsMoving { get; set; }

        public bool IsReverseTravelling { get; set; }


       


        [DataMemberRange(0.0f, 100.0f, 0.1f, 1.0f, 4)]
        [Display("Percentage")]
        public float Percentage
        {
            get { return _percentage; }
            set
            {
                _percentage = value;
                if (SplineComponent != null && SplineComponent.GetTotalSplineDistance() > 0)
                {
                    var splinePositionInfo = SplineComponent.GetPositionOnSpline(_percentage);
                    Entity.Transform.Position = splinePositionInfo.Position;
                    Entity.Transform.UpdateWorldMatrix();

                    currentSplineNodeComponent = splinePositionInfo.CurrentSplineNode;
                    targetSplineNodeComponent = splinePositionInfo.TargetSplineNode;
                }
            }
        }
        private float _percentage = 0f;

        /// <summary>
        /// Event triggered when the last node of the spline has been reached
        /// </summary>
        public delegate void SplineTravellerEndReachedHandler();
        public event SplineTravellerEndReachedHandler OnSplineEndReached;

        /// <summary>
        /// Event triggered when a spline node has been reached. Does not get triggered when the last node of the spline has been reached.
        /// </summary>
        /// <param name="splineNode"></param>
        public delegate void SplineTravellerNodeReachedHandler(SplineNodeComponent splineNode);
        public event SplineTravellerNodeReachedHandler OnSplineNodeReached;

        public SplineTravellerComponent()
        {
        }

        internal void Initialize()
        {
        }

        private SplineNodeComponent currentSplineNodeComponent { get; set; }
        private SplineNodeComponent targetSplineNodeComponent { get; set; }
        private Vector3 currentTargetPos { get; set; } = new Vector3(0);
        private int currentCurveIndex = 0;
        private int currentCurvePointIndex = 0;

        internal void Update(TransformComponent transformComponent)
        {
            //if (SplineComponent != null && IsMoving)
            //{
            //    var oriPos = Entity.Transform.WorldMatrix.TranslationVector;
            //    velocity = (currentTargetPos - oriPos);
            //    velocity.Normalize();
            //    velocity *= Speed; 
            //    //velocity *= (float)Game.UpdateTime.Elapsed.TotalSeconds;
            //    //}
            //    var movePos = oriPos + velocity;

            //    Entity.Transform.Position += movePos;



            //    if (Vector3.Distance(movePos, currentTargetPos) < 0.27)
            //    {
            //        SetNextTarget();
            //    }

            //}
        }
        private void SetNextTarget()
        {
 
            //next spline point possible
            var currentSplinePoints = currentSplineNodeComponent.GetBezierCurvePoints();
            if (currentCurvePointIndex + 1 < currentSplinePoints.Length)
            {
                SetNewTargetPosition();
                currentCurvePointIndex++;

            }
            else
            {
                //is there a next node?
                if (currentCurveIndex + 1 < SplineComponent.Nodes.Count)
                {
                    GoToNextSplineNode();
                }
                else
                {
                    //OnPathEndReached();
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
            //    GoToNextSplineNode(nextNode);
            //}
        }

        private void SetNewTargetPosition()
        {
             //Take next splinePoint
            currentTargetPos = currentSplineNodeComponent.GetBezierCurvePoints()[currentCurvePointIndex].Position;
        }

        private void GoToNextSplineNode()
        {
           
            //currentNodeEntity = nextNode;
            //currentCurvePointIndex = 0;
            //currentSplineNodeComponent = targetSplineNodeComponent;
            //currentCurveIndex++;
            //targetSplineNodeComponent = SplineComponent.Nodes[currentCurveIndex];
            //SetNextTarget();
        }
    }
}
