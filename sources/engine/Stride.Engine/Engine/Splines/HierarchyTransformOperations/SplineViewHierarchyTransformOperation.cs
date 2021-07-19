//// Copyright (c) Stride contributors (https://Stride.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Engine.Splines.Components;

namespace Stride.Engine.Splines
{
    /// <summary>
    /// Updates <see cref="Engine.SplineComponent"/>.
    /// </summary>
    public class SplineViewHierarchyTransformOperation : TransformOperation
    {
        public readonly SplineComponent SplineComponent;

        public SplineViewHierarchyTransformOperation(SplineComponent modelComponent)
        {
            SplineComponent = modelComponent;
        }

        /// <inheritdoc/>
        public override void Process(TransformComponent transformComponent)
        {
            SplineComponent.Update(transformComponent);
        }
    }
}
