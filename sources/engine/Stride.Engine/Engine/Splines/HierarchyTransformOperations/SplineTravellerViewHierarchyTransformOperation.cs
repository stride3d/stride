
using Stride.Engine.Splines.Components;

namespace Stride.Engine.Splines
{
    /// <summary>
    /// Updates <see cref="Engine.SplineFollowerComponent"/>.
    /// </summary>
    public class SplineTravellerViewHierarchyTransformOperation : TransformOperation
    {
        public readonly SplineTravellerComponent SplineTravellerComponent;

        public SplineTravellerViewHierarchyTransformOperation(SplineTravellerComponent modelComponent)
        {
            SplineTravellerComponent = modelComponent;
        }

        /// <inheritdoc/>
        public override void Process(TransformComponent transformComponent)
        {
            SplineTravellerComponent.Update(transformComponent);
        }
    }
}
