using System;
using System.Collections.Generic;
using BepuPhysicIntegrationTest.Integration.Components;
using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysicIntegrationTest.Integration.Extensions;
using BepuPhysics;
using BepuUtilities;
using BepuUtilities.Memory;
using Stride.Core;
using Stride.Core.Mathematics;
using static BepuPhysicIntegrationTest.Integration.StrideNarrowPhaseCallbacks;

namespace BepuPhysicIntegrationTest.Integration.Configurations;

[DataContract]
public class BepuSimulation
{
    private readonly List<SimulationUpdateComponent> _simulationUpdateComponents = new();

    internal ThreadDispatcher ThreadDispatcher { get; set; }
    internal BufferPool BufferPool { get; set; }
    internal Simulation Simulation { get; private set; }
    internal Dictionary<BodyHandle, BodyContainerComponent> BodiesContainers { get; } = new(BepuAndStrideExtensions.LIST_SIZE);
    internal Dictionary<StaticHandle, StaticContainerComponent> StaticsContainers { get; } = new(BepuAndStrideExtensions.LIST_SIZE);
    internal float RemainingUpdateTime { get; set; } = 0;

    internal CollidableProperty<MaterialProperties> CollidableMaterials = new CollidableProperty<MaterialProperties>();


    [Display(0, "Enabled")]
    public bool Enabled { get; set; } = true;
    [Display(1, "TimeWarp")]
    public float TimeWarp { get; set; } = 1f;

    [Display(12, "PoseGravity")]
    public Vector3 PoseGravity
    {
        get => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.Gravity.ToStrideVector();
        set => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.Gravity = value.ToNumericVector();
    }
    [Display(13, "PoseLinearDamping")]
    public float PoseLinearDamping
    {
        get => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.LinearDamping;
        set => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.LinearDamping = value;
    }
    [Display(14, "PoseAngularDamping")]
    public float PoseAngularDamping
    {
        get => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.AngularDamping;
        set => ((PoseIntegrator<StridePoseIntegratorCallbacks>)Simulation.PoseIntegrator).Callbacks.AngularDamping = value;
    }

    [Display(15, "SolveIteration")]
    public int SolveIteration { get => Simulation.Solver.VelocityIterationCount; init => Simulation.Solver.VelocityIterationCount = value; }

    [Display(16, "SolveSubStep")]
    public int SolveSubStep { get => Simulation.Solver.SubstepCount; init => Simulation.Solver.SubstepCount = value; }

    [Display(30, "Parallel update")]
    public bool ParallelUpdate { get; set; } = true;
    [Display(31, "Simulation fixed step")]
    public float SimulationFixedStep { get; set; } = 1000 / 60;
    [Display(32, "Max steps/frame")]
    public int MaxStepPerFrame { get; set; } = 3;

#pragma warning disable CS8618 //Done in setup to avoid 2 times the samecode.
    public BepuSimulation()
#pragma warning restore CS8618 
    {
        Setup();
    }
    private void Setup()
    {
        var targetThreadCount = Math.Max(1, Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount - 1);
        var _strideNarrowPhaseCallbacks = new StrideNarrowPhaseCallbacks() { CollidableMaterials = CollidableMaterials };
        var _stridePoseIntegratorCallbacks = new StridePoseIntegratorCallbacks();
        var _solveDescription = new SolveDescription(1, 1);

        ThreadDispatcher = new ThreadDispatcher(targetThreadCount);
        BufferPool = new BufferPool();
        Simulation = Simulation.Create(BufferPool, _strideNarrowPhaseCallbacks, _stridePoseIntegratorCallbacks, _solveDescription);
    }
    internal void Clear()
    {
        //Warning, calling this can lead to exceptions if there are entities with Bepu components since the ref is destroyed.
        BufferPool.Clear();
        BodiesContainers.Clear();
        StaticsContainers.Clear();
        Setup();
    }

    internal void CallSimulationUpdate(float simTimeStep)
    {
        _simulationUpdateComponents.ForEach(e => e.SimulationUpdate(simTimeStep));
    }

    internal void Register(SimulationUpdateComponent simulationUpdateComponent)
    {
        _simulationUpdateComponents.Add(simulationUpdateComponent);
    }
    internal void Unregister(SimulationUpdateComponent simulationUpdateComponent)
    {
        _simulationUpdateComponents.Remove(simulationUpdateComponent);
    }
}
