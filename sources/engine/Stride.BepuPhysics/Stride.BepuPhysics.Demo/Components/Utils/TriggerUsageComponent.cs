using Stride.BepuPhysics.Components;
using Stride.Engine;
using Stride.Rendering;


namespace Stride.BepuPhysics.Demo.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("BepuDemo - Utils")]
    public class TriggerUsageComponent : StartupScript
    {
        public ContainerComponent Trigger { get; set; }
        public Material MatRed { get; set; }
        public Material MatBase { get; set; }

        public override void Start()
        {
            var trigger = new Trigger();
            Trigger.ContactEventHandler = trigger;
            trigger.OnEnter += SetRedColor;
            trigger.OnLeave += SetBaseColor;
        }

        private void SetBaseColor(object? sender, ContainerComponent e)
        {
            var mc = Trigger.Entity.Get<ModelComponent>();
            mc.Materials.Clear();
            mc.Materials.Add(new(0,MatBase));
        }

        private void SetRedColor(object? sender, ContainerComponent e)
        {
            var mc = Trigger.Entity.Get<ModelComponent>();
            mc.Materials.Clear();
            mc.Materials.Add(new(0, MatRed));
        }

    }
}
