// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Input
{
    internal class GyroscopeSensor : Sensor, IGyroscopeSensor
    {
        public Vector3 RotationRate { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GyroscopeSensor"/> class.
        /// </summary>
        public GyroscopeSensor(IInputSource source, string systemName) : base(source, systemName, "Gyroscope")
        {
        }
    }
}