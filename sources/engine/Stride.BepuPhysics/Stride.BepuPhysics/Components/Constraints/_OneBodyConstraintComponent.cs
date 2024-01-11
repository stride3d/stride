using BepuPhysics.Constraints;
using Stride.BepuPhysics.Components.Containers.Interfaces;

namespace Stride.BepuPhysics.Components.Constraints
{
    public abstract class OneBodyConstraintComponent<T> : ConstraintComponent<T> where T : unmanaged, IConstraintDescription<T>, IOneBodyConstraintDescription<T>
    {
        public IBodyContainer? A
        {
            get => this[0];
            set => this[0] = value;
        }

        public OneBodyConstraintComponent() : base(1){ }
    }
}