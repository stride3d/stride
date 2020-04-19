// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Input
{
    internal class GravitySensor : Sensor, IGravitySensor
    {
        public Vector3 Vector { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GravitySensor"/> class.
        /// </summary>
        public GravitySensor(IInputSource source, string systemName) : base(source, systemName, "Gravity")
        {
        }
    }
}