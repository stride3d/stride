using BepuPhysicIntegrationTest.Integration.Components.Utils;
using BepuPhysics;
using BepuPhysics.Constraints;
using BepuUtilities;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Data;
using Stride.Engine;
using System.Collections.Generic;
using System;
using BepuUtilities.Memory;

namespace BepuPhysicIntegrationTest.Integration.Configurations;
[DataContract]
[Display("Bepu Settings")]
public class BepuConfiguration : Configuration
{
	public List<BepuSimulation> BepuSimulations = new();
}

[DataContract]
public class BepuSimulation
{
	private List<SimulationUpdateComponent> _simulationUpdateComponents = new();
	
	internal ThreadDispatcher ThreadDispatcher { get; private set; }
	internal BufferPool BufferPool { get; private set; }

	internal StrideNarrowPhaseCallbacks StrideNarrowPhaseCallbacks { get; private set; }
	internal StridePoseIntegratorCallbacks StridePoseIntegratorCallbacks { get; private set; }
	internal SolveDescription SolveDescription { get; private set; }


	internal Simulation Simulation { get; private set; }
	internal bool Destroyed { get; set; } = false;

	internal Dictionary<BodyHandle, Entity> Bodies { get; } = new(Extensions.LIST_SIZE);
	internal Dictionary<StaticHandle, Entity> Statics { get; } = new(Extensions.LIST_SIZE);

	[Display(0, "TimeWrap")]
	public bool Enabled { get; set; } = true;
	[Display(1, "TimeWrap")]
	public float TimeWrap { get; set; } = 1f;


	//Not working in editor since i'm using it in constructor !!! (i just need to replace autoproperty to StrideNarrowPhaseCallbacks...)
	[Display(10, "SpringFreq")]
	public float SpringFreq { get; init; } = 30f;
	[Display(11, "SpringDamping")]
	public float SpringDamping { get; init; } = 3f;

	[Display(12, "PoseGravity")]
	public Vector3 PoseGravity { get; init; } = new Vector3(0, -10, 0);
	[Display(13, "PoseLinearDamping")]
	public float PoseLinearDamping { get; init; } = 0.1f;
	[Display(14, "PoseAngularDamping")]
	public float PoseAngularDamping { get; init; } = 0.5f;

	//This work in editor
	[Display(15, "SolveIteration")]
	public int SolveIteration
	{
		get => Simulation.Solver.VelocityIterationCount;
		init => Simulation.Solver.VelocityIterationCount = value;
	}

	[Display(16, "SolveSubStep")]
	public int SolveSubStep
	{
		get => Simulation.Solver.SubstepCount;
		init => Simulation.Solver.SubstepCount = value;
	}

	public BepuSimulation()
	{
		var targetThreadCount = Math.Max(1, Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount - 1);
		ThreadDispatcher = new ThreadDispatcher(targetThreadCount);
		BufferPool = new BufferPool();

		StrideNarrowPhaseCallbacks = new StrideNarrowPhaseCallbacks(new SpringSettings(SpringFreq, SpringDamping));
		StridePoseIntegratorCallbacks = new StridePoseIntegratorCallbacks(PoseGravity.ToNumericVector(), PoseLinearDamping, PoseAngularDamping);
		SolveDescription = new SolveDescription(2, 4); //4, 8

		Simulation = Simulation.Create(BufferPool, StrideNarrowPhaseCallbacks, StridePoseIntegratorCallbacks, SolveDescription);
	}

	internal void Destroy()
	{
		Bodies.Clear();
		Simulation.Dispose();
		ThreadDispatcher.Dispose();
		BufferPool.Clear();
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
