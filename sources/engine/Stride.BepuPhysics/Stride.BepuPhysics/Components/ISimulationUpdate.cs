// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.BepuPhysics.Components;

/// <summary>
/// Only usable on <see cref="CollidableComponent"/>,
/// Used to let the simulation know to call pre- and post-simulation update events
/// </summary>
internal interface ISimulationUpdate
{
    /// <summary>
    /// Called before the simulation updates
    /// </summary>
    /// <param name="simTimeStep"> The amount of time in seconds this simulation lasts for </param>
    public void SimulationUpdate(float simTimeStep);

    /// <summary>
    /// Called after the simulation updates
    /// </summary>
    /// <param name="simTimeStep"> The amount of time in seconds this simulation lasts for </param>
    public void AfterSimulationUpdate(float simTimeStep);
}
