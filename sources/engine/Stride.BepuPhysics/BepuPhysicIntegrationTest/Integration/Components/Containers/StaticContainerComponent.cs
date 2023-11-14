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
    public class StaticContainerComponent : ContainerComponent
    {
        public StaticReference? GetPhysicStatic()
        {
            return ContainerData?.BepuSimulation.Simulation.Statics[ContainerData.SHandle];
        }
    }
}
