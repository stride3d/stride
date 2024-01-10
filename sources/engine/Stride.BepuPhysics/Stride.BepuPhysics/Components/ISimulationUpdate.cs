namespace Stride.BepuPhysics.Components
{
    public interface ISimulationUpdate
    {
        public void SimulationUpdate(float simTimeStep);
        public void AfterSimulationUpdate(float simTimeStep);
    }
}