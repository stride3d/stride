using System;
using Stride.BepuPhysics.Components.Constraints;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Rendering;


namespace Stride.BepuPhysics.Demo.Components.Utils
{
    [ComponentCategory("BepuDemo - Test")]
    public class TestComponent : SyncScript
    {

        public Entity ToPointAtEntity { get; set; }
        public Vector3 up = new Vector3(0, 1, 0);

        public override void Update()
        {
            if (ToPointAtEntity == null)
                return;

            var dest = ToPointAtEntity.Transform.GetWorldPos();
            var origin = Entity.Transform.GetWorldPos();

            var dir = dest - origin;
            dir.Normalize();

            Entity.Transform.Rotation = Quaternion.LookRotation(dir, up);
        }
    }
}
