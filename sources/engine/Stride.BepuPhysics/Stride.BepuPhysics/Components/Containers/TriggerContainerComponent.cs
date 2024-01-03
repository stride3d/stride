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
        public new IContactEventHandler? ContactEventHandler => base.ContactEventHandler; //Make it readonly.

        public event EventHandler<IContainer?>? ContainerEnter;
        public event EventHandler<IContainer?>? ContainerLeave;

        public TriggerContainerComponent()
        {
            base.ContactEventHandler = new TriggerContactEventHandler(() => Simulation, RaiseEnterEvent, RaiseLeaveEvent);
        }

        public void RaiseEnterEvent(IContainer? e)
        {
            ContainerEnter?.Invoke(this, e);
        }
        public void RaiseLeaveEvent(IContainer? e)
        {
            ContainerLeave?.Invoke(this, e);
        }

    }

}
