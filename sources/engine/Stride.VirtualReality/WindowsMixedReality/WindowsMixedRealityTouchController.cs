// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11 && STRIDE_PLATFORM_UWP

using Windows.Perception;
using Windows.Perception.Spatial;
using Windows.UI.Input.Spatial;
using Stride.Core.Mathematics;

namespace Stride.VirtualReality
{
    internal class WindowsMixedRealityTouchController : TouchController
    {
        private readonly SpatialInteractionSourceHandedness hand;
        private readonly SpatialInteractionManager interactionManager;

        private Vector3 currentAngularVelocity;
        private Vector3 currentLinearVelocity;
        private Vector3 currentPosition;
        private Quaternion currentRotation = Quaternion.Identity;
        private SpatialInteractionSourceState currentState;
        private DeviceState internalState;
        private SpatialInteractionSourceState previousState;

        internal WindowsMixedRealityTouchController(TouchControllerHand hand, SpatialInteractionManager interactionManager)
        {
            this.hand = (SpatialInteractionSourceHandedness)(hand + 1);
            this.interactionManager = interactionManager;

            interactionManager.SourceLost += InteractionManager_SourceLost;
        }

        public override Vector3 Position => currentPosition;

        public override Quaternion Rotation => currentRotation;

        public override Vector3 LinearVelocity => currentLinearVelocity;

        public override Vector3 AngularVelocity => currentAngularVelocity;

        public override DeviceState State => internalState;

        public override float Trigger => (float)currentState.SelectPressedValue;

        public override float Grip => currentState.IsGrasped ? 1.0f : 0.0f;

        public override bool IndexPointing => false;

        public override bool IndexResting => true;

        public override bool ThumbUp => !currentState.ControllerProperties.IsTouchpadTouched;

        public override bool ThumbResting => currentState.ControllerProperties.IsTouchpadTouched;

        public override Vector2 ThumbAxis => new Vector2((float)currentState.ControllerProperties.TouchpadX, (float)currentState.ControllerProperties.TouchpadY);

        public override Vector2 ThumbstickAxis => new Vector2((float)currentState.ControllerProperties.ThumbstickX, (float)currentState.ControllerProperties.ThumbstickY);

        public override bool IsPressed(TouchControllerButton button) => IsButtonPressed(button, currentState);

        public override bool IsPressedDown(TouchControllerButton button) => !IsButtonPressed(button, previousState) ? IsButtonPressed(button, currentState) : false;

        public override bool IsPressReleased(TouchControllerButton button) => IsButtonPressed(button, previousState) ? !IsButtonPressed(button, currentState) : false;

        public override bool IsTouched(TouchControllerButton button) => !IsButtonTouched(button, currentState);

        public override bool IsTouchedDown(TouchControllerButton button) => !IsButtonTouched(button, previousState) ? IsButtonTouched(button, currentState) : false;

        public override bool IsTouchReleased(TouchControllerButton button) => IsButtonTouched(button, previousState) ? !IsButtonTouched(button, currentState) : false;

        public void Update(PerceptionTimestamp timeStamp, SpatialCoordinateSystem coordinateSystem)
        {
            var states = interactionManager.GetDetectedSourcesAtTimestamp(timeStamp);

            foreach (SpatialInteractionSourceState state in states)
            {
                if (state.Source.Handedness == hand)
                {
                    SpatialInteractionSourceLocation location = state.Properties.TryGetLocation(coordinateSystem);

                    if (location != null)
                    {
                        SetSpatialInteractionSourceLocation(location);
                    }

                    previousState = currentState;
                    currentState = state;

                    internalState = previousState != null ? DeviceState.Valid : DeviceState.Invalid;
                }
            }
        }

        private bool IsButtonPressed(TouchControllerButton button, SpatialInteractionSourceState state)
        {
            switch (button)
            {
                case TouchControllerButton.Thumbstick:
                    return state.ControllerProperties.IsThumbstickPressed;
                case TouchControllerButton.Touchpad:
                    return state.ControllerProperties.IsTouchpadPressed;
                case TouchControllerButton.A when state.ControllerProperties.IsTouchpadPressed && hand == SpatialInteractionSourceHandedness.Right:
                    return ThumbAxis.X >= 0.0f;
                case TouchControllerButton.B when state.ControllerProperties.IsTouchpadPressed && hand == SpatialInteractionSourceHandedness.Right:
                    return ThumbAxis.X < 0.0f;
                case TouchControllerButton.X when state.ControllerProperties.IsTouchpadPressed && hand == SpatialInteractionSourceHandedness.Left:
                    return ThumbAxis.X < 0.0f;
                case TouchControllerButton.Y when state.ControllerProperties.IsTouchpadPressed && hand == SpatialInteractionSourceHandedness.Left:
                    return ThumbAxis.X >= 0.0f;
                case TouchControllerButton.Trigger:
                    return state.IsSelectPressed;
                case TouchControllerButton.Grip:
                    return state.IsGrasped;
                case TouchControllerButton.Menu:
                    return state.IsMenuPressed;
                default:
                    return false;
            }
        }

        private bool IsButtonTouched(TouchControllerButton button, SpatialInteractionSourceState state)
        {
            switch (button)
            {
                case TouchControllerButton.Touchpad:
                    return state.ControllerProperties.IsTouchpadTouched;
                case TouchControllerButton.A when state.ControllerProperties.IsTouchpadPressed && hand == SpatialInteractionSourceHandedness.Right:
                    return ThumbAxis.X >= 0.0f;
                case TouchControllerButton.B when state.ControllerProperties.IsTouchpadPressed && hand == SpatialInteractionSourceHandedness.Right:
                    return ThumbAxis.X < 0.0f;
                case TouchControllerButton.X when state.ControllerProperties.IsTouchpadPressed && hand == SpatialInteractionSourceHandedness.Left:
                    return ThumbAxis.X < 0.0f;
                case TouchControllerButton.Y when state.ControllerProperties.IsTouchpadPressed && hand == SpatialInteractionSourceHandedness.Left:
                    return ThumbAxis.X >= 0.0f;
                default:
                    return false;
            }
        }

        private void InteractionManager_SourceLost(SpatialInteractionManager sender, SpatialInteractionSourceEventArgs args)
        {
            if (args.State.Source.Handedness == hand)
            {
                internalState = DeviceState.Invalid;

                previousState = null;
                currentState = null;
            }
        }

        private void SetSpatialInteractionSourceLocation(SpatialInteractionSourceLocation location)
        {
            currentPosition = location.Position?.ToVector3() ?? currentPosition;
            currentRotation = location.Orientation?.ToQuaternion() ?? currentRotation;
            currentLinearVelocity = location.Velocity?.ToVector3() ?? currentLinearVelocity;
            currentAngularVelocity = location.AngularVelocity?.ToVector3() ?? currentAngularVelocity;
        }
    }
}

#endif
