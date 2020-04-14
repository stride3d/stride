// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Input
{
    /// <summary>
    /// This class represents a sensor of type Accelerometer. It measures the acceleration forces (including gravity) applying on the device.
    /// </summary>
    public interface IAccelerometerSensor : ISensorDevice
    {
        /// <summary>
        /// Gets the current acceleration applied on the device (in meters/seconds^2).
        /// </summary>
        Vector3 Acceleration { get; }
    }
}