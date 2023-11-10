using BepuPhysicIntegrationTest.Integration.Configurations;
using Stride.Core;
using Stride.Engine;

namespace BepuPhysicIntegrationTest.Integration.Components.Utils
{
    public abstract class SimulationUpdateComponent : SyncScript
    {
        [DataMemberIgnore]
        public BepuSimulation BepuSimulation { get; set; }

        public override void Start()
        {
			BepuSimulation = Services.GetService<BepuConfiguration>().BepuSimulations[0];

			base.Start();
            BepuSimulation.Register(this);
        }
        public override void Cancel()
        {
            base.Cancel();
            BepuSimulation.Unregister(this);
        }

        public abstract void SimulationUpdate(float simTimeStep);
    }
}
