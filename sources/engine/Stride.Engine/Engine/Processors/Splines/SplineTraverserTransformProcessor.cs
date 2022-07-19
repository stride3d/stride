using System;
using System.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine.Splines;
using Stride.Engine.Splines.Components;
using Stride.Engine.Splines.Models;
using Stride.Games;

namespace Stride.Engine.Processors;

/// <summary>
/// The processor for <see cref="SplineTraverserComponent"/>.
/// </summary>
public class SplineTraverserTransformProcessor : EntityProcessor<SplineTraverserComponent, SplineTraverserTransformProcessor.SplineTraverserTransformationInfo>
{
    private SplineTraverserComponent splineTraverserComponent;
    private Entity entity;

    private SplineNode originSplineNode { get; set; }
    private int currentSplineNodeIndex = 0;
    
    private SplineNode targetSplineNode { get; set; }
    private int targetSplineNodeIndex = 0;

    private bool attachedToSpline = false;
    private int originSplinePointIndex = 0;
    private BezierPoint[] splinePointsToTraverse = null;
    private Vector3 targetSplinePointWorldPosition { get; set; } = new Vector3(0);

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
        originSplineNode = null;
        targetSplineNode = null;
        targetSplinePointWorldPosition = new Vector3(0);
        targetSplineNodeIndex = 0;
        originSplinePointIndex = 0;
        splinePointsToTraverse = null;
    }

    public class SplineTraverserTransformationInfo
    {
        public SplineTraverserViewHierarchyTransformOperation TransformOperation;
    }

    private void CalculateTargets()
    {
        if (entity != null && splineTraverserComponent?.SplineComponent?.Nodes?.Count > 1 && splineTraverserComponent?.SplineComponent?.Spline?.SplineNodes?.Count > 1)
        {
            // A spline traverser should target the closes two spline nodes. 
            // 1. Get the closest point on spline
            // 2. Determine current en target spline node based on traverser speed

            var currentPositionOfTraverser = splineTraverserComponent.Entity.Transform.WorldMatrix.TranslationVector;
            var splinePositionInfo = splineTraverserComponent.SplineComponent.GetClosestPointOnSpline(currentPositionOfTraverser);

            // Are we going backwards?
            if (splineTraverserComponent.Speed < 0)
            {
                originSplineNode = splinePositionInfo.SplineNodeB;
                currentSplineNodeIndex = splinePositionInfo.SplineNodeBIndex;

                targetSplineNode = splinePositionInfo.SplineNodeA;
                targetSplineNodeIndex = splinePositionInfo.SplineNodeAIndex;

                Debug.WriteLine($"currentSplinePointIndex {originSplinePointIndex} -  targetSplineNodeIndex: {targetSplineNodeIndex}");

                 splinePointsToTraverse = targetSplineNode.GetBezierPoints();

                if (splinePointsToTraverse == null)
                {
                    return;
                }

                originSplinePointIndex = splinePointsToTraverse.Length - 1;
                targetSplinePointWorldPosition = splinePointsToTraverse[originSplinePointIndex].Position; //Take the last position at the end of the curve
            }
            else
            {
                originSplineNode = splinePositionInfo.SplineNodeAIndex > splinePositionInfo.SplineNodeBIndex ? splinePositionInfo.SplineNodeB : splinePositionInfo.SplineNodeA;
                currentSplineNodeIndex = splinePositionInfo.SplineNodeAIndex > splinePositionInfo.SplineNodeBIndex ? splinePositionInfo.SplineNodeBIndex: splinePositionInfo.SplineNodeAIndex;

                targetSplineNode = splineTraverserComponent?.SplineComponent?.Nodes[currentSplineNodeIndex + 1].SplineNode;
                targetSplineNodeIndex = splinePositionInfo.SplineNodeAIndex + 1;

                splinePointsToTraverse = originSplineNode.GetBezierPoints();

                if (splinePointsToTraverse == null)
                {
                    return;
                }

                originSplinePointIndex = 1;
                targetSplinePointWorldPosition = splinePointsToTraverse[originSplinePointIndex].Position;
            }

            //if (splineTraverserComponent.SplineComponent.Nodes.Count == splinePositionInfo.CurrentSplineNodeIndex + 1)
            //{
            //    targetCurveIndex = splinePositionInfo.CurrentSplineNodeIndex + 1;
            //}
            //else
            //{
            //    targetCurveIndex = 0;
            //}

            Debug.WriteLine("Instance Added: " + targetSplinePointWorldPosition );
            attachedToSpline = true;
            splineTraverserComponent.Dirty = false;

        }
    }

    public override void Update(GameTime time)
    {
        if (splineTraverserComponent?.SplineComponent == null)
            return;

        if (splineTraverserComponent.Dirty || !attachedToSpline)
        {
            CalculateTargets();
        }

        if (splineTraverserComponent.IsMoving && splineTraverserComponent.Speed != 0 && attachedToSpline)
        {
            var entityWorldPosition = entity.Transform.WorldMatrix.TranslationVector;
            var velocity = (targetSplinePointWorldPosition - entityWorldPosition);
            velocity.Normalize();
            velocity *= Math.Abs(splineTraverserComponent.Speed) * (float)time.Elapsed.TotalSeconds;

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

        // Are we going backwards?
        if (splineTraverserComponent.Speed < 0)
        {
            // Is there a previous curve point?
            if (originSplinePointIndex - 1 >= 0)
            {
                originSplinePointIndex--;
                targetSplinePointWorldPosition = splinePointsToTraverse[originSplinePointIndex].Position;
            }
            else
            {
                // Is there a next Spline node?
                if (targetSplineNodeIndex - 1 >= 0 || splineTraverserComponent.SplineComponent.Spline.Loop)
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
        else // We are going forward
        {

            // Is there a next point on the current spline?
            if (originSplinePointIndex + 1 < splinePointsToTraverse.Length)
            {
                originSplinePointIndex++;
                targetSplinePointWorldPosition = splinePointsToTraverse[originSplinePointIndex].Position;
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

    }

    private void GoToNextSplineNode(int nodesCount)
    {
        // Are we going backwards?
        if (splineTraverserComponent.Speed < 0)
        {
            originSplineNode = targetSplineNode;
           
            targetSplineNodeIndex--;
            if (targetSplineNodeIndex >= 0)
            {
                targetSplineNode = splineTraverserComponent.SplineComponent.Nodes[targetSplineNodeIndex].SplineNode;
            }
            else if (targetSplineNodeIndex < 0 && splineTraverserComponent.SplineComponent.Spline.Loop)
            {
                targetSplineNodeIndex = nodesCount - 1;
                targetSplineNode = splineTraverserComponent.SplineComponent.Nodes[targetSplineNodeIndex].SplineNode;
            }

            splinePointsToTraverse = targetSplineNode.GetBezierPoints();
            originSplinePointIndex = splinePointsToTraverse.Length - 1;

        }
        else // We are going forwards?
        {
            originSplinePointIndex = 0;
            originSplineNode = targetSplineNode;
            splinePointsToTraverse = originSplineNode.GetBezierPoints();

            targetSplineNodeIndex++;
            if (targetSplineNodeIndex < nodesCount)
            {
                targetSplineNode = splineTraverserComponent.SplineComponent.Nodes[targetSplineNodeIndex].SplineNode;
            }
            else if (targetSplineNodeIndex == nodesCount && splineTraverserComponent.SplineComponent.Spline.Loop)
            {
                targetSplineNode = splineTraverserComponent.SplineComponent.Nodes[0].SplineNode;
                targetSplineNodeIndex = 0;
            }
        }

        SetNextTarget();
    }
}
