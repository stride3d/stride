// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_DIRECT3D11

using System;
using Xenko.Core.Mathematics;
using Xenko.Games;

namespace Xenko.VirtualReality
{
    internal class OpenVRTouchController : TouchController
    {
        private readonly OpenVR.Controller.Hand hand;
        private int controllerIndex = -1;
        private OpenVR.Controller controller;
        private DeviceState internalState;
        private Vector3 currentPos;
        private Vector3 currentLinearVelocity;
        private Vector3 currentAngularVelocity;
        private Quaternion currentRot;

        internal OpenVRTouchController(TouchControllerHand hand)
        {
            this.hand = (OpenVR.Controller.Hand)hand;
        }

        public override void Update(GameTime gameTime)
        {
            var index = OpenVR.Controller.GetDeviceIndex(hand);

            if (controllerIndex != index)
            {
                if (index != -1)
                {
                    controller = new OpenVR.Controller(index);
                    controllerIndex = index;
                }
                else
                {
                    controller = null;
                }
            }

            if (controller != null)
            {
                controller.Update();

                Matrix mat;
                Vector3 vel, angVel;
                internalState = OpenVR.GetControllerPose(controllerIndex, out mat, out vel, out angVel);
                if (internalState != DeviceState.Invalid)
                {
                    Vector3 scale;
                    mat.Decompose(out scale, out currentRot, out currentPos);
                    currentLinearVelocity = vel;
                    currentAngularVelocity = new Vector3(MathUtil.DegreesToRadians(angVel.X), MathUtil.DegreesToRadians(angVel.Y), MathUtil.DegreesToRadians(angVel.Z));
                }
            }

            base.Update(gameTime);
        }

        public override float Trigger => controller?.GetAxis(OpenVR.Controller.ButtonId.ButtonSteamVrTrigger).X ?? 0.0f;

        public override float Grip => controller?.GetPress(OpenVR.Controller.ButtonId.ButtonGrip) ?? false ? 1.0f : 0.0f;

        public override bool IndexPointing => !controller?.GetTouch(OpenVR.Controller.ButtonId.ButtonSteamVrTrigger) ?? false; //not so accurate

        public override bool IndexResting => controller?.GetTouch(OpenVR.Controller.ButtonId.ButtonSteamVrTrigger) ?? false;

        public override bool ThumbUp => !controller?.GetTouch(OpenVR.Controller.ButtonId.ButtonSteamVrTouchpad) ?? false;

        public override bool ThumbResting => controller?.GetTouch(OpenVR.Controller.ButtonId.ButtonSteamVrTouchpad) ?? false;

        public override Vector2 ThumbAxis => controller?.GetAxis() ?? Vector2.Zero;

        public override Vector2 ThumbstickAxis => controller?.GetAxis() ?? Vector2.Zero;

        private OpenVR.Controller.ButtonId ToOpenVrButton(TouchControllerButton button)
        {
            switch (button)
            {
                case TouchControllerButton.Thumbstick:
                    return OpenVR.Controller.ButtonId.ButtonSteamVrTouchpad;              
                case TouchControllerButton.Trigger:
                    return OpenVR.Controller.ButtonId.ButtonSteamVrTrigger;
                case TouchControllerButton.Grip:
                    return OpenVR.Controller.ButtonId.ButtonGrip;
                case TouchControllerButton.Menu:
                    return OpenVR.Controller.ButtonId.ButtonApplicationMenu;
                default:
                    return OpenVR.Controller.ButtonId.ButtonMax;
            }
        }

        public override bool IsPressedDown(TouchControllerButton button)
        {
            return controller?.GetPressDown(ToOpenVrButton(button)) ?? false;
        }

        public override bool IsTouchedDown(TouchControllerButton button)
        {
            return controller?.GetTouchDown(ToOpenVrButton(button)) ?? false;
        }

        public override bool IsPressed(TouchControllerButton button)
        {
            return controller?.GetPress(ToOpenVrButton(button)) ?? false;
        }

        public override bool IsTouched(TouchControllerButton button)
        {
            return controller?.GetTouch(ToOpenVrButton(button)) ?? false;
        }

        public override bool IsPressReleased(TouchControllerButton button)
        {
            return controller?.GetPressUp(ToOpenVrButton(button)) ?? false;
        }

        public override bool IsTouchReleased(TouchControllerButton button)
        {
            return controller?.GetTouchUp(ToOpenVrButton(button)) ?? false;
        }

        public override Vector3 Position => currentPos;

        public override Quaternion Rotation => currentRot;

        public override Vector3 LinearVelocity => currentLinearVelocity;

        public override Vector3 AngularVelocity => currentAngularVelocity;

        public override DeviceState State => internalState;
    }
}

#endif
