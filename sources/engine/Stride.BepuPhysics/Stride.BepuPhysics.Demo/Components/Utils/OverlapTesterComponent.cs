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
            var buffer = System.Buffers.ArrayPool<ContainerComponent>.Shared.Rent(16);
            var bepuConfig = Services.GetService<BepuConfiguration>();
            var rot = Entity.Transform.GetWorldRot();
            var pos = Entity.Transform.GetWorldPos();

            if (bepuConfig.BepuSimulations[0].SweepCast(new Box(0.25f, 0.25f, 0.25f), new RigidPose(pos, rot), new BodyVelocity((rot * new Vector3(0, 0, 1)), default), 10, out _))
            {
                DebugText.Print("Sweep successful", new((int)(Game.Window.PreferredWindowedSize.X - 500 * 1.5f), 700));
            }
            else
            {
                DebugText.Print("No sweep", new((int)(Game.Window.PreferredWindowedSize.X - 500 * 1.5f), 700));
            }

            bepuConfig.BepuSimulations[0].Overlap(new Box(0.25f, 0.25f, 0.25f), new RigidPose(pos, rot), buffer, out var containers);
            for (int j = 0; j < containers.Length; j++)
            {
                var hitInfo = containers[j];
                DebugText.Print($"Overlap : {hitInfo.Entity.Name}", new((int)(Game.Window.PreferredWindowedSize.X - 500 * 1.5f), 430 + 25 * j));
            }
            if (containers.Length == 0)
            {
                DebugText.Print("no overlap", new((int)(Game.Window.PreferredWindowedSize.X - 500 * 1.5f), 430));
            }
            System.Buffers.ArrayPool<ContainerComponent>.Shared.Return(buffer);
        }
    }
}