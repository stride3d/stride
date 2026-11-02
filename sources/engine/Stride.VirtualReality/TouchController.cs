// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Input;

namespace Stride.VirtualReality
{
    /// <summary>
    /// A VR hand controller. It is an input device: an owner such as a VR menu can capture it, and while it is captured
    /// its buttons, touches and axes report no input to scripts and systems. Its pose and haptics are not affected.
    /// </summary>
    /// <remarks>
    /// Backends provide the controller's input through the protected raw members. The public queries apply the
    /// controller's <see cref="CaptureState"/> to them.
    /// </remarks>
    public abstract class TouchController : IInputDevice, IDisposable
    {
        private static readonly TouchControllerButton[] Buttons = Enum.GetValues<TouchControllerButton>();

        private readonly HashSet<TouchControllerButton> pressedButtons = new();
        private readonly HashSet<TouchControllerButton> pressedDownButtons = new();
        private readonly HashSet<TouchControllerButton> pressReleasedButtons = new();
        private readonly HashSet<TouchControllerButton> touchedButtons = new();
        private readonly HashSet<TouchControllerButton> touchedDownButtons = new();
        private readonly HashSet<TouchControllerButton> touchReleasedButtons = new();
        private readonly ButtonCaptureTracker<TouchControllerButton> pressTracker;
        private readonly ButtonCaptureTracker<TouchControllerButton> touchTracker;
        private IInputSource source;

        // Until the input manager updates the controller, the button queries report the backend's input as it is
        private bool sampled;

        protected TouchController()
        {
            pressTracker = CaptureState.TrackButtons(new ReadOnlySet<TouchControllerButton>(pressedButtons), new ReadOnlySet<TouchControllerButton>(pressedDownButtons), new ReadOnlySet<TouchControllerButton>(pressReleasedButtons));
            touchTracker = CaptureState.TrackButtons(new ReadOnlySet<TouchControllerButton>(touchedButtons), new ReadOnlySet<TouchControllerButton>(touchedDownButtons), new ReadOnlySet<TouchControllerButton>(touchReleasedButtons));
        }

        public abstract Vector3 Position { get; }

        public abstract Quaternion Rotation { get; }

        public abstract Vector3 LinearVelocity { get; }

        public abstract Vector3 AngularVelocity { get; }

        public abstract DeviceState State { get; }

        public virtual void Update(GameTime time)
        {
        }

        public float Trigger => CaptureState.IsMasked ? 0.0f : RawTrigger;

        public float Grip => CaptureState.IsMasked ? 0.0f : RawGrip;

        public bool IndexPointing => !CaptureState.IsMasked && RawIndexPointing;

        public bool IndexResting => !CaptureState.IsMasked && RawIndexResting;

        public bool ThumbUp => !CaptureState.IsMasked && RawThumbUp;

        public bool ThumbResting => !CaptureState.IsMasked && RawThumbResting;

        public Vector2 ThumbAxis => CaptureState.IsMasked ? Vector2.Zero : RawThumbAxis;

        public Vector2 ThumbstickAxis => CaptureState.IsMasked ? Vector2.Zero : RawThumbstickAxis;

        /// <summary>
        /// Gets the trigger value reported by the backend, whether or not the controller is captured.
        /// </summary>
        protected abstract float RawTrigger { get; }

        /// <summary>
        /// Gets the grip value reported by the backend, whether or not the controller is captured.
        /// </summary>
        protected abstract float RawGrip { get; }

        protected abstract bool RawIndexPointing { get; }

        protected abstract bool RawIndexResting { get; }

        protected abstract bool RawThumbUp { get; }

        protected abstract bool RawThumbResting { get; }

        protected abstract Vector2 RawThumbAxis { get; }

        protected abstract Vector2 RawThumbstickAxis { get; }

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
        public bool IsPressedDown(TouchControllerButton button) => !sampled ? IsRawPressedDown(button) : pressTracker.Pressed.Contains(button);

        /// <summary>
        /// Returns true if button is in the pressed state
        /// </summary>
        public bool IsPressed(TouchControllerButton button) => !sampled ? IsRawPressed(button) : pressTracker.Down.Contains(button);

        /// <summary>
        /// Returns true if in this frame the button was released
        /// </summary>
        public bool IsPressReleased(TouchControllerButton button) => !sampled ? IsRawPressReleased(button) : pressTracker.Released.Contains(button);

        /// <summary>
        /// Returns true if in this frame the button switched to touched state
        /// </summary>
        public bool IsTouchedDown(TouchControllerButton button) => !sampled ? IsRawTouchedDown(button) : touchTracker.Pressed.Contains(button);

        /// <summary>
        /// Returns true if button is in the touched state
        /// </summary>
        public bool IsTouched(TouchControllerButton button) => !sampled ? IsRawTouched(button) : touchTracker.Down.Contains(button);

        /// <summary>
        /// Returns true if in this frame the button stopped being touched
        /// </summary>
        public bool IsTouchReleased(TouchControllerButton button) => !sampled ? IsRawTouchReleased(button) : touchTracker.Released.Contains(button);

        /// <summary>
        /// Returns true if the backend reports that the button switched to the pressed state this frame.
        /// </summary>
        protected abstract bool IsRawPressedDown(TouchControllerButton button);

        /// <summary>
        /// Returns true if the backend reports the button in the pressed state.
        /// </summary>
        protected abstract bool IsRawPressed(TouchControllerButton button);

        /// <summary>
        /// Returns true if the backend reports that the button was released this frame.
        /// </summary>
        protected abstract bool IsRawPressReleased(TouchControllerButton button);

        /// <summary>
        /// Returns true if the backend reports that the button switched to the touched state this frame.
        /// </summary>
        protected abstract bool IsRawTouchedDown(TouchControllerButton button);

        /// <summary>
        /// Returns true if the backend reports the button in the touched state.
        /// </summary>
        protected abstract bool IsRawTouched(TouchControllerButton button);

        /// <summary>
        /// Returns true if the backend reports that the button stopped being touched this frame.
        /// </summary>
        protected abstract bool IsRawTouchReleased(TouchControllerButton button);

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

        /// <inheritdoc />
        public virtual string Name => GetType().Name;

        /// <inheritdoc />
        public Guid Id { get; } = Guid.NewGuid();

        /// <inheritdoc />
        public int Priority { get; set; }

        /// <summary>
        /// Gets the input source the controller is registered with, or <c>null</c> before the VR system registers it.
        /// </summary>
        public IInputSource Source
        {
            get => source;
            internal set
            {
                source = value;
                if (value == null)
                    sampled = false;
            }
        }

        /// <inheritdoc />
        public DeviceCaptureState CaptureState { get; } = new DeviceCaptureState();

        // Called by the input manager once per frame, after the VR system has updated the controller
        void IInputDevice.Update(List<InputEvent> inputEvents)
        {
            sampled = true;
            pressedButtons.Clear();
            pressedDownButtons.Clear();
            pressReleasedButtons.Clear();
            touchedButtons.Clear();
            touchedDownButtons.Clear();
            touchReleasedButtons.Clear();

            foreach (var button in Buttons)
            {
                if (IsRawPressed(button))
                    pressedButtons.Add(button);
                if (IsRawPressedDown(button))
                    pressedDownButtons.Add(button);
                if (IsRawPressReleased(button))
                    pressReleasedButtons.Add(button);
                if (IsRawTouched(button))
                    touchedButtons.Add(button);
                if (IsRawTouchedDown(button))
                    touchedDownButtons.Add(button);
                if (IsRawTouchReleased(button))
                    touchReleasedButtons.Add(button);
            }
        }

        public virtual void Dispose()
        {
        }
    }
}
