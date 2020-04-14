// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Engine
{
    /// <summary>
    /// Updates <see cref="Engine.ModelComponent.Skeleton"/>.
    /// </summary>
    public class ModelViewHierarchyTransformOperation : TransformOperation
    {
        public readonly ModelComponent ModelComponent;

        public ModelViewHierarchyTransformOperation(ModelComponent modelComponent)
        {
            ModelComponent = modelComponent;
        }

        /// <inheritdoc/>
        public override void Process(TransformComponent transformComponent)
        {
            ModelComponent.Update(transformComponent);
        }
    }
}
