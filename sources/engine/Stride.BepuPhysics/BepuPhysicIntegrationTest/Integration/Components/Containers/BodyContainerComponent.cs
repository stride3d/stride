using BepuPhysicIntegrationTest.Integration.Processors;
using BepuPhysics;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace BepuPhysicIntegrationTest.Integration.Components.Containers
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ContainerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Containers")]
    public class BodyContainerComponent : ContainerComponent
    {
        public bool Kinematic { get; set; } = false;

        public BodyReference? GetPhysicBody()
        {
            return ContainerData?.BepuSimulation.Simulation.Bodies[ContainerData.BHandle];
        }

    }
}
