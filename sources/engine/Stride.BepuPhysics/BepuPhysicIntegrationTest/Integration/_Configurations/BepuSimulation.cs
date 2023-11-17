using System;
using System.Collections.Generic;
using BepuPhysicIntegrationTest.Integration.Components.Utils;
using BepuPhysics;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace BepuPhysicIntegrationTest.Integration.Configurations;

[DataContract]
public class BepuSimulation
{
    private readonly List<SimulationUpdateComponent> _simulationUpdateComponents = new();

    internal BufferPool BufferPool { get; set; }
    internal ThreadDispatcher ThreadDispatcher { get; set; }
    internal Simulation Simulation { get; private set; }
    internal Dictionary<BodyHandle, Entity> Bodies { get; } = new(BepuAndStrideExtensions.LIST_SIZE);
    internal Dictionary<StaticHandle, Entity> Statics { get; } = new(BepuAndStrideExtensions.LIST_SIZE);
    internal float RemainingUpdateTime { get; set; } = 0;


    [Display(0, "Enabled")]
    public bool Enabled { get; set; } = true;
    [Display(1, "TimeWarp")]
    public float TimeWarp { get; set; } = 1f;

    [Display(10, "SpringFreq")]
    public float SpringFreq
    {
        get => ((NarrowPhase<StrideNarrowPhaseCallbacks>)Simulation.NarrowPhase).Callbacks.ContactSpringiness.Frequency;
        set => ((NarrowPhase<StrideNarrowPhaseCallbacks>)Simulation.NarrowPhase).Callbacks.ContactSpringiness.Frequency = value;
    }
    [Display(11, "SpringDamping")]
    public float SpringDamping
    {
        get => ((NarrowPhase<StrideNarrowPhaseCallbacks>)Simulation.NarrowPhase).Callbacks.ContactSpringiness.DampingRatio;
        set => ((NarrowPhase<StrideNarrowPhaseCallbacks>)Simulation.NarrowPhase).Callbacks.ContactSpringiness.DampingRatio = value;
    }


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
    public float SimulationFixedStep { get; set; } = 1000/120;
    [Display(32, "Max steps/frame")]
    public int MaxStepPerFrame { get; set; } = 3;

    public BepuSimulation()
    {
        var targetThreadCount = Math.Max(1, Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount - 1);
        ThreadDispatcher = new ThreadDispatcher(targetThreadCount);
        Setup();
    }
    private void Setup()
    {
        var _strideNarrowPhaseCallbacks = new StrideNarrowPhaseCallbacks(new SpringSettings(30, 3));
        var _stridePoseIntegratorCallbacks = new StridePoseIntegratorCallbacks();
        var _solveDescription = new SolveDescription(1, 1);

        BufferPool = new BufferPool();
        Simulation = Simulation.Create(BufferPool, _strideNarrowPhaseCallbacks, _stridePoseIntegratorCallbacks, _solveDescription);
    }
    internal void Clear()
    {
        //TODO : Check if something else should be clear
        //Warning, calling this can lead to exceptions if there are entities with Bepu components since the ref is destroyed.
        Simulation.Clear();
        BufferPool.Clear();
        Bodies.Clear();
        Statics.Clear();
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
