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
    [DefaultEntityComponentProcessor(typeof(SimulationProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Simulations")]
    public class SimulationComponent : StartupScript
    {
        internal ThreadDispatcher ThreadDispatcher { get; }
        internal BufferPool BufferPool { get; }
        internal Simulation Simulation { get; }

        internal List<(BodyHandle handle, Entity entity)> Bodies { get; } = new(Extensions.LIST_SIZE);
        internal List<(StaticHandle handle, Entity entity)> Statics { get; } = new(Extensions.LIST_SIZE);

        //Not working in editor since i'm using it in constructor !!!
        [Display(0, "SpringFreq")]
        public float SpringFreq = 30f;
        [Display(1, "SpringDamping")]
        public float SpringDamping = 3f;

        [Display(2, "PoseGravity")]
        public Vector3 PoseGravity = new Vector3(0, -10, 0);
        [Display(3, "PoseLinearDamping")]
        public float PoseLinearDamping = 0.1f;
        [Display(4, "PoseAngularDamping")]
        public float PoseAngularDamping = 0.5f;

        [Display(5, "SolveIteration")]
        public int SolveIteration = 2; //4
        [Display(6, "SolveSubStep")]
        public int SolveSubStep = 4; //8


        public override void Start()
        {

            base.Start();
        }

        public SimulationComponent()
        {
            var targetThreadCount = Math.Max(1, Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount - 1);
            ThreadDispatcher = new ThreadDispatcher(targetThreadCount);
            BufferPool = new BufferPool();
            Simulation = Simulation.Create(BufferPool, new StrideNarrowPhaseCallbacks(
                new SpringSettings(SpringFreq, SpringDamping)),
                new StridePoseIntegratorCallbacks(PoseGravity.ToNumericVector(), PoseLinearDamping, PoseAngularDamping),
                new SolveDescription(SolveIteration, SolveSubStep));
        }

    }
}
