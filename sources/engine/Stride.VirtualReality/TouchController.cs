// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
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

        public enum ControllerHaptics
        {
            None,
            Limited,
            LimitedFrequency,
            LimitedAmplitude,
            Full
        }
        /// <summary>
        /// Degree to which this touch controller type supports haptics.
        /// None: no haptics support, controller does not vibrate.
        /// Limited: cannot vibrate at any specific frequency or amplitude. Corresponding parameter is ignored.
        /// Full: vibrate method respects both frequency and vibration parameters
        /// </summary>
        public abstract ControllerHaptics HapticsSupport { get; }

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
        /// Vibrate the controller for a fixed duration. Do so at specified frequency/amplitude if supported by runtime.
        /// Oculus runtime supports vibrating at frequency 0.0, 0.5, or 1.0, and amplitude in range [0.0, 1.0]
        /// openVR supports vibrating, but does not support frequency or amplitude
        /// openXR and WindowsMixedReality currently do not support vibration.
        /// </summary>
        /// <param name="durationMs">Vibration duration, in milliseconds</param>
        /// <param name="frequency">Frequency of vibration in range [0.0, 1.0]</param>
        /// <param name="amplitude">Amplitude of vibration in range [0.0, 1.0]</param>
        /// <returns></returns>
        public abstract Task Vibrate(int durationMs, float frequency = 1, float amplitude = 1);

        public virtual void Dispose()
        {
        }
    }
}
