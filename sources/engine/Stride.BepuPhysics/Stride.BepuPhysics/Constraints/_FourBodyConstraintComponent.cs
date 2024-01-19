using BepuPhysics.Constraints;

namespace Stride.BepuPhysics.Constraints;

public abstract class FourBodyConstraintComponent<T> : ConstraintComponent<T> where T : unmanaged, IConstraintDescription<T>, IFourBodyConstraintDescription<T>
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

    public BodyComponent? D
    {
        get => this[3];
        set => this[3] = value;
    }

    public FourBodyConstraintComponent() : base(4){ }
}