namespace Stride.BepuPhysics.Components
{
    /// <summary>
    /// Only usable on Containers,
    /// This interface will register containers and call theses functions.
    /// </summary>
    internal interface ISimulationUpdate
    {
        public void SimulationUpdate(float simTimeStep);
        public void AfterSimulationUpdate(float simTimeStep);
    }
}