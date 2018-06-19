// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;

namespace Xenko.Input
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