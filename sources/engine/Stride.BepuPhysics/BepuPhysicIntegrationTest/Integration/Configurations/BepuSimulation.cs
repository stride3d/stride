using System;
using System.Collections.Generic;
using BepuPhysicIntegrationTest.Integration.Components.Utils;
using BepuPhysics;
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

    //TODO : Maybe we could also expose the Structs direclty ? (I got some issues of serializer when I tried with others structs)
    private SpringSettings _contactSpringiness;
    private StrideNarrowPhaseCallbacks _strideNarrowPhaseCallbacks;
    private StridePoseIntegratorCallbacks _stridePoseIntegratorCallbacks;
    private SolveDescription _solveDescription;

    internal BufferPool BufferPool { get; set; }
    internal ThreadDispatcher ThreadDispatcher { get; set; }
    internal Simulation Simulation { get; private set; }
    internal Dictionary<BodyHandle, Entity> Bodies { get; } = new(BepuAndStrideExtensions.LIST_SIZE);
    internal Dictionary<StaticHandle, Entity> Statics { get; } = new(BepuAndStrideExtensions.LIST_SIZE);


    [Display(0, "Enabled")]
    public bool Enabled { get; set; } = true;
    [Display(1, "TimeWarp")]
    public float TimeWarp { get; set; } = 1f;


    [Display(10, "SpringFreq")]
    public float SpringFreq { get => _contactSpringiness.Frequency; set => _contactSpringiness.Frequency = value; }
    [Display(11, "SpringDamping")]
    public float SpringDamping { get => _contactSpringiness.DampingRatio; set => _contactSpringiness.DampingRatio = value; }


    [Display(12, "PoseGravity")]
    public Vector3 PoseGravity { get => _stridePoseIntegratorCallbacks.Gravity.ToStrideVector(); set => _stridePoseIntegratorCallbacks.Gravity = value.ToNumericVector(); }
    [Display(13, "PoseLinearDamping")]
    public float PoseLinearDamping { get => _stridePoseIntegratorCallbacks.LinearDamping; set => _stridePoseIntegratorCallbacks.LinearDamping = value; }
    [Display(14, "PoseAngularDamping")]
    public float PoseAngularDamping { get => _stridePoseIntegratorCallbacks.AngularDamping; set => _stridePoseIntegratorCallbacks.AngularDamping = value; }

    [Display(15, "SolveIteration")]
    public int SolveIteration { get => Simulation.Solver.VelocityIterationCount; init => Simulation.Solver.VelocityIterationCount = value; }

    [Display(16, "SolveSubStep")]
    public int SolveSubStep { get => Simulation.Solver.SubstepCount; init => Simulation.Solver.SubstepCount = value; }

    [Display(30, "Parallel update")]
    public bool Para { get; set; } = true;


    public BepuSimulation()
    {
        var targetThreadCount = Math.Max(1, Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount - 1);

        _contactSpringiness = new SpringSettings(30, 3);
        _strideNarrowPhaseCallbacks = new StrideNarrowPhaseCallbacks(_contactSpringiness);
        _stridePoseIntegratorCallbacks = new StridePoseIntegratorCallbacks();
        _solveDescription = new SolveDescription(2, 4); //4, 8

        BufferPool = new BufferPool();
        ThreadDispatcher = new ThreadDispatcher(targetThreadCount);
        Simulation = Simulation.Create(BufferPool, _strideNarrowPhaseCallbacks, _stridePoseIntegratorCallbacks, _solveDescription);
    }

    internal void Clear()
    {
        //TODO : Check if something else should be clear
        Simulation.Clear();
        Bodies.Clear();
        Statics.Clear();
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
