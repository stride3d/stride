using System.Diagnostics;
using BepuPhysics;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Extensions;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Games;

namespace Stride.BepuPhysics
{
    internal class BepuPhysicsGameSystem : GameSystemBase
    {
        private BepuConfiguration _bepuConfiguration;

        public BepuPhysicsGameSystem(IServiceRegistry registry) : base(registry)
        {
            _bepuConfiguration = registry.GetService<BepuConfiguration>();
            UpdateOrder = BepuOrderHelper.ORDER_OF_GAME_SYSTEM;
            Enabled = true; //enabled by default


            foreach (var bepuSim in _bepuConfiguration.BepuSimulations)
            {
                bepuSim.ResetSoftStart();
            }
        }

        public override void Update(GameTime time)
        {
            var elapsed = time.WarpElapsed;
            if (elapsed <= TimeSpan.Zero)
                return;

            foreach (var bepuSim in _bepuConfiguration.BepuSimulations)
            {
                bepuSim.Update(elapsed);
            }
        }
    }
}
