// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Input
{
    internal class UserAccelerationSensor : Sensor, IUserAccelerationSensor
    {
        public Vector3 Acceleration { get; internal set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="UserAccelerationSensor"/> class.
        /// </summary>
        public UserAccelerationSensor(IInputSource source, string systemName) : base(source, systemName, "User Acceleration")
        {
        }
    }
}