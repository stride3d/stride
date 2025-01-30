// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.BepuPhysics.Definitions;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.FlexibleProcessing;

namespace Stride.BepuPhysics.Components;

/// <summary>
/// Add this interface to your Entity components to get notified when physics simulation update occur
/// </summary>
public interface ISimulationUpdate : IComponent<ISimulationUpdate.SimUpdateProcessor, ISimulationUpdate>
{
    /// <summary>
    /// The entity this component belongs to, used with <see cref="SimulationSelector"/>
    /// </summary>
    Entity Entity { get; }

    /// <summary>
    /// The simulation which will call <see cref="SimulationUpdate"/> and <see cref="AfterSimulationUpdate"/> when it updates
    /// </summary>
    /// <remarks>
    /// Changing this in the middle of runtime will not change the simulation this object belongs to,
    /// you must call <see cref="NotifySimulationChanged"/> afterward
    /// </remarks>
    ISimulationSelector SimulationSelector => SceneBasedSimulationSelector.Shared;

    BepuSimulation? Simulation
    {
        get
        {
            if (Entity.EntityManager?.Services.GetService<SimUpdateProcessor>() is { } updateProcessor && updateProcessor.TryGetSimulationOf(this, out var sim))
                return sim;

            return null;
        }
    }

    /// <summary>
    /// Called before the simulation updates
    /// </summary>
    /// <param name="simulation"> The simulation this <see cref="ISimulationUpdate"/> is bound to, and the one currently updating </param>
    /// <param name="simTimeStep"> The amount of time in seconds this simulation lasts for </param>
    void SimulationUpdate(BepuSimulation simulation, float simTimeStep);

    /// <summary>
    /// Called after the simulation updates
    /// </summary>
    /// <param name="simulation"> The simulation this <see cref="ISimulationUpdate"/> is bound to, and the one currently updating </param>
    /// <param name="simTimeStep"> The amount of time in seconds this simulation lasts for </param>
    void AfterSimulationUpdate(BepuSimulation simulation, float simTimeStep);

    public void NotifySimulationChanged()
    {
        Entity.EntityManager?.Services.GetService<SimUpdateProcessor>()?.RebindSimulation(this);
    }

    public class SimUpdateProcessor : IProcessor
    {
        private IServiceRegistry? _services;
        private BepuConfiguration _config = null!;
        private Dictionary<ISimulationUpdate, BepuSimulation> _simulations = new();

        public void SystemAdded(IServiceRegistry registryParam)
        {
            _services = registryParam;
            registryParam.AddService(this);
            ServicesHelper.LoadBepuServices(registryParam, out _config, out _, out _);
        }

        public void SystemRemoved()
        {
            _services!.RemoveService<SimUpdateProcessor>();
        }

        public void RebindSimulation(ISimulationUpdate item)
        {
            OnComponentRemoved(item);
            OnComponentAdded(item);
        }

        public void OnComponentAdded(ISimulationUpdate item)
        {
            if (item is CollidableComponent)
                return; // Handled through the CollidableComponentProcessor

            var sim = item.SimulationSelector.Pick(_config, item.Entity);
            _simulations.Add(item, sim);
            sim.Register(item);
        }

        public void OnComponentRemoved(ISimulationUpdate item)
        {
            if (item is CollidableComponent)
                return; // Handled through the CollidableComponentProcessor

            if (_simulations.Remove(item, out var sim))
                sim.Unregister(item);
        }

        public bool TryGetSimulationOf(ISimulationUpdate item, [MaybeNullWhen(false)] out BepuSimulation sim) => _simulations.TryGetValue(item, out sim);
    }
}
