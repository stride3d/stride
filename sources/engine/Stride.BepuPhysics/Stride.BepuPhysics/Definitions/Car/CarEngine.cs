using System.Collections.Generic;
using Stride.Core;

namespace Stride.BepuPhysics.Definitions.Car
{
    public class CarEngine
    {
        public int MinRPM { get; set; } = 1000;
        public int MaxRPM { get; set; } = 10000;
        public float EngineBreakForce { get; set; } = 0.001f;

        [DataMemberIgnore] //Editor bug
        public List<CarEngineGear> Gears { get; set; } = new List<CarEngineGear>() {
            new() { AccelerationForce = 0.0300f, GearRatio = -0.001f },

            new() { AccelerationForce = 0.0450f, GearRatio = 0.0012f },
            new() { AccelerationForce = 0.0800f, GearRatio = 0.003f },
            new() { AccelerationForce = 0.0800f, GearRatio = 0.006f },
            new() { AccelerationForce = 0.0750f, GearRatio = 0.012f },
            new() { AccelerationForce = 0.0600f, GearRatio = 0.016f }
        }; //0 => reverse, 1 => first, ..

    }
}
