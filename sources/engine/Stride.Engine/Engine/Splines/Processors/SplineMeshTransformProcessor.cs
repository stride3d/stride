//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine.Splines.Components;
using Stride.Engine.Splines.HierarchyTransformOperations;
using Stride.Engine.Splines.Models;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.Engine.Splines.Processors
{
    /// <summary>
    /// The processor for <see cref="SplineMeshComponent"/>.
    /// </summary>
    public class SplineMeshTransformProcessor : EntityProcessor<SplineMeshComponent, SplineMeshTransformProcessor.SplineMeshTransformationInfo>
    {
        private HashSet<SplineMeshComponent> splineMeshComponentsToUpdate = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SplineMeshTransformProcessor"/> class.
        /// </summary>
        public SplineMeshTransformProcessor()
            : base(typeof(TransformComponent))
        {
        }

        protected override SplineMeshTransformationInfo GenerateComponentData(Entity entity, SplineMeshComponent component)
        {
            return new SplineMeshTransformationInfo
            {
                TransformOperation = new SplineMeshViewHierarchyTransformOperation(component),
            };
        }

        protected override bool IsAssociatedDataValid(Entity entity, SplineMeshComponent component, SplineMeshTransformationInfo associatedData)
        {
            return component == associatedData.TransformOperation.SplineMeshComponent;
        }

        protected override void OnEntityComponentAdding(Entity entity, SplineMeshComponent component, SplineMeshTransformationInfo data)
        {
            component.OnMeshRequiresUpdate += EnqueueMeshComponentForUpdate;
            if(component.SplineComponent != null && component.SplineMesh == null)
            {
                EnqueueMeshComponentForUpdate(component);
            }

            entity.Transform.PostOperations.Add(data.TransformOperation);
        }

        private void EnqueueMeshComponentForUpdate(SplineMeshComponent component)
        {
            splineMeshComponentsToUpdate.Add(component);
        }

        protected override void OnEntityComponentRemoved(Entity entity, SplineMeshComponent component, SplineMeshTransformationInfo data)
        {
            component.OnMeshRequiresUpdate -= EnqueueMeshComponentForUpdate;
            entity.Transform.PostOperations.Remove(data.TransformOperation);
        }

        public class SplineMeshTransformationInfo
        {
            public SplineMeshViewHierarchyTransformOperation TransformOperation;
        }

        public struct TempMesh
        {

        }

        public override void Draw(RenderContext context)
        {
            foreach (var splineMeshComponent in splineMeshComponentsToUpdate)
            {
                if (splineMeshComponent.SplineComponent == null)
                {
                    continue;
                }

                //Delete existing model first
                var generatedSplineMeshEntity = splineMeshComponent.Entity.FindChild("GeneratedSplineMeshEntity");
                if (generatedSplineMeshEntity != null)
                {
                    splineMeshComponent.Entity.RemoveChild(generatedSplineMeshEntity);
                    SceneInstance.GetCurrent(context)?.Remove(generatedSplineMeshEntity);
                }

                //Gather all spline bezierpoints in to mesh data set
                var totalNodesCount = splineMeshComponent.SplineComponent.Spline.SplineNodes.Count;
                List<BezierPoint> splineBezierPoints = new List<BezierPoint>();
                Vector3 originWorldPos = Vector3.Zero;
                for (int i = 0; i < totalNodesCount; i++)
                {
                   
                    var currentSplineNodeComponent = splineMeshComponent.SplineComponent.Nodes[i];
                    if (currentSplineNodeComponent == null)
                    {
                        break;
                    }
                    var bezierPoints = currentSplineNodeComponent.GetBezierCurvePoints();
                    if (bezierPoints == null)
                    {
                        break;
                    }


                    if (i == 0)
                    {
                        originWorldPos = bezierPoints[0].Position;
                        splineBezierPoints.AddRange(bezierPoints);
                    }
                    else if (i < totalNodesCount - 1 || i == totalNodesCount-1 && splineMeshComponent.SplineComponent.Loop)
                    {
                        var lastPositionOnSpline = splineBezierPoints[splineBezierPoints.Count - 1].Position;
                        splineBezierPoints[0].DistanceToPreviousPoint = Vector3.Distance(splineBezierPoints[0].Position, lastPositionOnSpline);
                        splineBezierPoints.AddRange(bezierPoints[1..(bezierPoints.Length-1)]);
                    }
                }

                //Create a mode and generate its mesh
                var model = new Model();
                //splineMeshComponent.SplineMesh.LocalOffset = originWorldPos;
                splineMeshComponent.SplineMesh.bezierPoints = splineBezierPoints.ToArray();
                splineMeshComponent.SplineMesh.Loop = splineMeshComponent.SplineComponent.Loop;
                splineMeshComponent.SplineMesh.Generate(Services, model);

                //Create a new entity, with a model component which holds the generated splinemesh
                generatedSplineMeshEntity = new Entity("GeneratedSplineMeshEntity");
                generatedSplineMeshEntity.Add(new ModelComponent(model));
                generatedSplineMeshEntity.Transform.Position -= splineMeshComponent.Entity.Transform.Position;
                //generatedSplineMeshEntity.Transform.Position -= splineMeshComponent.Entity.Transform.WorldMatrix.TranslationVector - originWorldPos;

                // Add the generated spline mesh entity as a child of the component's entity
                splineMeshComponent.Entity.AddChild(generatedSplineMeshEntity);
            }

            //Now that dirty splines meshes are updated, clear the collection
            splineMeshComponentsToUpdate.Clear();
        }
    }
}
