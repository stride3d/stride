using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;

namespace Stride.BepuPhysics.Components.Colliders
{
    [DataContract(Inherited = true)]
    [DefaultEntityComponentProcessor(typeof(ColliderProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Colliders")]
    [AllowMultipleComponents]
    public abstract class ColliderComponent : EntityComponent
    {
        private float _mass = 1f;

        public float Mass
        {
            get => _mass;
            set
            {
                _mass = value;
                Container?.ContainerData?.TryUpdateContainer();
            }
        }

        [DataMemberIgnore]
        public ContainerComponent? Container { get; internal set; }

        internal abstract void AddToCompoundBuilder(IGame game, ref CompoundBuilder builder, RigidPose localPose);


    }
}
