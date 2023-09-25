using System;
using System.Collections.Generic;
using System.Numerics;
using BepuPhysicIntegrationTest.Integration.Processors;
using BepuPhysics;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace BepuPhysicIntegrationTest.Integration.Components.Simulations
{
    [DefaultEntityComponentProcessor(typeof(SimulationProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Simulations")]
    public class SimulationComponent : SyncScript
    {
        internal ThreadDispatcher ThreadDispatcher { get; }
        internal BufferPool BufferPool { get; }
        internal Simulation Simulation { get; }

        internal List<(BodyHandle handle, Entity entity)> Bodies { get; } = new();
        internal List<(StaticHandle handle, Entity entity)> Statics { get; } = new();


        public SimulationComponent()
        {
            var targetThreadCount = Math.Max(1, Environment.ProcessorCount > 4 ? Environment.ProcessorCount - 2 : Environment.ProcessorCount - 1);
            ThreadDispatcher = new ThreadDispatcher(targetThreadCount);
            BufferPool = new BufferPool();
            Simulation = Simulation.Create(BufferPool, new StrideNarrowPhaseCallbacks(new SpringSettings(30, 3)), new StridePoseIntegratorCallbacks(new Vector3(0, -10, 0), 0.1f, 0.5f), new SolveDescription(4, 8));//new SolveDescription(64, 8));
        }

        public override void Update()
        {
        }
    }
}
