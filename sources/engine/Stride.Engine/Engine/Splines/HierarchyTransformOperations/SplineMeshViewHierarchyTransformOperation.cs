//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine.Splines.Components;

namespace Stride.Engine.Splines.HierarchyTransformOperations
{
    /// <summary>
    /// Updates <see cref="Engine.SplineMeshComponent"/>.
    /// </summary>
    public class SplineMeshViewHierarchyTransformOperation : TransformOperation
    {
        public readonly SplineMeshComponent SplineMeshComponent;

        public SplineMeshViewHierarchyTransformOperation(SplineMeshComponent modelComponent)
        {
            SplineMeshComponent = modelComponent;
        }

        /// <inheritdoc/>
        public override void Process(TransformComponent transformComponent)
        {
            SplineMeshComponent.Update(transformComponent);
        }
    }
}
