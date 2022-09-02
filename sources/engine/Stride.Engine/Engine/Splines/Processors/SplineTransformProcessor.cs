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
    /// The processor for <see cref="SplineComponent"/>.
    /// </summary>
    public class SplineTransformProcessor : EntityProcessor<SplineComponent, SplineTransformProcessor.SplineTransformationInfo>
    {
        private HashSet<SplineComponent> splineComponentsToUpdate = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SplineTransformProcessor"/> class.
        /// </summary>
        public SplineTransformProcessor()
            : base(typeof(TransformComponent))
        {
        }

        protected override SplineTransformationInfo GenerateComponentData(Entity entity, SplineComponent component)
        {
            return new SplineTransformationInfo
            {
                TransformOperation = new SplineViewHierarchyTransformOperation(component),
            };
        }

        protected override bool IsAssociatedDataValid(Entity entity, SplineComponent component, SplineTransformationInfo associatedData)
        {
            return component == associatedData.TransformOperation.SplineComponent;
        }

        protected override void OnEntityComponentAdding(Entity entity, SplineComponent component, SplineTransformationInfo data)
        {
            component.Spline.OnSplineDirty += () => splineComponentsToUpdate.Add(component);

            splineComponentsToUpdate.Add(component);

            entity.Transform.PostOperations.Add(data.TransformOperation);
        }

        protected override void OnEntityComponentRemoved(Entity entity, SplineComponent component, SplineTransformationInfo data)
        {
            component.Spline.OnSplineDirty -= () => splineComponentsToUpdate.Add(component);

            entity.Transform.PostOperations.Remove(data.TransformOperation);
        }

        public class SplineTransformationInfo
        {
            public SplineViewHierarchyTransformOperation TransformOperation;
        }

        public override void Draw(RenderContext context)
        {
            foreach (var splineComponent in splineComponentsToUpdate)
            {
                if (splineComponent.SplineRenderer.SegmentsMaterial.Passes.Count == 0)
                    return;

                // Allways perform cleanup
                var existingRenderer = splineComponent.Entity.FindChild("SplineRenderer");
                if (existingRenderer != null)
                {
                    splineComponent.Entity.RemoveChild(existingRenderer);
                    SceneInstance.GetCurrent(context)?.Remove(existingRenderer);
                }

                var totalNodesCount = splineComponent.Nodes.Count;

                if (totalNodesCount > 1)
                {
                    splineComponent.Spline.SplineNodes.Clear();
                    for (int i = 0; i < totalNodesCount; i++)
                    {
                        var currentSplineNodeComponent = splineComponent.Nodes[i];

                        if (currentSplineNodeComponent == null)
                            break;

                        // Get all worldpositions
                        currentSplineNodeComponent.Entity.Transform.WorldMatrix.Decompose(out var scale, out Quaternion rotation, out var startTangentOutWorldPosition);
                        currentSplineNodeComponent.SplineNode.WorldPosition = startTangentOutWorldPosition;
                        currentSplineNodeComponent.SplineNode.TangentOutWorldPosition = startTangentOutWorldPosition + currentSplineNodeComponent.SplineNode.TangentOutLocal;
                        currentSplineNodeComponent.SplineNode.TangentInWorldPosition = startTangentOutWorldPosition + currentSplineNodeComponent.SplineNode.TangentInLocal;
                        splineComponent.Spline.SplineNodes.Add(currentSplineNodeComponent.SplineNode);
                    }
                }

                if (splineComponent.Spline.SplineNodes.Count > 1)
                {
                    splineComponent.Spline.RegisterSplineNodeDirtyEvents();

                    splineComponent.Spline.CalculateSpline();

                    // Update spline renderer
                    if (splineComponent.SplineRenderer.Segments || splineComponent.SplineRenderer.BoundingBox)
                    {
                        var graphicsDeviceService = Services.GetService<IGraphicsDeviceService>();
                        var splineDebugEntity = splineComponent.SplineRenderer.Create(splineComponent.Spline, graphicsDeviceService?.GraphicsDevice, splineComponent.Entity);

                        if (splineDebugEntity != null)
                        {
                            splineComponent.Entity.AddChild(splineDebugEntity);
                        }
                    }
                }
            }

            //Now that dirty splines are updated, clear the collection
            splineComponentsToUpdate.Clear();
        }
    }
}
