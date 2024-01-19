using BepuPhysics.Constraints;

namespace Stride.BepuPhysics.Constraints;

public abstract class OneBodyConstraintComponent<T> : ConstraintComponent<T> where T : unmanaged, IConstraintDescription<T>, IOneBodyConstraintDescription<T>
{
    public BodyComponent? A
    {
        get => this[0];
        set => this[0] = value;
    }

    public OneBodyConstraintComponent() : base(1){ }
}