// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Input
{
    internal class AccelerometerSensor : Sensor, IAccelerometerSensor
    {
        public Vector3 Acceleration { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccelerometerSensor"/> class.
        /// </summary>
        public AccelerometerSensor(IInputSource source, string systemName) : base(source, systemName, "Accelerometer")
        {
        }
    }
}