using Stride.Engine.Splines;
using Stride.Engine.Splines.Components;
using Stride.Core.Mathematics;
using Stride.Games;
using System.Collections.Generic;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.Engine.Processors
{
    /// <summary>
    /// The processor for <see cref="SplineComponent"/>.
    /// </summary>
    public class SplineTransformProcessor : EntityProcessor<SplineComponent, SplineTransformProcessor.SplineTransformationInfo>
    {

        private SplineComponent SplineComponent;


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
            // Register model view hierarchy update
            SplineComponent = component;

            entity.Transform.PostOperations.Add(data.TransformOperation);
        }

        protected override void OnEntityComponentRemoved(Entity entity, SplineComponent component, SplineTransformationInfo data)
        {
            // Unregister model view hierarchy update
            entity.Transform.PostOperations.Remove(data.TransformOperation);
        }

        public class SplineTransformationInfo
        {
            public SplineViewHierarchyTransformOperation TransformOperation;
        }

        public override void Draw(RenderContext context)
        {
            if (!SplineComponent.Spline.Dirty)
            {
                return;
            }

            // Allways perform cleanup
            var existingRenderer = SplineComponent.Entity.FindChild("SplineRenderer");
            if (existingRenderer != null)
            {
                SplineComponent.Entity.RemoveChild(existingRenderer);
                SceneInstance.GetCurrent(context)?.Remove(existingRenderer);
            }

            var totalNodesCount = SplineComponent.Nodes.Count;

            if (totalNodesCount > 1)
            {
                SplineComponent.Spline.SplineNodes.Clear();
                for (int i = 0; i < totalNodesCount; i++)
                {
                    var currentSplineNodeComponent = SplineComponent.Nodes[i];

                    if (currentSplineNodeComponent == null)
                        break;

                    // Get all worldpositions
                    currentSplineNodeComponent.Entity.Transform.WorldMatrix.Decompose(out var scale, out Quaternion rotation, out var startTangentOutWorldPosition);
                    currentSplineNodeComponent.SplineNode.WorldPosition = startTangentOutWorldPosition;
                    currentSplineNodeComponent.SplineNode.TangentOutWorldPosition = startTangentOutWorldPosition + currentSplineNodeComponent.SplineNode.TangentOutLocal;
                    currentSplineNodeComponent.SplineNode.TangentInWorldPosition = startTangentOutWorldPosition + currentSplineNodeComponent.SplineNode.TangentInLocal;
                    SplineComponent.Spline.SplineNodes.Add(currentSplineNodeComponent.SplineNode);
                }
            }

            if (SplineComponent.Spline.SplineNodes.Count > 1)
            {
                SplineComponent.Spline.RegisterSplineNodeDirtyEvents();

                SplineComponent.Spline.UpdateSpline();

                // Update spline renderer
                if (SplineComponent.SplineRenderer.Segments || SplineComponent.SplineRenderer.BoundingBox)
                {
                    var graphicsDeviceService = Services.GetService<IGraphicsDeviceService>();
                    var splineDebugEntity = SplineComponent.SplineRenderer.Create(SplineComponent.Spline, graphicsDeviceService?.GraphicsDevice, SplineComponent.Entity.Transform.Position);

                    if (splineDebugEntity != null)
                    {
                        SplineComponent.Entity.AddChild(splineDebugEntity);
                    }
                }
            }
        }
    }
}
