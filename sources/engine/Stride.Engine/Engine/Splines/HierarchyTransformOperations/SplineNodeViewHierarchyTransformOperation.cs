using Stride.Engine.Splines.Components;

namespace Stride.Engine.Splines
{
    /// <summary>
    /// Updates <see cref="Engine.SplineNodeComponent"/>.
    /// </summary>
    public class SplineNodeViewHierarchyTransformOperation : TransformOperation
    {
        public readonly SplineNodeComponent SplineNodeComponent;

        public SplineNodeViewHierarchyTransformOperation(SplineNodeComponent modelComponent)
        {
            SplineNodeComponent = modelComponent;
        }

        /// <inheritdoc/>
        public override void Process(TransformComponent transformComponent)
        {
            SplineNodeComponent.Update(transformComponent);
        }
    }
}
