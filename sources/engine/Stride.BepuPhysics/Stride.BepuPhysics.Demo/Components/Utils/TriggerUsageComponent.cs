using System;
using Stride.BepuPhysics.Components.Constraints;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Components.Containers.Interfaces;
using Stride.BepuPhysics.Extensions;
using Stride.Engine;
using Stride.Input;
using Stride.Rendering;


namespace Stride.BepuPhysics.Demo.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("BepuDemo - Utils")]
    public class TriggerUsageComponent : StartupScript
    {
        public TriggerContainerComponent Trigger { get; set; }
        public Material MatRed { get; set; }
        public Material MatBase { get; set; }

        public override void Start()
        {
            Trigger.ContainerEnter += SetRedColor;
            Trigger.ContainerLeave += SetBaseColor;
        }

        private void SetBaseColor(object? sender, IContainer e)
        {
            var mc = Trigger.Entity.Get<ModelComponent>();
            mc.Materials.Clear();
            mc.Materials.Add(new(0,MatBase));
        }

        private void SetRedColor(object? sender, IContainer e)
        {
            var mc = Trigger.Entity.Get<ModelComponent>();
            mc.Materials.Clear();
            mc.Materials.Add(new(0, MatRed));
        }

    }
}
