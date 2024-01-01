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
    public class SplineProcessor : EntityProcessor<SplineComponent, SplineProcessor.SplineTransformationInfo>
    {
        private HashSet<SplineComponent> splineComponentsToUpdate = new();
        private SplineBuilder splineBuilder;
        private SplineRenderer SplineRenderer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SplineProcessor"/> class.
        /// </summary>
        public SplineProcessor()
            : base(typeof(TransformComponent))
        {
            splineBuilder = new SplineBuilder();
            SplineRenderer = new SplineRenderer();
        }

        protected override SplineTransformationInfo GenerateComponentData(Entity entity, SplineComponent component)
        {
            var transformationInfo = new SplineTransformationInfo { TransformOperation = new SplineViewHierarchyTransformOperation(component), };

            return transformationInfo;
        }

        protected override bool IsAssociatedDataValid(Entity entity, SplineComponent component, SplineTransformationInfo associatedData)
        {
            return component == associatedData.TransformOperation.SplineComponent;
        }

        protected override void OnEntityComponentAdding(Entity entity, SplineComponent component, SplineTransformationInfo data)
        {
            component.Spline.OnSplineDirty += () => data.Update(this, component);
            splineComponentsToUpdate.Add(component);
            entity.Transform.PostOperations.Add(data.TransformOperation);
        }

        protected override void OnEntityComponentRemoved(Entity entity, SplineComponent component, SplineTransformationInfo data)
        {
            component.Spline.OnSplineDirty -= () => data.Update(this, component);
            entity.Transform.PostOperations.Remove(data.TransformOperation);
        }

        public class SplineTransformationInfo
        {
            public SplineViewHierarchyTransformOperation TransformOperation;

            public void Update(SplineProcessor processor, SplineComponent component)
            {
                processor.splineComponentsToUpdate.Add(component);
            }
        }

        public override void Draw(RenderContext context)
        {
            foreach (var splineComponent in splineComponentsToUpdate)
            {
                if (splineComponent.RenderSettings.SegmentsMaterial == null || splineComponent.RenderSettings.SegmentsMaterial.Passes.Count == 0)
                    return;


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

                if (!splineComponent.RenderSettings.ShowSegments && !splineComponent.RenderSettings.ShowBoundingBox)
                {
                    continue;
                }

                // Update spline renderer

                var graphicsDeviceService = Services.GetService<IGraphicsDeviceService>();
                var existingRendererEntity = splineComponent.Entity.FindChild("SplineRenderer");
                var splineMeshEntity = SplineRenderer.Create(existingRendererEntity, splineComponent.Spline, splineComponent.RenderSettings, graphicsDeviceService?.GraphicsDevice,
                    splineComponent.Entity);

                if (splineMeshEntity != null && existingRendererEntity == null)
                {
                    splineComponent.Entity.AddChild(splineMeshEntity);
                }
            }

            //Now that dirty splines are updated, clear the collection
            splineComponentsToUpdate.Clear();
        }
    }
}
