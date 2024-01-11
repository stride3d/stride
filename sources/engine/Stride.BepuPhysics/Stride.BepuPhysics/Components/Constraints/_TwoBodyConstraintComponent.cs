using BepuPhysics.Constraints;
using Stride.BepuPhysics.Components.Containers.Interfaces;

namespace Stride.BepuPhysics.Components.Constraints
{
    public abstract class TwoBodyConstraintComponent<T> : ConstraintComponent<T> where T : unmanaged, IConstraintDescription<T>, ITwoBodyConstraintDescription<T>
    {
        public IBodyContainer? A
        {
            get => this[0];
            set => this[0] = value;
        }

        public IBodyContainer? B
        {
            get => this[1];
            set => this[1] = value;
        }

        public TwoBodyConstraintComponent() : base(2) { }
    }
}