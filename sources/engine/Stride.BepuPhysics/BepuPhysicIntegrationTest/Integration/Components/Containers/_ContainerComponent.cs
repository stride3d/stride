using BepuPhysicIntegrationTest.Integration.Components.Simulations;
using BepuPhysicIntegrationTest.Integration.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace BepuPhysicIntegrationTest.Integration.Components.Containers
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ContainerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Containers")]

    public abstract class ContainerComponent : StartupScript
    {
        public SimulationComponent BepuSimulation => Entity.GetInMeOrParents<SimulationComponent>();
        internal ContainerData ContainerData { get; set; }

        public override void Start()
        {
            base.Start();
        }
    }
}
