using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Extensions;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;

namespace Stride.BepuPhysics.Components.Colliders
{
    [DataContract]
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
        internal ContainerComponent? Container { get; set; }

        internal abstract void AddToCompoundBuilder(IGame game, ref CompoundBuilder builder, RigidPose localPose);
    }
}
