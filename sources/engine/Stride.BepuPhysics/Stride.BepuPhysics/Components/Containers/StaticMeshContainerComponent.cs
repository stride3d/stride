using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Rendering;

namespace Stride.BepuPhysics.Components.Containers
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ContainerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Containers")]
    public class StaticMeshContainerComponent : StaticContainerComponent, IMeshContainerComponent
    {

        private float _mass = 1f;
        private bool _closed = true;
        private Model? _model;

        public float Mass
        {
            get => _mass;
            set
            {
                _mass = value;
                ContainerData?.TryUpdateContainer();
            }
        }
        public bool Closed
        {
            get => _closed;
            set
            {
                _closed = value;
                ContainerData?.TryUpdateContainer();
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
    }
}
