using System.Collections.Generic;
using Stride.Core;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Core.Mathematics;

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
        private List<SplineNodeComponent> _splineNodes;
        private Vector3 previousPosition;

        /// <summary>
        /// Event triggered when the spline has been update
        /// This happens when the entity is translated or rotated, or when a SplineNode is updated
        /// </summary>
        public delegate void SplineUpdatedHandler();
        public event SplineUpdatedHandler OnSplineUpdated;

        [Display(100, "Nodes")]
        public List<SplineNodeComponent> Nodes
        {
            get
            {
                _splineNodes ??= new List<SplineNodeComponent>();
                return _splineNodes;
            }
            set
            {
                _splineNodes = value;

                DeregisterSplineNodeDirtyEvents();
                UpdateSpline();
                RegisterSplineNodeDirtyEvents();
            }
        }

        [Display(80, "Debug settings")]
        public SplineDebugInfo DebugInfo;

        [Display(70, "Dirty")]
        public bool Dirty { get; set; }

        private bool loop;
        [Display(80, "Loop")]
        public bool Loop
        {
            get
            {
                return loop;
            }
            set
            {
                loop = value;
                UpdateSpline();
            }
        }

        public float TotalSplineDistance { get; internal set; }

        public SplineComponent()
        {
            Nodes ??= new List<SplineNodeComponent>();
        }

        internal void Initialize()
        {
            UpdateSpline();
        }

        internal void Update(TransformComponent transformComponent)
        {
            if (previousPosition.X != Entity.Transform.Position.X || previousPosition.Y != Entity.Transform.Position.Y || previousPosition.Z != Entity.Transform.Position.Z)
            {
                Dirty = true;
                previousPosition = Entity.Transform.Position;
            }

            if (Dirty)
            {
                UpdateSpline();
            }
        }

        public SplinePositionInfo GetPositionOnSpline(float percentage)
        {
            var splinePositionInfo = new SplinePositionInfo();
            var totalSplineDistance = GetTotalSplineDistance();
            if (totalSplineDistance <= 0)
                return splinePositionInfo;

            var requiredDistance = totalSplineDistance * (percentage / 100);
            var nextNodeDistance = 0.0f;
            var prevNodeDistance = 0.0f;

            for (int i = 0; i < _splineNodes.Count - 1; i++)
            {
                var node = _splineNodes[i];
                splinePositionInfo.CurrentSplineNode = node;
                var curve = node.GetBezierCurve();
                nextNodeDistance += curve.Distance;
                if (requiredDistance < nextNodeDistance)
                {
                    splinePositionInfo.TargetSplineNode = _splineNodes[i + 1];

                    var percentageInCurve = ((requiredDistance - prevNodeDistance) / (nextNodeDistance - prevNodeDistance)) * 100;
                    //inverse lerp(betweenValue - minHeight) / (maxHeight - minHeight);

                    splinePositionInfo.Position = curve.GetPositionOnCurve(percentageInCurve);
                    return splinePositionInfo;
                }

                prevNodeDistance = nextNodeDistance;
            }

            splinePositionInfo.Position = _splineNodes[_splineNodes.Count - 2].GetBezierCurve().TargetPosition;

            return splinePositionInfo;
        }

        public struct SplinePositionInfo
        {
            public SplineNodeComponent CurrentSplineNode { get; set; }
            public SplineNodeComponent TargetSplineNode { get; set; }
            public Vector3 Position { get; set; }
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

        private void RegisterSplineNodeDirtyEvents()
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                var curNode = Nodes[i];
                if (curNode != null)
                {
                    curNode.OnDirty += MakeSplineDirty;
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
                    {
                        curNode?.UpdateBezierCurve(Nodes[i + 1]);
                    }
                    else if (i == totalNodesCount - 1 && Loop)
                    {
                        curNode?.UpdateBezierCurve(Nodes[0]);
                    }
                }
            }

            TotalSplineDistance = GetTotalSplineDistance();

            OnSplineUpdated?.Invoke();
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
