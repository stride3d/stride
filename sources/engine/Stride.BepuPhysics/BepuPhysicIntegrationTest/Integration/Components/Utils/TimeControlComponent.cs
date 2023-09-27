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
        public SimulationComponent SimulationComponent { get; set; }
        public override void Update()
        {
            if (SimulationComponent == null)
                return;

            if (Input.IsKeyPressed(Keys.Add))
            {
                SimulationComponent.TimeWrap *= 1.1f;
            }
            if (Input.IsKeyPressed(Keys.Subtract))
            {
                SimulationComponent.TimeWrap /= 1.1f;
            }
            if (Input.IsKeyPressed(Keys.Multiply))
            {
                SimulationComponent.Enabled = !SimulationComponent.Enabled;
            }

            DebugText.Print($"Physic Enabled : {SimulationComponent.Enabled} (Numpad *)", new(Extensions.X_TEXT_POS, 225));
            DebugText.Print($"Time multiplicator : {SimulationComponent.TimeWrap} (numpad + & -)", new(Extensions.X_TEXT_POS, 250));
        }
    }
}
