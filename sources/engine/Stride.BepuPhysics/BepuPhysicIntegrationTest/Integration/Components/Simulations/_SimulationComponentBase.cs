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
    public abstract class SimulationComponentBase : SyncScript
    {
    }
}
