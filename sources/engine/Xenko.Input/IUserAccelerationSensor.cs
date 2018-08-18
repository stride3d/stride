// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;

namespace Xenko.Input
{
    /// <summary>
    /// This class represents a sensor of type user acceleration. It measures the acceleration applied by the user (no gravity) onto the device.
    /// </summary>
    public interface IUserAccelerationSensor : ISensorDevice
    {
        /// <summary>
        /// Gets the current acceleration applied by the user (in meters/seconds^2).
        /// </summary>
        Vector3 Acceleration { get; }
    }
}
