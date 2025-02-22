// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics.Definitions;
using Stride.Core;
using Stride.Games;

namespace Stride.BepuPhysics.Systems;

internal class PhysicsGameSystem : GameSystemBase
{
    private BepuConfiguration _bepuConfiguration;

    public PhysicsGameSystem(BepuConfiguration configuration, IServiceRegistry registry) : base(registry)
    {
        _bepuConfiguration = configuration;
        UpdateOrder = SystemsOrderHelper.ORDER_OF_GAME_SYSTEM;
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
