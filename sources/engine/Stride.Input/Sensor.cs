// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Stride.Input
{
    /// <summary>
    /// Describes a sensor that implements Enabled/Disable and provides a name/guid set from constructor
    /// </summary>
    internal class Sensor : ISensorDevice
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Sensor"/> class.
        /// </summary>
        protected Sensor(IInputSource source, string systemName, string sensorName)
        {
            Source = source;
            Name = $"{systemName} {sensorName} Sensor";
            Id = InputDeviceUtils.DeviceNameToGuid(systemName + sensorName);
        }

        public string Name { get; }

        public Guid Id { get; }

        public int Priority { get; set; }

        public IInputSource Source { get; }

        public bool IsEnabled { get; set; }

        public void Update(List<InputEvent> inputEvents)
        {
        }

        public virtual void Dispose()
        {
        }
    }
}