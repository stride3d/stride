using System;
using System.Collections.Generic;
using BepuPhysicIntegrationTest.Integration.Processors;
using BepuPhysics;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace BepuPhysicIntegrationTest.Integration.Components.Simulations
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(SimulationProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Simulations")]
    public class SimulationComponent : EntityComponent
    {
        internal ThreadDispatcher ThreadDispatcher { get; private set; }
        internal BufferPool BufferPool { get; private set; }
        internal Simulation Simulation { get; private set; }

        internal List<(BodyHandle handle, Entity entity)> Bodies { get; } = new(Extensions.LIST_SIZE);
        internal List<(StaticHandle handle, Entity entity)> Statics { get; } = new(Extensions.LIST_SIZE);

        //Not working in editor since i'm using it in constructor !!!
        [Display(0, "SpringFreq")]
        public float SpringFreq { get; init; } = 30f;
        [Display(1, "SpringDamping")]
        public float SpringDamping { get; init; } = 3f;

        [Display(2, "PoseGravity")]
        public Vector3 PoseGravity { get; init; } = new Vector3(0, -10, 0);
        [Display(3, "PoseLinearDamping")]
        public float PoseLinearDamping { get; init; } = 0.1f;
        [Display(4, "PoseAngularDamping")]
        public float PoseAngularDamping { get; init; } = 0.5f;

        //This work in editor
        [Display(5, "SolveIteration")]
        public int SolveIteration
        {
            get => Simulation.Solver.VelocityIterationCount;
            init => Simulation.Solver.VelocityIterationCount = value;
        }

        [Display(6, "SolveSubStep")]
        public int SolveSubStep
        {
            get => Simulation.Solver.SubstepCount;
            init => Simulation.Solver.SubstepCount = value;
        }       
        public SimulationComponent()
        {
            var targetThreadCount = Math.Max(1, Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount - 1);
            ThreadDispatcher = new ThreadDispatcher(targetThreadCount);
            BufferPool = new BufferPool();
            Simulation = Simulation.Create(BufferPool, new StrideNarrowPhaseCallbacks(
                new SpringSettings(SpringFreq, SpringDamping)),
                new StridePoseIntegratorCallbacks(PoseGravity.ToNumericVector(), PoseLinearDamping, PoseAngularDamping),
                new SolveDescription(2, 4)); //4, 8
        }

    }
}
