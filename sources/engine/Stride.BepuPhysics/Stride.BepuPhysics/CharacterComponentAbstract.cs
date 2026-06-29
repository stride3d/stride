// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Systems.Characters;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics;

/// <summary>
/// Abstract barebone class one can inherit from to implement custom character logic
/// </summary>
public abstract class CharacterComponentAbstract : BodyComponent, ISimulationUpdate
{
    /// <inheritdoc/>
    protected override void AttachInner(NRigidPose pose, BodyInertia shapeInertia, TypedIndex shapeIndex)
    {
        base.AttachInner(pose, new BodyInertia { InverseMass = 1f }, shapeIndex);

        Debug.Assert(BodyReference.HasValue);
        Debug.Assert(Simulation is not null);

        Simulation.Characters.AllocateCharacter(BodyReference.Value.Handle);
    }

    /// <inheritdoc/>
    protected override void DetachInner()
    {
        Debug.Assert(BodyReference.HasValue);
        Debug.Assert(Simulation is not null);

        Simulation.Characters.RemoveCharacterByBodyHandle(BodyReference.Value.Handle);

        base.DetachInner();
    }

    void ISimulationUpdate.SimulationUpdate(BepuSimulation simulation, float simTimeStep)
    {
        if (BodyReference is null)
            return; // null when the collider couldn't attach

        ref var characterData = ref simulation.Characters.GetCharacterByBodyHandle(BodyReference.Value);
        SimulationUpdate(simulation, simTimeStep, ref characterData, BodyReference.Value, out bool awakeBody);

        if (awakeBody)
        {
            simulation.Characters.Simulation.Awakener.AwakenBody(characterData.BodyHandle);
        }
    }

    void ISimulationUpdate.AfterSimulationUpdate(BepuSimulation simulation, float simTimeStep)
    {
        if (BodyReference is null)
            return; // null when the collider couldn't attach

        ref var character = ref simulation.Characters.GetCharacterByBodyHandle(BodyReference.Value);
        AfterSimulationUpdate(simulation, simTimeStep, ref character);
    }

    /// <inheritdoc cref="ISimulationUpdate.SimulationUpdate"/>
    protected abstract void SimulationUpdate(BepuSimulation simulation, float simTimeStep, ref InternalCharacterData characterData, in BodyReference characterBody, out bool wakeupBody);

    /// <inheritdoc cref="ISimulationUpdate.AfterSimulationUpdate"/>
    protected abstract void AfterSimulationUpdate(BepuSimulation simulation, float simTimeStep, ref InternalCharacterData characterData);
}
