using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Components.Containers.Interfaces;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.BepuPhysics.Extensions;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Rendering;

namespace Stride.BepuPhysics.Components.Containers
{
    public class StaticMeshContainerComponent : ContainerComponent, IStaticContainer, IContainerWithMesh
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

        #region WithMesh

        private float _mass = 1f;
        private bool _closed = true;
        private Model _model = null!; // We have a 'required' guard making sure it is assigned

        public float Mass
        {
            get => _mass;
            set
            {
                if (_mass != value)
                {
                    _mass = value;
                    ContainerData?.TryUpdateContainer();
                }
            }
        }
        public bool Closed
        {
            get => _closed;
            set
            {
                if (_closed != value)
                {
                    _closed = value;
                    ContainerData?.TryUpdateContainer();
                }
            }
        }

        [MemberRequired]
        public required Model Model
        {
            get => _model;
            set
            {
                _model = value;
                ContainerData?.TryUpdateContainer();
            }
        }

        #endregion

    }
}
