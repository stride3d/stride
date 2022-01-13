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
