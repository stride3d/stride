using BepuPhysics.Collidables;
using BepuPhysics;
using Stride.BepuPhysics.Components.Containers.Interfaces;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Rendering;
using Stride.Core.Mathematics;
using Stride.BepuPhysics.Extensions;

namespace Stride.BepuPhysics.Components.Containers
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ContainerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Containers")]
    public class StaticMeshContainerComponent : ContainerComponent, IStaticContainer, IContainerWithMesh
    {

        #region Static

        private StaticReference GetPhysicStaticRef()
        {
            if (ContainerData == null)
                throw new Exception("Container data is null");

            return ContainerData.BepuSimulation.Simulation.Statics[ContainerData.SHandle];
        }

        /// <summary>
        /// Get the bepu StaticReference /!\
        /// </summary>
        /// <returns>A volatil ref to the bepu static associed with this bodyContainer</returns>
        [DataMemberIgnore]
        public StaticReference? GetPhysicStatic => ContainerData?.BepuSimulation.Simulation.Statics[ContainerData.SHandle];
        [DataMemberIgnore]
        public Vector3 Position
        {
            get => GetPhysicStaticRef().Pose.Position.ToStrideVector();
            set
            {
                var bodyRef = GetPhysicStaticRef();
                bodyRef.Pose.Position = value.ToNumericVector();
            }
        }
        [DataMemberIgnore]
        public Quaternion Orientation
        {
            get => GetPhysicStaticRef().Pose.Orientation.ToStrideQuaternion();
            set
            {
                var bodyRef = GetPhysicStaticRef();
                bodyRef.Pose.Orientation = value.ToNumericQuaternion();
            }
        }
        [DataMemberIgnore]
        public ContinuousDetection ContinuousDetection
        {
            get => GetPhysicStaticRef().Continuity;
            set
            {
                var bodyRef = GetPhysicStaticRef();
                bodyRef.Continuity = value;
            }
        }

        #endregion

        #region WithMesh

        private float _mass = 1f;
        private bool _closed = true;
        private Model? _model;

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
        public Model? Model
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
