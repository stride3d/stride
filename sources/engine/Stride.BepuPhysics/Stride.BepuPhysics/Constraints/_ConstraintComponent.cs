// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using BepuPhysics;
using BepuPhysics.Constraints;

namespace Stride.BepuPhysics.Constraints;

public abstract class ConstraintComponent<T> : ConstraintComponentBase where T : unmanaged, IConstraintDescription<T>
{
    internal T BepuConstraint;
    private readonly List<BodyComponent> _attachedBodies = new();
    private ConstraintHandle _cHandle = new(-1);
    private BepuSimulation? _bepuSimulation;
    private BepuConfiguration? _bepuConfig;

    public override bool Attached => _cHandle.Value != -1;

    internal override void Activate(BepuConfiguration bepuConfig)
    {
        _bepuConfig = bepuConfig;

        foreach (var component in Bodies)
        {
            if (component is null)
                continue;

            component.BoundConstraints ??= new();
            component.BoundConstraints.Add(this);
            _attachedBodies.Add(component);
        }

        TryReattachConstraint();
    }

    internal override void Deactivate()
    {
        foreach (var component in Bodies)
        {
            if (component is null)
                continue;

            component.BoundConstraints ??= new();
            component.BoundConstraints.Add(this);
            _attachedBodies.Add(component);
        }

        DetachConstraint();
    }

    protected override void BodiesChanged()
    {
        foreach (var component in _attachedBodies)
        {
            Debug.Assert(component.BoundConstraints is not null);
            component.BoundConstraints.Remove(this);
        }
        _attachedBodies.Clear();

        foreach (var component in Bodies)
        {
            if (component is null)
                continue;

            component.BoundConstraints ??= new();
            component.BoundConstraints.Add(this);
            _attachedBodies.Add(component);
        }

        TryReattachConstraint();
    }

    internal override ConstraintState TryReattachConstraint()
    {
        DetachConstraint();

        if (_bepuConfig is null)
            return ConstraintState.ConstraintNotInScene;


        if (!Enabled)
            return ConstraintState.ConstraintDisabled;

        foreach (var component in Bodies)
        {
            if (component is null)
                return ConstraintState.BodyNull; // need to wait for a body to be attached or instanced

            if (component.BodyReference.HasValue == false)
                return ConstraintState.BodyNotInScene; // need to wait for a body to be attached or instanced
        }

        var newSimulation = Bodies[0]!.SimulationSelector.Pick(_bepuConfig, Bodies[0]!.Entity);

        Span<BodyHandle> bodies = stackalloc BodyHandle[Bodies.Length];
        int count = 0;

        foreach (var component in Bodies)
        {
            Debug.Assert(component is not null); // Both of these are handled in the other foreach above
            Debug.Assert(component.BodyReference.HasValue);

            if (ReferenceEquals(component.Simulation, newSimulation) == false)
            {
                int simIndex = _bepuConfig.BepuSimulations.IndexOf(newSimulation);
                string otherSimulation = component.Simulation == null ? "null" : _bepuConfig.BepuSimulations.IndexOf(component.Simulation).ToString();
                Logger.Warning($"A constraint between object with different Simulation is not possible ({this} @ #{simIndex} -> {component} @ #{otherSimulation})");
                return ConstraintState.SimulationMismatch;
            }

            bodies[count++] = component.BodyReference.Value.Handle;
        }

        _bepuSimulation = newSimulation;

        Span<BodyHandle> validBodies = bodies[..count];

        _cHandle = _bepuSimulation.Simulation.Solver.Add(validBodies, BepuConstraint);

        return ConstraintState.FullyOperational;
    }

    internal override void DetachConstraint()
    {
        if (_cHandle.Value != -1)
        {
            Debug.Assert(_bepuSimulation is not null);
            _bepuSimulation.Simulation.Solver.Remove(_cHandle);
            _cHandle = new(-1);
        }

        _bepuSimulation = null;
    }

    internal void TryUpdateDescription()
    {
        if (_bepuSimulation != null && _cHandle.Value != -1 && _bepuSimulation.Simulation.Solver.ConstraintExists(_cHandle))
        {
            _bepuSimulation.Simulation.Solver.ApplyDescription(_cHandle, BepuConstraint);
        }
    }

    protected ConstraintComponent(int bodies) : base(bodies) { }
}
