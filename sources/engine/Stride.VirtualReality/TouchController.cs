// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Games;

namespace Stride.VirtualReality
{
    public abstract class TouchController : IDisposable
    {
        public abstract Vector3 Position { get; }

        public abstract Quaternion Rotation { get; }

        public abstract Vector3 LinearVelocity { get; }

        public abstract Vector3 AngularVelocity { get; }

        public abstract DeviceState State { get; }

        public virtual void Update(GameTime time)
        {
        }

        public abstract float Trigger { get; }

        public abstract float Grip { get; }

        public abstract bool IndexPointing { get; }

        public abstract bool IndexResting { get; }

        public abstract bool ThumbUp { get; }

        public abstract bool ThumbResting { get; }

        public abstract Vector2 ThumbAxis { get; }

        public abstract Vector2 ThumbstickAxis { get; }
        //Number of concurrent calls to Vibrate(duration) so that the longest will complete.
        int _vibrationCounter;

        /// <summary>
        /// Returns true if in this frame the button switched to pressed state
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        public abstract bool IsPressedDown(TouchControllerButton button);

        /// <summary>
        /// Returns true if button switched is in the pressed state
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        public abstract bool IsPressed(TouchControllerButton button);

        /// <summary>
        /// Returns true if in this frame the button was released
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        public abstract bool IsPressReleased(TouchControllerButton button);

        /// <summary>
        /// Returns true if in this frame the button switched to pressed state
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        public abstract bool IsTouchedDown(TouchControllerButton button);

        /// <summary>
        /// Returns true if button switched is in the pressed state
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        public abstract bool IsTouched(TouchControllerButton button);

        /// <summary>
        /// Returns true if in this frame the button was released
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        public abstract bool IsTouchReleased(TouchControllerButton button);

        /// <summary>
        /// Vibrate the controller for a fixed duration. Stops vibration at end of duration if no other Vibrate calls are currently running.
        /// </summary>
        /// <param name="durationMs">Vibration duration, in milliseconds</param>
        /// <returns></returns>
        public async Task Vibrate(int durationMs)
        {
            Vibrate();
            await Task.Delay(durationMs);
            Interlocked.Decrement(ref _vibrationCounter);
            if (_vibrationCounter <= 0)
                await StopVibration();
        }
        /// <summary>
        /// Vibrate the controller until StopVibration() is called
        /// </summary>
        /// <returns></returns>
        public async void Vibrate()
        {
            Interlocked.Increment(ref _vibrationCounter);
            while (_vibrationCounter > 0)
                await EnableVibration();
        }
        /// <summary>
        /// Stop the controller's vibration
        /// </summary>
        /// <returns>A Task which completes when controller vibration is stopped</returns>
        public async Task StopVibration()
        {
            _vibrationCounter = 0;
            await DisableVibration();
        }
        /// <summary>
        /// Enable controller vibration for a period of time
        /// </summary>
        /// <returns>A Task which completes when the vibration times out</returns>
        protected abstract Task EnableVibration();
        /// <summary>
        /// Disable controller vibration
        /// </summary>
        /// <returns>A Task which completes when the controller vibration has stopped.</returns>

        protected abstract Task DisableVibration();
        public virtual void Dispose()
        {
        }
    }
}
