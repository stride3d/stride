using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Components.Containers;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;

namespace Stride.BepuPhysics.Definitions.Colliders
{
    [DataContract(Inherited = true)]
    public abstract class ColliderBase
    {
        private float _mass = 1f;
        private Vector3 _linearOffset = Vector3.Zero;
        private Vector3 _rotationOffset = Vector3.Zero;

        public float Mass
        {
            get => _mass;
            set
            {
                _mass = value;
                Container?.ContainerData?.TryUpdateContainer();
            }
        }
        public Vector3 LinearOffset
        {
            get => _linearOffset;
            set
            {
                _linearOffset = value;
                Container?.ContainerData?.TryUpdateContainer();
            }
        }
        public Vector3 RotationOffset
        {
            get => _rotationOffset;
            set
            {
                _rotationOffset = value;
                Container?.ContainerData?.TryUpdateContainer();
            }
        }

        [DataMemberIgnore]
        public ContainerComponent? Container { get; internal set; }

        internal abstract void AddToCompoundBuilder(IGame game, ref CompoundBuilder builder, RigidPose localPose);


    }
}
