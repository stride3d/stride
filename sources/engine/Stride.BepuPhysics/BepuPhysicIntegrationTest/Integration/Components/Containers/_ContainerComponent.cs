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
        public int SimulationIndex { get; set; } = 0; //TODO : destroy/rebuild shape on edit. + Check Services.GetService<BepuConfiguration>().BepuSimulations bounds.

        /// <summary>
        /// ContainerData is the bridge to Bepu.
        /// Automatically set by processor.
        /// </summary>
        internal ContainerData ContainerData { get; set; }
    }
}
