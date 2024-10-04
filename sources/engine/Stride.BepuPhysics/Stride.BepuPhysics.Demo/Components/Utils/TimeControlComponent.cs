// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Engine;
using Stride.Input;

namespace Stride.BepuPhysics.Demo.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("BepuDemo - Utils")]
    public class TimeControlComponent : SyncScript
    {
        private BepuSimulation? _bepuSimulation { get; set; }

        public int SimulationIndex { get; set; } = 0;

        public override void Start()
        {
            _bepuSimulation = Services.GetSafeServiceAs<BepuConfiguration>().BepuSimulations[SimulationIndex];
        }

        public override void Update()
        {
            if (_bepuSimulation == null)
                return;

            if (Input.IsKeyPressed(Keys.Add))
            {
                _bepuSimulation.TimeScale *= 1.1f;
            }
            if (Input.IsKeyPressed(Keys.Subtract))
            {
                _bepuSimulation.TimeScale /= 1.1f;
            }

            if (Input.IsKeyPressed(Keys.Multiply))
            {
                _bepuSimulation.Enabled = !_bepuSimulation.Enabled;
            }


            if (Input.IsKeyPressed(Keys.O))
            {
                _bepuSimulation.PoseGravity += new Core.Mathematics.Vector3(0, 1, 0);
            }
            if (Input.IsKeyPressed(Keys.L))
            {
                _bepuSimulation.PoseGravity -= new Core.Mathematics.Vector3(0, 1, 0);
            }

            DebugText.Print($"Physic Enabled : {_bepuSimulation.Enabled} (Numpad *)", new(Game.Window.PreferredWindowedSize.X - 500, 225));
            DebugText.Print($"Time scale : {_bepuSimulation.TimeScale} (numpad + & -)", new(Game.Window.PreferredWindowedSize.X - 500, 250));
            DebugText.Print($"Gravity : {_bepuSimulation.PoseGravity} (numpad o & l)", new(Game.Window.PreferredWindowedSize.X - 500, 275));
        }
    }
}
