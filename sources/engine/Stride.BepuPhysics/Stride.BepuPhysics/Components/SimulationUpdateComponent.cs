using Stride.BepuPhysics.Configurations;
using Stride.Core;
using Stride.Engine;

namespace Stride.BepuPhysics.Components
{

    public abstract class SimulationUpdateComponent : SyncScript, ISimulationUpdate
    {
        private bool _started = false;
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
                    if (_started)
                        Start();
                }
            }
        }


        [DataMemberIgnore]
        public BepuSimulation? BepuSimulation { get; set; }

        public override void Start()
        {
            _started = true;
            base.Start();
            BepuServicesHelper.LoadBepuServices(Services);
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
        public abstract void AfterSimulationUpdate(float simTimeStep);
    }
}
