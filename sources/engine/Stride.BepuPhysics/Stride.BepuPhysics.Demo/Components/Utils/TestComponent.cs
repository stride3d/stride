// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;

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
