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
        private SplineComponent splineComponent;
        private SplineNodeComponent currentSplineNodeComponent { get; set; }
        private SplineNodeComponent targetSplineNodeComponent { get; set; }
        private Vector3 currentTargetPos { get; set; } = new Vector3(0);
        private int currentCurveIndex = 0;
        private int currentCurvePointIndex = 0;
        private Vector3 velocity { get; set; } = new Vector3(0);
        private float percentage = 0f;

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

        //[DataMember(1)]
        //[Display("Editor control")]
        //public SplineTravellerControl Control = new SplineTravellerControl();

        [Display(10, "SplineComponent")]
        public SplineComponent SplineComponent
        {
            get { return splineComponent; }
            set
            {
                splineComponent = value;
                if (splineComponent != null)
                {
                    SetInitialTargets();
                }
            }
        }

        [Display(20, "Speed")]
        public float Speed { get; set; } = 1;

        [Display(40, "Moving")]
        public bool IsMoving { get; set; }

        [Display(50, "InReverse")]
        public bool IsReverseTravelling { get; set; }

        [DataMemberRange(0.0f, 100.0f, 0.1f, 1.0f, 4)]
        [Display(70, "Percentage")]
        public float Percentage
        {
            get { return percentage; }
            set
            {
                percentage = value;
                if (SplineComponent != null && SplineComponent.GetTotalSplineDistance() > 0)
                {
                    //SetInitialTargets();
                }
            }
        }

        private void SetInitialTargets()
        {
            var splinePositionInfo = SplineComponent.GetPositionOnSpline(percentage);
            Entity.Transform.Position = splinePositionInfo.Position;
            Entity.Transform.UpdateWorldMatrix();

            currentSplineNodeComponent = splinePositionInfo.CurrentSplineNode;
            targetSplineNodeComponent = splinePositionInfo.TargetSplineNode;

            var currentSplinePoints = currentSplineNodeComponent.GetBezierCurvePoints();
            //SetNewTargetPosition(currentSplinePoints);
        }










        internal void Update(TransformComponent transformComponent)
        {
            if (SplineComponent != null && IsMoving)
            {
                var oriPos = Entity.Transform.WorldMatrix.TranslationVector;
                velocity = (currentTargetPos - oriPos);
                velocity.Normalize();
                velocity *= Speed;
                //velocity *= (float)Game.UpdateTime.Elapsed.TotalSeconds;
                //}
                var movePos = oriPos + velocity;

                Entity.Transform.Position += movePos;



                //if (Vector3.Distance(movePos, currentTargetPos) < 0.2)
                //{
                //    SetNextTarget();
                //}

            }
        }

        //private void SetNextTarget()
        //{
        //    var currentSplinePoints = currentSplineNodeComponent.GetBezierCurvePoints();

        //    if (currentCurvePointIndex + 1 < currentSplinePoints.Length)// next cure point possible
        //    {
        //        SetNewTargetPosition(currentSplinePoints);
        //        currentCurvePointIndex++;
        //    }
        //    else
        //    {
        //        //is there a next Spline node?
        //        if (currentCurveIndex + 2 < SplineComponent.Nodes.Count)
        //        {
        //            GoToNextSplineNode();
        //        }
        //        else
        //        {
        //            OnSplineEndReached?.Invoke();
        //        }
        //    }

        //    //currentSplineNodeComponent = currentSplinePoints[currentCurvePointIndex + 1];
        //    //currentCurveIndex++;
        //    //currentCurvePointIndex = 0;
        //    ////Get next Node
        //    //var nextNode = SplineComponent.Nodes _currentSplinePointIndex.script.node.nextNode;
        //    //if (targetSplineNodeComponent == null)
        //    //{
        //    //    IsMoving = false;
        //    //    GoToNextSplineNode(nextNode);
        //    //}
        //}

        //private void SetNewTargetPosition(BezierCurve.BezierPoint[] splinePoints)
        //{
        //     //Take next splinePoint
        //    currentTargetPos = splinePoints[currentCurvePointIndex].Position;
        //}

        //private void GoToNextSplineNode()
        //{
        //    currentCurvePointIndex = 0;
        //    currentSplineNodeComponent = targetSplineNodeComponent;
        //    currentCurveIndex++;
        //    targetSplineNodeComponent = SplineComponent.Nodes[currentCurveIndex + 1];
        //    SetNextTarget();
        //}
    }
}
