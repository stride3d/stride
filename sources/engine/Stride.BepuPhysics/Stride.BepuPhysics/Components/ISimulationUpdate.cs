namespace Stride.BepuPhysics.Components;

/// <summary>
/// Only usable on <see cref="CollidableComponent"/>,
/// Used to let the simulation know to call pre- and post-simulation update events
/// </summary>
internal interface ISimulationUpdate
{
    public void SimulationUpdate(float simTimeStep);
    public void AfterSimulationUpdate(float simTimeStep);
}
