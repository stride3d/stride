using System;
using System.Collections.Generic;
using BepuPhysicIntegrationTest.Integration.Components.Simulations;
using BepuPhysicIntegrationTest.Integration.Processors;
using BepuPhysics;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace BepuPhysicIntegrationTest.Integration.Components.Utils
{
    public abstract class SimulationUpdateComponent : SyncScript
    {
        private SimulationComponent _bepuSimulation = null;

        /// <summary>
        /// Get or set the SimulationComponent. If set null, it will try to find it in this or parent entities
        /// </summary>
        public SimulationComponent BepuSimulation
        {
            get => _bepuSimulation ?? Entity.GetInMeOrParents<SimulationComponent>();
            set => _bepuSimulation = value;
        }

        public override void Start()
        {
            base.Start();
            BepuSimulation.Register(this);
        }
        public override void Cancel()
        {
            base.Cancel();
            BepuSimulation.Unregister(this);
        }

        public abstract void SimulationUpdate(float simTimeStep);
    }
}
