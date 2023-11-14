using System.Collections.Generic;
using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysicIntegrationTest.Integration.Configurations;
using BepuPhysicIntegrationTest.Integration.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace BepuPhysicIntegrationTest.Integration.Components.ConstraintsV2
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ConstraintProcessorV2), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - ConstraintV2")]
    [AllowMultipleComponents]

    public abstract class ConstraintComponentV2 : EntityComponent
    {
        /// <summary>
        /// Get or set the SimulationComponent. If set null, it will try to find it in this or parent entities
        /// </summary>
        public BepuSimulation BepuSimulation { get; set; }

        public List<BodyContainerComponent> Bodies { get; set; } = new();


        /// <summary>
        /// ContainerData is the bridge to Bepu.
        /// Automatically set by processor.
        /// </summary>
        internal ConstraintDataV2 ConstraintData { get; set; }
    }
}
