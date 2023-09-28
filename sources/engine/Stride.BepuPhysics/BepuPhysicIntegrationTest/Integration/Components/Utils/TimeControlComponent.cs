using System;
using System.Linq;
using System.Windows.Media.Media3D;
using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysicIntegrationTest.Integration.Components.Simulations;
using SharpDX.MediaFoundation;
using Silk.NET.OpenGL;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace BepuPhysicIntegrationTest.Integration.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("Bepu - Utils")]
    public class TimeControlComponent : SyncScript
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

        public override void Update()
        {
            if (BepuSimulation == null)
                return;

            if (Input.IsKeyPressed(Keys.Add))
            {
                BepuSimulation.TimeWrap *= 1.1f;
            }
            if (Input.IsKeyPressed(Keys.Subtract))
            {
                BepuSimulation.TimeWrap /= 1.1f;
            }
            if (Input.IsKeyPressed(Keys.Multiply))
            {
                BepuSimulation.Enabled = !BepuSimulation.Enabled;
            }

            DebugText.Print($"Physic Enabled : {BepuSimulation.Enabled} (Numpad *)", new(Extensions.X_DEBUG_TEXT_POS, 225));
            DebugText.Print($"Time multiplicator : {BepuSimulation.TimeWrap} (numpad + & -)", new(Extensions.X_DEBUG_TEXT_POS, 250));
        }
    }
}
