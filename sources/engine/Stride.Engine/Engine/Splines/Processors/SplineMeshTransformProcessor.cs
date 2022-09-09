//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine.Splines.Components;
using Stride.Engine.Splines.HierarchyTransformOperations;
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
            component.SplineComponent.Spline.OnSplineDirty += () => splineMeshComponentsToUpdate.Add(component);

            splineMeshComponentsToUpdate.Add(component);

            entity.Transform.PostOperations.Add(data.TransformOperation);
        }

        protected override void OnEntityComponentRemoved(Entity entity, SplineMeshComponent component, SplineMeshTransformationInfo data)
        {
            component.SplineComponent.Spline.OnSplineDirty -= () => splineMeshComponentsToUpdate.Add(component);

            entity.Transform.PostOperations.Remove(data.TransformOperation);
        }

        public class SplineMeshTransformationInfo
        {
            public SplineMeshViewHierarchyTransformOperation TransformOperation;
        }

        public override void Draw(RenderContext context)
        {
            foreach (var splineMeshComponent in splineMeshComponentsToUpdate)
            {
                // Allways perform cleanup
                //var existingRenderer = splineMeshComponent.Entity.FindChild("SplineRenderer");
                //if (existingRenderer != null)
                //{
                //    splineMeshComponent.Entity.RemoveChild(existingRenderer);
                //    SceneInstance.GetCurrent(context)?.Remove(existingRenderer);
                //}


                var children = splineMeshComponent.Entity.GetChildren();
                foreach (var child in children)
                {
                    splineMeshComponent.Entity.RemoveChild(child);
                }

                var totalNodesCount = splineMeshComponent.SplineComponent.Spline.SplineNodes.Count;
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

                    for (int j = 0; j < bezierPoints.Length - 1; j++)
                    {
                        var meshEntity = new Entity();
                        var model = new Model();
                        var modelComponent = new ModelComponent(model); 
                        var currentSplinePoint = bezierPoints[j];
                        var nextSplinePoint = bezierPoints[j+1];

                        //// Generate the procedual model
                        splineMeshComponent.SplineMesh.LocalOffset = currentSplinePoint.Position;
                        splineMeshComponent.SplineMesh.TargetOffset = nextSplinePoint.Position;

                        splineMeshComponent.SplineMesh.Generate(Services, model);

                        //// Add everything to the entity
                        meshEntity.Add(modelComponent);
                        splineMeshComponent.Entity.AddChild(meshEntity);
                    }
                }
            }

            //Now that dirty splines meshes are updated, clear the collection
            splineMeshComponentsToUpdate.Clear();
        }
    }
}
