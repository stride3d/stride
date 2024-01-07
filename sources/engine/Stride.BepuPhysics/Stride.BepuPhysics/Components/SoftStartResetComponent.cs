using Stride.BepuPhysics.Configurations;
using Stride.Engine;

namespace Stride.BepuPhysics.Components
{
    public abstract class SoftStartResetComponent : StartupScript
    {
        public int SimulationIndex { get; set; }

        public override void Start()
        {
            Services.GetService<BepuConfiguration>().BepuSimulations[SimulationIndex]?.ResetSoftStart();
        }
    }
}
