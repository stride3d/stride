// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Input
{
    internal class CompassSensor : Sensor, ICompassSensor
    {
        public float Heading { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompassSensor"/> class.
        /// </summary>
        public CompassSensor(IInputSource source, string systemName) : base(source, systemName, "Compass")
        {
        }
    }
}