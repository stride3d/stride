// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Collidables;
using Stride.BepuPhysics.Definitions;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.BepuPhysics.Demo.Components.Utils
{
    [ComponentCategory("BepuDemo - Utils")]
    public class OverlapTesterComponent : SyncScript
    {
        public override void Update()
        {
            System.Span<CollidableStack> buffer = stackalloc CollidableStack[16];
            var bepuConfig = Services.GetService<BepuConfiguration>();
            var rot = Entity.Transform.GetWorldRot();
            var pos = Entity.Transform.GetWorldPos();

            if (Entity.GetSimulation().SweepCast(new Box(0.25f, 0.25f, 0.25f), new RigidPose(pos, rot), new BodyVelocity((rot * new Vector3(0, 0, 1)), default), 10, out _))
            {
                DebugText.Print("Sweep successful", new((int)(Game.Window.PreferredWindowedSize.X - 500 * 1.5f), 700));
            }
            else
            {
                DebugText.Print("No sweep", new((int)(Game.Window.PreferredWindowedSize.X - 500 * 1.5f), 700));
            }

            int j = 0;
            foreach (var hit in Entity.GetSimulation().Overlap(new Box(0.25f, 0.25f, 0.25f), new RigidPose(pos, rot), buffer))
            {
                DebugText.Print($"Overlap : {hit.Entity.Name}", new((int)(Game.Window.PreferredWindowedSize.X - 500 * 1.5f), 430 + 25 * j));
                j++;
            }
            if (j == 0)
            {
                DebugText.Print("no overlap", new((int)(Game.Window.PreferredWindowedSize.X - 500 * 1.5f), 430));
            }
        }
    }
}
