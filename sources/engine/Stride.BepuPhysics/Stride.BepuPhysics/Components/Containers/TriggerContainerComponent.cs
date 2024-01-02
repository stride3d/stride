using Stride.BepuPhysics.Definitions.Contacts;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Components.Containers
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ContainerProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Containers")]
    public class TriggerContainerComponent : StaticContainerComponent
    {

        public event EventHandler<ContainerComponent?>? ContainerEnter;
        public event EventHandler<ContainerComponent?>? ContainerLeave;

        public TriggerContainerComponent()
        {
            ContactEventHandler = new TriggerContactEventHandler(() => Simulation, RaiseEnterEvent, RaiseLeaveEvent);
        }

        public void RaiseEnterEvent(ContainerComponent? e)
        {
            ContainerEnter?.Invoke(this, e);
            Console.WriteLine("e");
        }
        public void RaiseLeaveEvent(ContainerComponent? e)
        {
            ContainerLeave?.Invoke(this, e);
            Console.WriteLine("l");
        }

    }

}
