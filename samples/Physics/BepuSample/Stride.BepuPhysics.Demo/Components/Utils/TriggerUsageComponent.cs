// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics.Definitions;
using Stride.Engine;
using Stride.Rendering;


namespace Stride.BepuPhysics.Demo.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("BepuDemo - Utils")]
    public class TriggerUsageComponent : StartupScript
    {
        public CollidableComponent Trigger { get; set; }
        public Material MatRed { get; set; }
        public Material MatBase { get; set; }

        public override void Start()
        {
            var trigger = new Trigger();
            Trigger.ContactEventHandler = trigger;
            trigger.OnEnter += SetRedColor;
            trigger.OnLeave += SetBaseColor;
        }

        private void SetBaseColor(object? sender, CollidableComponent e)
        {
            var mc = Trigger.Entity.Get<ModelComponent>();
            mc.Materials.Clear();
            mc.Materials.Add(new(0,MatBase));
        }

        private void SetRedColor(object? sender, CollidableComponent e)
        {
            var mc = Trigger.Entity.Get<ModelComponent>();
            mc.Materials.Clear();
            mc.Materials.Add(new(0, MatRed));
        }

    }
}
