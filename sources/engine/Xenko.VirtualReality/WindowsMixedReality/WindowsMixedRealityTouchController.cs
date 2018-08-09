// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if XENKO_GRAPHICS_API_DIRECT3D11 && XENKO_PLATFORM_UWP

using Windows.Perception.Spatial;
using Windows.UI.Input.Spatial;
using Xenko.Core.Mathematics;

namespace Xenko.VirtualReality
{
    internal class WindowsMixedRealityTouchController : TouchController
    {
        private readonly SpatialInteractionSourceHandedness hand;
        private readonly SpatialInteractionManager interactionManager;

        private SpatialInteractionController controller;
        private Vector3 currentAngularVelocity;
        private Vector3 currentLinearVelocity;
        private Vector3 currentPosition;
        private Quaternion currentRotation;
        private SpatialInteractionSourceState currentState;
        private DeviceState internalState;
        private SpatialInteractionSourceLocation pose;
        private SpatialInteractionSourceState previousState;

        internal WindowsMixedRealityTouchController(TouchControllerHand hand, SpatialInteractionManager interactionManager)
        {
            this.hand = (SpatialInteractionSourceHandedness)(hand + 1);
            this.interactionManager = interactionManager;

            interactionManager.SourceDetected += InteractionManager_SourceDetected;
            interactionManager.SourceLost += InteractionManager_SourceLost;
            interactionManager.SourceUpdated += InteractionManager_SourceUpdated;
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

        public override bool IsPressed(TouchControllerButton button) => !IsButtonPressed(button, previousState) ? IsButtonPressed(button, currentState) : false;

        public override bool IsPressedDown(TouchControllerButton button) => IsButtonPressed(button, currentState);

        public override bool IsPressReleased(TouchControllerButton button) => !IsButtonPressed(button, currentState);

        public override bool IsTouched(TouchControllerButton button) => !IsButtonTouched(button, previousState) ? IsButtonTouched(button, currentState) : false;

        public override bool IsTouchedDown(TouchControllerButton button) => IsButtonTouched(button, currentState);

        public override bool IsTouchReleased(TouchControllerButton button) => !IsButtonTouched(button, currentState);

        internal SpatialCoordinateSystem CoordinateSystem { get; set; }

        private bool IsButtonPressed(TouchControllerButton button, SpatialInteractionSourceState state)
        {
            switch (button)
            {
                case TouchControllerButton.Thumbstick:
                    return currentState.ControllerProperties.IsThumbstickPressed;
                case TouchControllerButton.Touchpad:
                    return currentState.ControllerProperties.IsTouchpadPressed;
                case TouchControllerButton.A when currentState.ControllerProperties.IsTouchpadPressed && hand == SpatialInteractionSourceHandedness.Right:
                    return ThumbAxis.X >= 0.0f;
                case TouchControllerButton.B when currentState.ControllerProperties.IsTouchpadPressed && hand == SpatialInteractionSourceHandedness.Right:
                    return ThumbAxis.X < 0.0f;
                case TouchControllerButton.X when currentState.ControllerProperties.IsTouchpadPressed && hand == SpatialInteractionSourceHandedness.Left:
                    return ThumbAxis.X < 0.0f;
                case TouchControllerButton.Y when currentState.ControllerProperties.IsTouchpadPressed && hand == SpatialInteractionSourceHandedness.Left:
                    return ThumbAxis.X >= 0.0f;
                case TouchControllerButton.Trigger:
                    return currentState.SelectPressedValue == 1.0 ? true : false;
                case TouchControllerButton.Grip:
                    return currentState.IsGrasped;
                case TouchControllerButton.Menu:
                    return currentState.IsMenuPressed;
                default:
                    return false;
            }
        }

        private bool IsButtonTouched(TouchControllerButton button, SpatialInteractionSourceState state)
        {
            switch (button)
            {
                case TouchControllerButton.Touchpad:
                    return currentState.ControllerProperties.IsTouchpadTouched;
                case TouchControllerButton.A when currentState.ControllerProperties.IsTouchpadPressed && hand == SpatialInteractionSourceHandedness.Right:
                    return ThumbAxis.X >= 0.0f;
                case TouchControllerButton.B when currentState.ControllerProperties.IsTouchpadPressed && hand == SpatialInteractionSourceHandedness.Right:
                    return ThumbAxis.X < 0.0f;
                case TouchControllerButton.X when currentState.ControllerProperties.IsTouchpadPressed && hand == SpatialInteractionSourceHandedness.Left:
                    return ThumbAxis.X < 0.0f;
                case TouchControllerButton.Y when currentState.ControllerProperties.IsTouchpadPressed && hand == SpatialInteractionSourceHandedness.Left:
                    return ThumbAxis.X >= 0.0f;
                default:
                    return false;
            }
        }

        private void InteractionManager_SourceDetected(SpatialInteractionManager sender, SpatialInteractionSourceEventArgs args)
        {
            if (args.State.Source.Handedness == hand)
            {
                controller = args.State.Source.Controller;
                internalState = DeviceState.Valid;

                pose = args.State.Properties.TryGetLocation(CoordinateSystem);
                SetSpatialInteractionSourceLocation(pose);

                previousState = currentState;
                currentState = args.State;
            }
        }

        private void InteractionManager_SourceLost(SpatialInteractionManager sender, SpatialInteractionSourceEventArgs args)
        {
            if (args.State.Source.Handedness == hand)
            {
                controller = null;
                internalState = DeviceState.Invalid;

                previousState = currentState;
                currentState = args.State;
            }
        }

        private void InteractionManager_SourceUpdated(SpatialInteractionManager sender, SpatialInteractionSourceEventArgs args)
        {
            if (args.State.Source.Handedness == hand)
            {
                pose = args.State.Properties.TryGetLocation(CoordinateSystem);
                SetSpatialInteractionSourceLocation(pose);

                previousState = currentState;
                currentState = args.State;
            }
        }

        private void SetSpatialInteractionSourceLocation(SpatialInteractionSourceLocation pose)
        {
            currentPosition = pose.Position ?? currentPosition;
            currentRotation = pose.Orientation ?? currentRotation;
            currentLinearVelocity = pose.Velocity ?? currentLinearVelocity;
            currentAngularVelocity = pose.AngularVelocity ?? currentAngularVelocity;
        }
    }
}

#endif
