using Stride.Core;
using Stride.Games;

namespace Stride.BepuPhysics.Systems;

internal class PhysicsGameSystem : GameSystemBase
{
    private BepuConfiguration _bepuConfiguration;

    public PhysicsGameSystem(IServiceRegistry registry) : base(registry)
    {
        _bepuConfiguration = registry.GetService<BepuConfiguration>();
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