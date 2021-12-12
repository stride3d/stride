using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine.Splines.Components;

namespace Stride.Engine.Splines
{

    [DataContract]
    public class Spline
    {
        /// <summary>
        /// Event triggered when the spline has been update
        /// This happens when the entity is translated or rotated, or when a SplineNode is updated
        /// </summary>
        public delegate void SplineUpdatedHandler();
        public event SplineUpdatedHandler OnSplineUpdated;

        private List<SplineNode> _splineNodes;
        private SplineDebugInfo _debugInfo = new();

        [DataMemberIgnore]
        public List<SplineNode> splineNodes
        {
            get
            {
                _splineNodes ??= new List<SplineNode>();
                return _splineNodes;
            }
            set
            {
                _splineNodes = value;
                DeregisterSplineNodeDirtyEvents();
                RegisterSplineNodeDirtyEvents();

                Dirty = true;
            }
        }

        [DataMemberIgnore]
        public bool Dirty { get; set; }

        public float TotalSplineDistance { get; internal set; }

        private bool loop;
        [Display(60, "Loop")]
        public bool Loop
        {
            get
            {
                return loop;
            }
            set
            {
                loop = value;
                Dirty = true;
            }
        }

        [Display(80, "Debug settings")]
        public SplineDebugInfo DebugInfo
        {
            get
            {
                return _debugInfo;
            }
            set
            {
                _debugInfo = value;
                Dirty = true;
            }
        }

        public void UpdateSpline()
        {
            if (Dirty)
            {
                var totalNodesCount = splineNodes.Count;
                if (splineNodes.Count > 1)
                {

                    for (int i = 0; i < totalNodesCount; i++)
                    {
                        var currentSplineNode = splineNodes[i];
                        if (splineNodes[i] == null)
                            break;

                        if (i < totalNodesCount - 1)
                        {
                            var nextSplineNode = splineNodes[i + 1];
                            if (nextSplineNode == null)
                                break;

                            currentSplineNode.TargetWorldPosition = nextSplineNode.WorldPosition;
                            currentSplineNode.TargetTangentInWorldPosition = nextSplineNode.TangentInWorldPosition;

                            splineNodes[i].CalculateBezierCurve();
                        }
                        else if (i == totalNodesCount - 1 && Loop)
                        {
                            var firstSplineNode = splineNodes[0];
                            currentSplineNode.TargetWorldPosition = firstSplineNode.WorldPosition;
                            currentSplineNode.TargetTangentInWorldPosition = firstSplineNode.TangentInWorldPosition;

                            splineNodes[i].CalculateBezierCurve();
                        }
                    }
                }

                TotalSplineDistance = GetTotalSplineDistance();

                OnSplineUpdated?.Invoke();
            }
        }

        public float GetTotalSplineDistance()
        {
            float distance = 0;
            for (int i = 0; i < splineNodes.Count; i++)
            {
                var curve = splineNodes[i];
                if (curve != null)
                {
                    if (Loop || (!Loop && i < splineNodes.Count - 1))
                    {
                        distance += curve.Distance;
                    }
                }
            }

            return distance;
        }


        //public SplinePositionInfo GetPositionOnSpline(float percentage)
        //{
        //    var splinePositionInfo = new SplinePositionInfo();
        //    var totalSplineDistance = GetTotalSplineDistance();
        //    if (totalSplineDistance <= 0)
        //        return splinePositionInfo;

        //    var requiredDistance = totalSplineDistance * (percentage / 100);
        //    var nextNodeDistance = 0.0f;
        //    var prevNodeDistance = 0.0f;

        //    for (int i = 0; i < BezierCurves.Count; i++)
        //    {
        //        var curve = BezierCurves[i];
        //        splinePositionInfo.CurrentSplineNodeIndex = i;
        //        splinePositionInfo.CurrentSplineNode = curve;

        //        var curve = curve.GetBezierCurve();
        //        nextNodeDistance += curve.Distance;

        //        if (requiredDistance < nextNodeDistance)
        //        {
        //            var targetIndex = (i == _splineNodes.Count - 1) ? 0 : i;
        //            splinePositionInfo.TargetSplineNode = _splineNodes[targetIndex];

        //            var percentageInCurve = ((requiredDistance - prevNodeDistance) / (nextNodeDistance - prevNodeDistance)) * 100;
        //            //inverse lerp(betweenValue - minHeight) / (maxHeight - minHeight);

        //            splinePositionInfo.Position = curve.GetPositionOnCurve(percentageInCurve);
        //            return splinePositionInfo;
        //        }

        //        prevNodeDistance = nextNodeDistance;
        //    }

        //    splinePositionInfo.Position = _splineNodes[_splineNodes.Count - 2].GetBezierCurve().TargetPosition;

        //    return splinePositionInfo;
        //}

        public void DeregisterSplineNodeDirtyEvents()
        {
            for (int i = 0; i < splineNodes?.Count; i++)
            {
                var splineNode = splineNodes[i];
                if (splineNode != null)
                {
                    splineNode.OnSplineNodeDirty -= MakeSplineDirty;
                }
            }
        }

        public void RegisterSplineNodeDirtyEvents()
        {
            for (int i = 0; i < splineNodes?.Count; i++)
            {
                var splineNode = splineNodes[i];
                if (splineNode != null)
                {
                    splineNode.OnSplineNodeDirty += MakeSplineDirty;
                }
            }
        }

        private void MakeSplineDirty()
        {
            Dirty = true;
        }

        //public struct SplinePositionInfo
        //{
        //    public SplineNodeComponent CurrentSplineNode { get; set; }
        //    public SplineNodeComponent TargetSplineNode { get; set; }
        //    public Vector3 Position { get; set; }
        //    public int CurrentSplineNodeIndex { get; internal set; }
        //}
    }
}
