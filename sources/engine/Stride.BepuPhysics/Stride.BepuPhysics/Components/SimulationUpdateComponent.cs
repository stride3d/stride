// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Engine;

namespace Stride.BepuPhysics.Components;

public abstract class SimulationUpdateComponent : SyncScript, ISimulationUpdate
{
    private BepuSimulation? _simulation;
    private ISimulationSelector _simulationSelector = SceneBasedSimulationSelector.Shared;

    /// <summary>
    /// How the simulation this component belongs to and updates with is selected
    /// </summary>
    [DefaultValueIsSceneBased]
    public ISimulationSelector SimulationSelector
    {
        get
        {
            return _simulationSelector;
        }
        set
        {
            _simulationSelector = value;
            if (_simulation is not null)
            {
                _simulation.Unregister(this);
                _simulation = null;
                _simulation = SimulationSelector.Pick(Services.GetSafeServiceAs<BepuConfiguration>(), Entity);
            }
        }
    }

    /// <summary>
    /// Return the simulation this component uses
    /// </summary>
    /// <remarks>
    /// Depends on the <see cref="SimulationSelector"/> set
    /// </remarks>
    [DataMemberIgnore]
    public BepuSimulation Simulation
    {
        get
        {
            if (_simulation is not null)
                return _simulation;

            ServicesHelper.LoadBepuServices(Services, out var config, out _, out _);
            _simulation = SimulationSelector.Pick(config, Entity);
            return _simulation;
        }
    }

    /// <inheritdoc/>
    public override void Start()
    {
        base.Start();
        ServicesHelper.LoadBepuServices(Services, out var config, out _, out _);
        _simulation = SimulationSelector.Pick(config, Entity);
        _simulation.Register(this);
    }

    /// <inheritdoc/>
    public override void Cancel()
    {
        base.Cancel();
        _simulation?.Unregister(this);
        _simulation = null;
    }

    /// <inheritdoc cref="ISimulationUpdate.SimulationUpdate"/>
    public abstract void SimulationUpdate(float simTimeStep);

    /// <inheritdoc cref="ISimulationUpdate.AfterSimulationUpdate"/>
    public abstract void AfterSimulationUpdate(float simTimeStep);
}
