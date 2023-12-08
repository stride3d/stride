using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Components.Containers
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ContainerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Containers")]
    public class BodyMeshContainerComponent : BodyContainerComponent
    {

        private float _mass = 1f;
        private bool _closed = true;

        public float Mass
        {
            get => _mass;
            set
            {
                _mass = value;
                if (ContainerData?.Exist == true)
                    ContainerData.BuildOrUpdateContainer();
            }
        }
        public bool Closed
        {
            get => _closed;
            set
            {
                _closed = value;
                if (ContainerData?.Exist == true)
                    ContainerData.BuildOrUpdateContainer();
            }
        }

        public ModelComponent? ModelData { get; set; }

    }
}
