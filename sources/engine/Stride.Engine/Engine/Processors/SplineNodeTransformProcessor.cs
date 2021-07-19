//// Copyright (c) Stride contributors (https://Stride.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine.Splines;
using Stride.Engine.Splines.Components;

namespace Stride.Engine.Processors
{
    /// <summary>
    /// The processor for <see cref="SplineNodeComponent"/>.
    /// </summary>
    public class SplineNodeTransformProcessor : EntityProcessor<SplineNodeComponent, SplineNodeTransformProcessor.SplineNodeTransformationInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SplineTransformProcessor"/> class.
        /// </summary>
        public SplineNodeTransformProcessor()
            : base(typeof(TransformComponent))
        {
        }

        protected override SplineNodeTransformationInfo GenerateComponentData(Entity entity, SplineNodeComponent component)
        {
            return new SplineNodeTransformationInfo
            {
                TransformOperation = new SplineNodeViewHierarchyTransformOperation(component),
            };
        }

        protected override bool IsAssociatedDataValid(Entity entity, SplineNodeComponent component, SplineNodeTransformationInfo associatedData)
        {
            return component == associatedData.TransformOperation.SplineNodeComponent;
        }

        protected override void OnEntityComponentAdding(Entity entity, SplineNodeComponent component, SplineNodeTransformationInfo data)
        {
            // Register model view hierarchy update
            entity.Transform.PostOperations.Add(data.TransformOperation);
        }

        protected override void OnEntityComponentRemoved(Entity entity, SplineNodeComponent component, SplineNodeTransformationInfo data)
        {
            // Unregister model view hierarchy update
            entity.Transform.PostOperations.Remove(data.TransformOperation);
        }

        public class SplineNodeTransformationInfo
        {
            public SplineNodeViewHierarchyTransformOperation TransformOperation;
        }
    }
}
