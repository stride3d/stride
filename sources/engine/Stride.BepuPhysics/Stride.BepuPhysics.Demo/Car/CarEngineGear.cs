// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

#warning I can see this being useful, but it would have to be far more flexible and probably its own project, so maybe move this to demo/sample for now

namespace Stride.BepuPhysics.Demo.Car
{
    [DataContract]
    public class CarEngineGear
    {
        public float AccelerationForce { get; set; }
        public float GearRatio { get; set; }

    }
}
