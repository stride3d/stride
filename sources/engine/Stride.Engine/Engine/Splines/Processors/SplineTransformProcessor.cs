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
        private SplineBuilder splineBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="SplineTransformProcessor"/> class.
        /// </summary>
        public SplineTransformProcessor()
            : base(typeof(TransformComponent))
        {
            splineBuilder = new SplineBuilder();
        }

        protected override SplineTransformationInfo GenerateComponentData(Entity entity, SplineComponent component)
        {
            var transformationInfo = new SplineTransformationInfo
            {
                TransformOperation = new SplineViewHierarchyTransformOperation(component),
            };

            // Assign the SplineProcessor property
            transformationInfo.SplineProcessor = this;

            return transformationInfo;
        }

        protected override bool IsAssociatedDataValid(Entity entity, SplineComponent component, SplineTransformationInfo associatedData)
        {
            return component == associatedData.TransformOperation.SplineComponent;
        }

        protected override void OnEntityComponentAdding(Entity entity, SplineComponent component, SplineTransformationInfo data)
        {
            // Subscribe to the event using the Update method with the component as a delegate
            component.Spline.OnSplineDirty += () => data.Update(component);

            // Add the component to the list
            splineComponentsToUpdate.Add(component);

            // Add the transformation operation to the entity
            entity.Transform.PostOperations.Add(data.TransformOperation);
        }

        protected override void OnEntityComponentRemoved(Entity entity, SplineComponent component, SplineTransformationInfo data)
        {
            // Unsubscribe from the event using the Update method

            //TODO: this does not work properly atm
            component.Spline.OnSplineDirty -= () => data.Update(component);

            // Remove the transformation operation from the entity
            entity.Transform.PostOperations.Remove(data.TransformOperation);
        }

        public class SplineTransformationInfo
        {
            public SplineViewHierarchyTransformOperation TransformOperation;
            public SplineTransformProcessor SplineProcessor;
            

            //TODO wip
            public void Update(SplineComponent component)
            {
                SplineProcessor.splineComponentsToUpdate.Add(component);
            }
        }

        public override void Draw(RenderContext context)
        {
            foreach (var splineComponent in splineComponentsToUpdate)
            {
                if (splineComponent.SplineRenderer.SegmentsMaterial == null || splineComponent.SplineRenderer.SegmentsMaterial.Passes.Count == 0)
                    return;

                // Always perform cleanup
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

                        // Get all world positions
                        currentSplineNodeComponent.Entity.Transform.WorldMatrix.Decompose(out var scale, out Quaternion rotation, out var startTangentOutWorldPosition);
                        currentSplineNodeComponent.SplineNode.WorldPosition = startTangentOutWorldPosition;
                        currentSplineNodeComponent.SplineNode.TangentOutWorldPosition = startTangentOutWorldPosition + currentSplineNodeComponent.SplineNode.TangentOutLocal;
                        currentSplineNodeComponent.SplineNode.TangentInWorldPosition = startTangentOutWorldPosition + currentSplineNodeComponent.SplineNode.TangentInLocal;
                        splineComponent.Spline.SplineNodes.Add(currentSplineNodeComponent.SplineNode);
                    }
                }

                if (splineComponent.Spline.SplineNodes.Count <= 1)
                {
                    continue;
                }

                splineComponent.Spline.RegisterSplineNodeDirtyEvents();

                splineBuilder.CalculateSpline(splineComponent.Spline);

                if (!splineComponent.SplineRenderer.Segments && !splineComponent.SplineRenderer.BoundingBox)
                {
                    continue;
                }

                // Update spline renderer
                var graphicsDeviceService = Services.GetService<IGraphicsDeviceService>();
                var splineDebugEntity = splineComponent.SplineRenderer.Create(splineComponent.Spline, graphicsDeviceService?.GraphicsDevice, splineComponent.Entity);

                if (splineDebugEntity != null)
                {
                    splineComponent.Entity.AddChild(splineDebugEntity);
                }
            }

            //Now that dirty splines are updated, clear the collection
            splineComponentsToUpdate.Clear();
        }
    }
}
