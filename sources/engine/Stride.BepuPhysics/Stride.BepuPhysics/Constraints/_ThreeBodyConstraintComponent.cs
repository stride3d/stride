using BepuPhysics.Constraints;

namespace Stride.BepuPhysics.Constraints;

public abstract class ThreeBodyConstraintComponent<T> : ConstraintComponent<T> where T : unmanaged, IConstraintDescription<T>, IThreeBodyConstraintDescription<T>
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

    public BodyComponent? C
    {
        get => this[2];
        set => this[2] = value;
    }

    public ThreeBodyConstraintComponent() : base(3){ }
}