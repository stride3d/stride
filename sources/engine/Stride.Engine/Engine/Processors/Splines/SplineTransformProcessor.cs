//// Copyright (c) Stride contributors (https://Stride.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine.Splines;
using Stride.Engine.Splines.Components;
using Stride.Rendering;

using Stride.Core.Mathematics;
using Stride.Games;
using System.Collections.Generic;
using Stride.Graphics;

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

        public override void Update(GameTime time)
        {
            if (!SplineComponent.Spline.Dirty)
            {
                return;
            }

            var updatedSplineNodes = new List<SplineNode>();
            var totalNodesCount = SplineComponent.SplineNodesComponents.Count;

            if (totalNodesCount > 1)
            {
                for (int i = 0; i < totalNodesCount; i++)
                {
                    var currentSplineNodeComponent = SplineComponent.SplineNodesComponents[i];

                    if (currentSplineNodeComponent == null)
                        break;

                    currentSplineNodeComponent.Entity.Transform.WorldMatrix.Decompose(out var scale, out Quaternion rotation, out var startTangentOutWorldPosition);
                    currentSplineNodeComponent.SplineNode.WorldPosition = startTangentOutWorldPosition;
                    currentSplineNodeComponent.SplineNode.TangentOutWorldPosition = startTangentOutWorldPosition + currentSplineNodeComponent.SplineNode.TangentOutLocal;
                    currentSplineNodeComponent.SplineNode.TangentInWorldPosition = startTangentOutWorldPosition + currentSplineNodeComponent.SplineNode.TangentInLocal;
                    updatedSplineNodes.Add(currentSplineNodeComponent.SplineNode);
                }
            }

            SplineComponent.Spline.SplineNodes = updatedSplineNodes;
            SplineComponent.Spline.UpdateSpline();
            var graphicsDeviceService = Services.GetService<IGraphicsDeviceService>();
            var splineDebugEntity = SplineComponent.SplineRenderer.Update(SplineComponent.Spline, graphicsDeviceService?.GraphicsDevice, SplineComponent.Entity.Transform.Position);
            if (splineDebugEntity != null)
            {
                var existingRenderer = SplineComponent.Entity.FindChild("SplineRenderer");
                if (existingRenderer != null)
                {
                    SplineComponent.Entity.RemoveChild(existingRenderer);
                }

                SplineComponent.Entity.AddChild(splineDebugEntity);
            }
        }
    }
}
