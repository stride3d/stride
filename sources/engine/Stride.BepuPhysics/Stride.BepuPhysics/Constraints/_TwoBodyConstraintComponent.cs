using BepuPhysics.Constraints;

namespace Stride.BepuPhysics.Constraints;

public abstract class TwoBodyConstraintComponent<T> : ConstraintComponent<T> where T : unmanaged, IConstraintDescription<T>, ITwoBodyConstraintDescription<T>
{
    public BodyComponent? A
    {
        get => this[0];
        set => this[0] = value;
    }

    public BodyComponent? B
    {
        get => this[1];
        set => this[1] = value;
    }

    public TwoBodyConstraintComponent() : base(2) { }
}