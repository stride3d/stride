using BepuPhysicIntegrationTest.Integration.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace BepuPhysicIntegrationTest.Integration.Components.Containers
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ContainerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Containers")]

    public abstract class ContainerComponent : EntityComponent
    {
        private int? _simulationIndex = 0;

        public int SimulationIndex
        {
            get => _simulationIndex ?? 0;
            set
            {
                ContainerData?.DestroyContainer();
                _simulationIndex = value;
                ContainerData?.BuildOrUpdateContainer();
            }
        }

        /// <summary>
        /// ContainerData is the bridge to Bepu.
        /// Automatically set by processor.
        /// </summary>
        internal ContainerData? ContainerData { get; set; }
    }
}
