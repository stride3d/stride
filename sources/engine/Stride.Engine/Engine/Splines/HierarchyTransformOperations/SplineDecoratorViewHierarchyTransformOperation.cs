using Stride.Engine.Splines.Components;

namespace Stride.Engine.Splines
{
    /// <summary>
    /// Updates <see cref="Engine.SplineDecoratorComponent"/>.
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
