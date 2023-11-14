using System.Collections.Generic;
using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysicIntegrationTest.Integration.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace BepuPhysicIntegrationTest.Integration.Components.ConstraintsV2
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Constraint")]
    [AllowMultipleComponents]

    public abstract class ConstraintComponent : EntityComponent
    {
        public List<BodyContainerComponent> Bodies { get; set; } = new();


        /// <summary>
        /// ContainerData is the bridge to Bepu.
        /// Automatically set by processor.
        /// </summary>
        internal ConstraintData ConstraintData { get; set; }
    }
}
