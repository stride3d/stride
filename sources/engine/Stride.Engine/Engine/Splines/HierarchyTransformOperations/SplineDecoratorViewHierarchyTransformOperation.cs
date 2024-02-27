//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine.Splines.Components;

namespace Stride.Engine.Splines.HierarchyTransformOperations
{
    /// <summary>
    /// Updates <see cref="SplineDecoratorComponent"/>.
    /// </summary>
    public class SplineDecoratorViewHierarchyTransformOperation : TransformOperation
    {
        public readonly SplineDecoratorComponent SplineDecoratorComponent;

        public SplineDecoratorViewHierarchyTransformOperation(SplineDecoratorComponent modelComponent)
        {
            SplineDecoratorComponent = modelComponent;
        }

        /// <inheritdoc/>
        public override void Process(TransformComponent transformComponent)
        {
            SplineDecoratorComponent.Update(transformComponent);
        }
    }
}
