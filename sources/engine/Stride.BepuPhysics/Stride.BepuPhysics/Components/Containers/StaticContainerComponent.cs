using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Components.Containers.Interfaces;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.BepuPhysics.Extensions;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Components.Containers
{
    public class StaticContainerComponent : ContainerComponent, IStaticContainer, IContainerWithColliders
    {

        [DataMemberIgnore]
        public new IContactEventHandler? ContactEventHandler
        {
            get => base.ContactEventHandler;
            set => base.ContactEventHandler = value;
        }

        #region Static

        private StaticReference? GetPhysicStaticRef() => ContainerData?.StaticReference;

        [DataMemberIgnore]
        public Vector3 Position
        {
            get => GetPhysicStaticRef()?.Pose.Position.ToStrideVector() ?? default;
            set
            {
                if (GetPhysicStaticRef() is {} staticRef)
                    staticRef.Pose.Position = value.ToNumericVector();
            }
        }
        [DataMemberIgnore]
        public Quaternion Orientation
        {
            get => GetPhysicStaticRef()?.Pose.Orientation.ToStrideQuaternion() ?? default;
            set
            {
                if (GetPhysicStaticRef() is {} staticRef)
                    staticRef.Pose.Orientation = value.ToNumericQuaternion();
            }
        }
        [DataMemberIgnore]
        public ContinuousDetection ContinuousDetection
        {
            get => GetPhysicStaticRef()?.Continuity ?? default;
            set
            {
                if (GetPhysicStaticRef() is {} staticRef)
                    staticRef.Continuity = value;
            }
        }

        #endregion

        #region WithCollider

        [Display(Expand = ExpandRule.Always)]
        public ListOfColliders Colliders { get; set; } = new();

        public StaticContainerComponent()
        {
            Colliders.OnEditCallBack = () => ContainerData?.TryUpdateContainer();
        }

        #endregion

    }
}
