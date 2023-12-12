using BepuPhysics;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Components.Containers
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ContainerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Containers")]
    public class StaticContainerComponent : ContainerComponent
    {
        #warning same as with the BodyContainerComponent, dump the methods users might need in here and hide it away
        StaticReference? GetPhysicStatic()
        {
            return ContainerData?.BepuSimulation.Simulation.Statics[ContainerData.SHandle];
        }
    }
}
