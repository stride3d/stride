//// Copyright (c) Stride contributors (https://Stride.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine.Splines;
using Stride.Engine.Splines.Components;

namespace Stride.Engine.Processors
{
    /// <summary>
    /// The processor for <see cref="SplineDecoratorComponent"/>.
    /// </summary>
    public class SplineDecoratorTransformProcessor : EntityProcessor<SplineDecoratorComponent, SplineDecoratorTransformProcessor.SplineDecoratorTransformationInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SplineDecoratorTransformProcessor"/> class.
        /// </summary>
        public SplineDecoratorTransformProcessor()
            : base(typeof(TransformComponent))
        {
        }

        protected override SplineDecoratorTransformationInfo GenerateComponentData(Entity entity, SplineDecoratorComponent component)
        {
            return new SplineDecoratorTransformationInfo
            {
                TransformOperation = new SplineDecoratorViewHierarchyTransformOperation(component),
            };
        }

        protected override bool IsAssociatedDataValid(Entity entity, SplineDecoratorComponent component, SplineDecoratorTransformationInfo associatedData)
        {
            return component == associatedData.TransformOperation.SplineDecoratorComponent;
        }

        protected override void OnEntityComponentAdding(Entity entity, SplineDecoratorComponent component, SplineDecoratorTransformationInfo data)
        {
            // Register model view hierarchy update
            entity.Transform.PostOperations.Add(data.TransformOperation);
        }

        protected override void OnEntityComponentRemoved(Entity entity, SplineDecoratorComponent component, SplineDecoratorTransformationInfo data)
        {
            // Unregister model view hierarchy update
            entity.Transform.PostOperations.Remove(data.TransformOperation);
        }

        public class SplineDecoratorTransformationInfo
        {
            public SplineDecoratorViewHierarchyTransformOperation TransformOperation;
        }
    }
}
