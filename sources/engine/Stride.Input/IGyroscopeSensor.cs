// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Input
{
    /// <summary>
    /// This class represents a sensor of type Gyroscope. It measures the rotation speed of device along the x/y/z axis.
    /// </summary>
    public interface IGyroscopeSensor : ISensorDevice
    {
        /// <summary>
        /// Gets the current rotation speed of the device along x/y/z axis.
        /// </summary>
        Vector3 RotationRate { get; }
    }
}