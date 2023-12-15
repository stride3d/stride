using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Engine;

namespace Stride.BepuPhysics.Components
{
    public abstract class SimulationUpdateComponent : SyncScript
    {

        private int _simulationIndex = 0;

        public int SimulationIndex
        {
            get => _simulationIndex;
            set
            {
                if (_simulationIndex != value)
                {
                    Cancel();
                    _simulationIndex = value;
                    Start();
                }
            }
        }


        [DataMemberIgnore]
        public BepuSimulation? BepuSimulation { get; set; }

        public override void Start()
        {
            base.Start();
            BepuSimulation = Services.GetService<BepuConfiguration>().BepuSimulations[SimulationIndex];
            BepuSimulation.Register(this);
        }
        public override void Cancel()
        {
            base.Cancel();
            BepuSimulation?.Unregister(this);
            BepuSimulation = null;
        }

        public abstract void SimulationUpdate(float simTimeStep);
        public virtual void AfterSimulationUpdate(float simTimeStep) { }
    }
}
