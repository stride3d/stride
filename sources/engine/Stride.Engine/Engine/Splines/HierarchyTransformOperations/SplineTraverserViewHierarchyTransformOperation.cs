using Stride.Engine.Splines.Components;

namespace Stride.Engine.Splines;

/// <summary>
/// Updates <see cref="Engine.SplineTraverserComponent"/>.
/// </summary>
public class SplineTraverserViewHierarchyTransformOperation : TransformOperation
{
    public readonly SplineTraverserComponent SplineTraverserComponent;

    public SplineTraverserViewHierarchyTransformOperation(SplineTraverserComponent modelComponent)
    {
        SplineTraverserComponent = modelComponent;
    }

    /// <inheritdoc/>
    public override void Process(TransformComponent transformComponent)
    {
        SplineTraverserComponent.Update(transformComponent);
    }
}
