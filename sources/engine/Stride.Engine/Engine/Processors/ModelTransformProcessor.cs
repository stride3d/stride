// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Stride.Engine.Processors
{
    /// <summary>
    /// The processor for <see cref="ModelComponent"/>.
    /// </summary>
    public class ModelTransformProcessor : EntityProcessor<ModelComponent, ModelTransformProcessor.ModelTransformationInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelTransformProcessor"/> class.
        /// </summary>
        public ModelTransformProcessor()
            : base(typeof(TransformComponent))
        {
        }

        protected override ModelTransformationInfo GenerateComponentData(Entity entity, ModelComponent component)
        {
            return new ModelTransformationInfo
            {
                TransformOperation = new ModelViewHierarchyTransformOperation(component),
            };
        }

        protected override bool IsAssociatedDataValid(Entity entity, ModelComponent component, ModelTransformationInfo associatedData)
        {
            return component == associatedData.TransformOperation.ModelComponent;
        }

        protected override void OnEntityComponentAdding(Entity entity, ModelComponent component, ModelTransformationInfo data)
        {
            // Register model view hierarchy update
            entity.Transform.PostOperations.Add(data.TransformOperation);
        }

        protected override void OnEntityComponentRemoved(Entity entity, ModelComponent component, ModelTransformationInfo data)
        {
            // Unregister model view hierarchy update
            entity.Transform.PostOperations.Remove(data.TransformOperation);
        }

        public class ModelTransformationInfo
        {
            public ModelViewHierarchyTransformOperation TransformOperation;
        }
    }
}
