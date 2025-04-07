// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core;

#warning I can see this being useful, but it would have to be far more flexible and probably its own project, so maybe move this to demo/sample for now

namespace Stride.BepuPhysics.Demo.Car
{
    [DataContract]
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
            new() { AccelerationForce = 0.0900f, GearRatio = 0.006f },
            new() { AccelerationForce = 0.0950f, GearRatio = 0.012f },
            new() { AccelerationForce = 0.1000f, GearRatio = 0.016f }
        }; //0 => reverse, 1 => first, ..

    }
}
