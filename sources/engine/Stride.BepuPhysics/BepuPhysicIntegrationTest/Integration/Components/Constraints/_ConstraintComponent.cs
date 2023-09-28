using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysicIntegrationTest.Integration.Components.Simulations;
using BepuPhysicIntegrationTest.Integration.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace BepuPhysicIntegrationTest.Integration.Components.Constraints
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ContainerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Constraint")]
    [AllowMultipleComponents]

    public abstract class ConstraintComponent : EntityComponent
    {
        private SimulationComponent _bepuSimulation = null;

        /// <summary>
        /// Get or set the SimulationComponent. If set null, it will try to find it in this or parent entities
        /// </summary>
        public SimulationComponent BepuSimulation
        {
            get => _bepuSimulation ?? Entity.GetInMeOrParents<SimulationComponent>();
            set
            {
                ConstraintData?.DestroyConstraint();
                _bepuSimulation = value;
                ConstraintData?.BuildConstraint();
            }
        }

        public BodyContainerComponent BodyA { get; set; }
        public BodyContainerComponent BodyB { get; set; }


        /// <summary>
        /// ContainerData is the bridge to Bepu.
        /// Automatically set by processor.
        /// </summary>
        internal ConstraintData ConstraintData { get; set; }
    }
}
