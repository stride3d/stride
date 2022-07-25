//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine.Splines.Components;
using Stride.Engine.Splines.HierarchyTransformOperations;
using Stride.Engine.Splines.Models;
using Stride.Games;

namespace Stride.Engine.Splines.Processors
{
    /// <summary>
    /// The processor for <see cref="SplineTraverserComponent"/>.
    /// </summary>
    public class SplineTraverserTransformProcessor : EntityProcessor<SplineTraverserComponent, SplineTraverserTransformProcessor.SplineTraverserTransformationInfo>
    {
        private HashSet<SplineTraverserComponent> splineTraverserComponents = new();

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
            component.SplineTraverser.OnSplineTraverserDirty += () => component.SplineTraverser?.CalculateTargets();

            splineTraverserComponents.Add(component);

            // Register model view hierarchy update
            entity.Transform.PostOperations.Add(data.TransformOperation);
        }

        protected override void OnEntityComponentRemoved(Entity entity, SplineTraverserComponent component, SplineTraverserTransformationInfo data)
        {
            // Unregister model view hierarchy update
            entity.Transform.PostOperations.Remove(data.TransformOperation);

            splineTraverserComponents.Remove(component);
        }

        public class SplineTraverserTransformationInfo
        {
            public SplineTraverserViewHierarchyTransformOperation TransformOperation;
        }

        public override void Update(GameTime time)
        {
            foreach (var splineTraverserComponent in splineTraverserComponents)
            {
                splineTraverserComponent.SplineTraverser.Update(time);
            }
        }
    }
}
