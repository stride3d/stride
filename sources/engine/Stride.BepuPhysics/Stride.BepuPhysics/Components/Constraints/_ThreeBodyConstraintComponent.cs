using BepuPhysics.Constraints;
using Stride.BepuPhysics.Components.Containers.Interfaces;

namespace Stride.BepuPhysics.Components.Constraints
{
    public abstract class ThreeBodyConstraintComponent<T> : ConstraintComponent<T> where T : unmanaged, IConstraintDescription<T>, IThreeBodyConstraintDescription<T>
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

        public IBodyContainer? C
        {
            get => this[2];
            set => this[2] = value;
        }

        public ThreeBodyConstraintComponent() : base(3){ }
    }
}