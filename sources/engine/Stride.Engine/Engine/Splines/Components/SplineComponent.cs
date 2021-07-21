using System.Collections.Generic;
using Stride.Core;
using Stride.Engine.Design;
using Stride.Engine.Processors;

namespace Stride.Engine.Splines.Components
{
    /// <summary>
    /// Component representing an Spline.
    /// </summary>
    [DataContract("SplineComponent")]
    [Display("Spline", Expand = ExpandRule.Once)]
    [DefaultEntityComponentProcessor(typeof(SplineTransformProcessor))]
    [ComponentCategory("Splines")]
    public sealed class SplineComponent : EntityComponent
    {
        public SplineDebugInfo DebugInfo;
        public bool Dirty { get; set; }
        public float distanceTest;

        private Scene _editorScene;


        private int _previousNodeCount = 0;

        private List<SplineNodeComponent> _nodes;
        public List<SplineNodeComponent> Nodes
        {
            get
            {
                if (_nodes == null)
                {
                    _nodes = new List<SplineNodeComponent>();
                }
                return _nodes;
            }
            set
            {
                _nodes = value;
            }
        }

        public SplineComponent()
        {
            _previousNodeCount = 0;
            Nodes = new List<SplineNodeComponent>();
        }

        internal void Initialize()
        {
            UpdateSpline();
        }

        internal void Update(TransformComponent transformComponent)
        {
            int currentNodeCount = Nodes.Count;
            if (_previousNodeCount != currentNodeCount)
            {
                DeregisterSplineNodeDirtyEvents();
                UpdateSpline();
            }
            else
            {
                if (Dirty)
                {
                    UpdateSpline();
                }
            }

            _previousNodeCount = currentNodeCount;
        }

        private void DeregisterSplineNodeDirtyEvents()
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                var curNode = Nodes[i];
                if (curNode != null)
                {
                    curNode.OnDirty -= MakeSplineDirty;
                }
            }
        }

        public void UpdateSpline()
        {
            MakeSplineDirty();
            if (Nodes.Count > 1)
            {
                var totalNodesCount = Nodes.Count;

                for (int i = 0; i < totalNodesCount; i++)
                {
                    var curNode = Nodes[i];

                    if (curNode == null)
                        break;

                    if (i < totalNodesCount - 1)
                        curNode?.UpdateBezierCurve(Nodes[i + 1]);

                    curNode.OnDirty += MakeSplineDirty;
                }
            }
            distanceTest = GetTotalSplineDistance();
        }

        private void MakeSplineDirty()
        {
            Dirty = true;
        }

        public float GetTotalSplineDistance()
        {
            float distance = 0;
            foreach (var node in Nodes)
            {
                if (node != null)
                {
                    var curve = node.GetBezierCurve();
                    if (curve != null)
                        distance += curve.Distance;
                }
            }
            return distance;
        }

        ////Button to create new spline node
        //private bool _createSplineNode;
        //public bool CreateSplineNode
        //{
        //    get
        //    {
        //        return _createSplineNode;
        //    }
        //    set
        //    {
        //        _createSplineNode = value;
        //        if (_createSplineNode)
        //        {
        //            CreateSplineNodeEntity();
        //        }
        //    }
        //}

        //public void CreateSplineNodeEntity()
        //{
        //    var nodesCount = spline.BezierCurves.Count;
        //    var entityName = "Node_" + nodesCount;
        //    var startPos = nodesCount > 0 ? spline.BezierCurves[nodesCount - 1].TargetNode.Position : Entity.Transform.Position;
        //    var newSplineNode = new Entity(startPos, entityName);
        //    var newSplineNodeComponent = newSplineNode.GetOrCreate<SplineNodeComponent>();
        //    //SplineNodeComponents.Add(newSplineNodeComponent);
        //    //SceneSystem.SceneInstance.RootScene.Entities.Add(newSplineNode);
        //    //_editorScene?.Entities.Add(newSplineNode);

        //    //Entity.Scene.Entities.Add(entity);
        //}


        //public float GetNodeLinkDistance()
        //{
        //    float distance = 0;
        //    foreach (var node in SplineNodeComponents)
        //    {
        //        distance += node.GetSplineNode().NodeLinkDistance;
        //    }
        //    return distance;
        //}

        //public ClosestPointInfo GetClosestPointOnSpline(Vector3 otherPosition)
        //{
        //    ClosestPointInfo closestPointInfo = null;

        //    var totalNodesCount = SplineNodeComponents.Count;
        //    for (int i = 0; i < totalNodesCount - 1; i++)
        //    {

        //        //for key, nodeEntity in ipairs(self.nodeEntities) do
        //        //        local nextNodeEntity = nodeEntity.script.nextNodeEntity
        //        //    if nextNodeEntity ~= nil then
        //        //        local closestPointInfoTemp = nodeEntity.script.node:GetClosestPointOnNodeCurve(otherPosition)
        //        //        local dist = closestPointInfoTemp.closestPoint:DistanceToPoint(otherPosition)

        //        //        if shortestDistance == nil or shortestDistance > dist then

        //        //            shortestDistance = dist
        //        //            closestPointInfo = closestPointInfoTemp
        //        //        end
        //        //    else
        //        //    break
        //        //    end
        //        //end
        //        //closestPointInfo.distance = closestPointInfo.closestPoint:DistanceToPoint(otherPosition)
        //        //closestPointInfo.Distance = closestPointInfo.ClosestPoint:
        //    }

        //    return closestPointInfo;
        //}

        //public class ClosestPointInfo
        //{
        //    public float Distance = 0;
        //    public Vector3 ClosestPoint;
        //}
    }
}
