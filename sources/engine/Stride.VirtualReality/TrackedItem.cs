// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Mathematics;
using Stride.Games;

namespace Stride.VirtualReality
{
    public abstract class TrackedItem : IDisposable
    {
        public abstract Vector3 Position { get; }

        public abstract Quaternion Rotation { get; }

        public abstract Vector3 LinearVelocity { get; }

        public abstract Vector3 AngularVelocity { get; }

        public abstract DeviceState State { get; }

        public abstract DeviceClass Class { get; }

        public abstract string SerialNumber { get; }

        public abstract float BatteryPercentage { get; }

        public virtual void Update(GameTime time)
        {           
        }

        public virtual void Dispose()
        {          
        }
    }
}
