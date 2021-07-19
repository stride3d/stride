using Stride.Core;
using Stride.Engine.Design;
using Stride.Engine.Processors;

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
        //public SplineComponent SplineComponent { get; set; }
        //public bool IsMoving { get; set; }
        //public bool UsePhysics { get; set; }
        //public bool IsReverseTravelling { get; set; }


        //private int _currentNodeIndex = 0;
        //private int _currentSplinePointIndex = 0;
        //private BezierCurveComponent _currentSplineNodeComponent { get; set; }
        //private BezierCurveComponent _nextSplineNodeComponent { get; set; }
        //private BezierCurveComponent _previousSplineNodeComponent { get; set; }

        //public float Speed { get; set; } = 1;
        //public Vector3 Velocity { get; set; } = new Vector3(0);

        //private Vector3 _currentTargetPos;

        //[DataMemberRange(0, 100)]
        //[Display("Percentage")]
        //public float Percentage
        //{
        //    get { return _percentage; }
        //    set
        //    {
        //        if (SplineComponent != null && SplineComponent.SplineNodeComponents.Count > 1)
        //        {
        //            _currentSplineNodeComponent = SplineComponent.SplineNodeComponents[0];
        //            _nextSplineNodeComponent = SplineComponent.SplineNodeComponents[1];
        //            _previousSplineNodeComponent = null;

        //            var firstNodeWorldPosition = _currentSplineNodeComponent.Entity.Transform.WorldMatrix.TranslationVector;
        //            Entity.Transform.WorldMatrix.TranslationVector = firstNodeWorldPosition;

        //            _currentNodeIndex = 0;
        //            _currentSplinePointIndex = 0;
        //           // SetNextTarget();
        //        }
        //        _percentage = value;
        //    }
        //}
        //private float _percentage = 0f;

        /// <summary>
        /// Event triggered when the last node of the spline has been reached
        /// </summary>
        public delegate void SplineTravellerEndReachedHandler();
        public event SplineTravellerEndReachedHandler OnPathEndReached;

        /// <summary>
        /// Event triggered when a node has been reached. Does not get triggered when the last node of the spline has been reached.
        /// </summary>
        /// <param name="splineNode"></param>
        public delegate void SplineTravellerNodeReachedHandler(SplineNodeComponent splineNode);
        public event SplineTravellerNodeReachedHandler OnNodeReached;

        public SplineTravellerComponent()
        {
        }

        internal void Initialize()
        {
        }

        internal void Update(TransformComponent transformComponent)
        {
            //if (SplineComponent != null && IsMoving) {
            //    var oriPos = Entity.Transform.WorldMatrix.TranslationVector;
            //    Velocity = (_currentTargetPos - oriPos);
            //    Velocity.Normalize() ; 
            //    Velocity *= Speed; //self:GetSpeed(oriPos);

            //    if (!UsePhysics)
            //    {
            //        Velocity *= (float)Game.UpdateTime.Elapsed.TotalSeconds;
            //    }
            //    var movePos = oriPos + Velocity;
            //    var curNode = self.currentNodeEntity.script.node;




                //if self.usePhysics then

                //    self.entityJoint:SetTargetPosition(movePos, 1)

                //else
                //entity:SetPosition(movePos)

                //end


                //if (movePos:DistanceToPoint(_currentTargetPos) < 0.27){
                //    SetNextTarget()
                //}
        
        }

        //public void DetachFromSpline()
        //{
        //    SplineComponent = null;
        //}


        //private void SetNextTarget()
        //{
        //    //if (IsReverseTravelling)
        //    //{
        //    //    if (_splinePointIndex - 1 < 1)
        //    //    {
        //    //        //if the target node(previousNode) has no previousNode himself, we are done
        //    //        //var prevNodeEntity = _currentSplineNodeComponent.script.node.previousNodeEntity
        //    //    }
        //    //}
        //    //if (_previousSplineNodeComponent == null)
        //    //{
        //    //    //P("Previous node == nill. path end")
        //    //    IsMoving = false;
        //    //    //if (OnPathReached != null)
        //    //    //{
        //    //    //    OnPathReached();
        //    //    //}
        //    //    //else
        //    //    //{
        //    //    //P("Previous Node reached")
        //    //    //if (OnNodeReached != null)
        //    //    {
        //    //        //OnNodeReached(prevNodeEntity);

        //    //        GoToPreviousSpline(prevNodeEntity);
        //    //    }
        //    //    else
        //    //    {
        //    //        SetNewTargetPosition();

        //    //        //TODO: rotation
        //    //        //var curNode = currentNodeEntity.script.node
        //    //        //if curNode.rotationMode == 2 ) {
        //    //        //    nodeStartRotation = entity:GetRotation(true)
        //    //        //end
        //    //    }
        //    //}

        //    //next spline point possible
        //    var currentSplinePoints = _currentSplineNodeComponent.GetSplineNode().GetSplinePoints();
        //    if (_currentSplinePointIndex + 1 < currentSplinePoints.Length)
        //    {
        //        SetNewTargetPosition();
        //        _currentSplinePointIndex++;

        //        //if (_currentSplineNodeComponent.rotationMode == 2)
        //        //{
        //        //    nodeStartRotation = entity:GetRotation(true);
        //        //}
        //    }
        //    else
        //    {
        //        //is there a next node?
        //        if (_currentNodeIndex + 1 < SplineComponent.Nodes.Count)
        //        {
        //            OnNodeReached(_nextSplineNodeComponent);
        //            GoToNextSplineNode();
        //        }
        //        else
        //        {
        //            OnPathEndReached();
        //        }
        //    }

        //    //_currentSplineNodeComponent = currentSplinePoints[_currentSplinePointIndex + 1];
        //    //_currentNodeIndex++;
        //    //_currentSplinePointIndex = 0;
        //    ////Get next Node
        //    ////var nextNode = SplineComponent.Nodes _currentSplinePointIndex.script.node.nextNode;
        //    //if (_nextSplineNodeComponent == null)
        //    //{
        //    //    IsMoving = false;
        //    //    //if (OnPadReached != null)
        //    //    //{
        //    //    //    OnPathReached();
        //    //    //}
        //    //    //else
        //    //    //{
        //    //    //    if (OnNodeReached != null)
        //    //    //    {
        //    //    //        OnNodeReached(nextNode);
        //    //    //    }
        //    //    GoToNextSplineNode();
        //    //}
        //}

        //private void SetNewTargetPosition()
        //{
        //    //    if (travelReverse)
        //    //    {
        //    //        //Take the splinepoint that was last visited
        //    //        var prevNode = currentNodeEntity.script.node.previousNodeEntity;
        //    //        splinePointIndex = splinePointIndex - 1;

        //    //        currentTargetPos = prevNode.script.node.splinePoints[splinePointIndex].point;

        //    //else
        //    //            //Take next splinePoint
        //    _currentTargetPos = _currentSplineNodeComponent.GetSplineNode().GetSplinePoints()[_currentSplinePointIndex].position;

        //    //    }
        //}

        //private void GoToNextSplineNode()
        //{
        //    //nodeStartRotation = Entity.Transform.Rotation;//WORLD
        //    //endRotation = nextNode...nextNode.tra(true);
        //    //currentNodeEntity = nextNode;
        //    _currentSplinePointIndex = 0;
        //    _previousSplineNodeComponent = _currentSplineNodeComponent; 
        //    _currentSplineNodeComponent = _nextSplineNodeComponent;
        //    _currentNodeIndex++;
        //    _nextSplineNodeComponent = SplineComponent.Nodes[_currentNodeIndex];
        //    SetNextTarget();
        //}

        //private void GoToPreviousSpline(SplineNodeComponent arrivedAtNode)
        //{
        //    //TODO rotation
        //    //nodeStartRotation = entity:GetRotation(true)
        //    //endRotation = previousNode.script.node.nextNode:GetRotation(true)
        //    _currentSplineNodeComponent = arrivedAtNode;
        //    var targetNode = arrivedAtNode.script.node.previousNodeEntity;
        //    splinePointIndex = #targetNode.script.node.splinePoints ;

        //    SetNextTarget();
        //}
    }
}
