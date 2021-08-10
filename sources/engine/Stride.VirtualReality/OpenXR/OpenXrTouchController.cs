using System;
using System.Collections.Generic;
using System.Text;
using Silk.NET.OpenXR;
using Stride.Core.Mathematics;
using Stride.Games;

namespace Stride.VirtualReality
{
    class OpenXrTouchController : TouchController
    {
        private string baseHandPath;
        private OpenXRHmd baseHMD;
        private SpaceLocation handLocation;
        private TouchControllerHand myHand;

        public ulong[] hand_paths = new ulong[12];

        public Space myHandSpace;
        public Silk.NET.OpenXR.Action myHandAction;

        public OpenXrTouchController(OpenXRHmd hmd, TouchControllerHand whichHand)
        {
            baseHMD = hmd;
            handLocation.Type = StructureType.TypeSpaceLocation;
            myHand = whichHand;
            myHandAction = OpenXRInput.MappedActions[(int)myHand, (int)OpenXRInput.HAND_PATHS.Position];

            ActionSpaceCreateInfo action_space_info = new ActionSpaceCreateInfo()
            {
                Type = StructureType.TypeActionSpaceCreateInfo,
                Action = myHandAction,
                PoseInActionSpace = new Posef(new Quaternionf(0f, 0f, 0f, 1f), new Vector3f(0f, 0f, 0f)),
            };

            OpenXRHmd.CheckResult(baseHMD.Xr.CreateActionSpace(baseHMD.globalSession, in action_space_info, ref myHandSpace), "CreateActionSpace");
        }

        private Vector3 currentPos;
        public override Vector3 Position => currentPos;

        private Quaternion currentRot;
        public override Quaternion Rotation => currentRot;

        private Vector3 currentVel;
        public override Vector3 LinearVelocity => currentVel;

        private Vector3 currentAngVel;
        public override Vector3 AngularVelocity => currentAngVel;

        public override DeviceState State => (handLocation.LocationFlags & SpaceLocationFlags.SpaceLocationPositionValidBit) != 0 ? DeviceState.Valid : DeviceState.OutOfRange;

        private Quaternion? holdOffset;
        private float _holdoffset;

        public override float Trigger => OpenXRInput.GetActionFloat(myHand, TouchControllerButton.Trigger, out _);

        public override float Grip => OpenXRInput.GetActionFloat(myHand, TouchControllerButton.Grip, out _);

        public override bool IndexPointing => false;

        public override bool IndexResting => false;

        public override bool ThumbUp => false;

        public override bool ThumbResting => false;

        public override Vector2 ThumbstickAxis => GetAxis((int)TouchControllerButton.Thumbstick);

        public override Vector2 ThumbAxis => GetAxis((int)TouchControllerButton.Touchpad);

        public Vector2 GetAxis(int index)
        {
            TouchControllerButton button = index == 0 ? TouchControllerButton.Thumbstick : TouchControllerButton.Touchpad;

            return new Vector2(OpenXRInput.GetActionFloat(myHand, button, out _, false),
                               OpenXRInput.GetActionFloat(myHand, button, out _, true));
        }

        public override bool IsPressed(TouchControllerButton button)
        {
            return OpenXRInput.GetActionBool(myHand, button, out _);
        }

        public override bool IsPressedDown(TouchControllerButton button)
        {
            bool isDownNow = OpenXRInput.GetActionBool(myHand, button, out bool changed);
            return isDownNow && changed;
        }

        public override bool IsPressReleased(TouchControllerButton button)
        {
            bool isDownNow = OpenXRInput.GetActionBool(myHand, button, out bool changed);
            return !isDownNow && changed;
        }

        public override bool IsTouched(TouchControllerButton button)
        {
            // unsupported right now
            return false;
        }

        public override bool IsTouchedDown(TouchControllerButton button)
        {
            // unsupported right now
            return false;
        }

        public override bool IsTouchReleased(TouchControllerButton button)
        {
            // unsupported right now
            return false;
        }

        public override unsafe void Update(GameTime time)
        {
            ActionStatePose hand_pose_state = new ActionStatePose()
            {
                Type = StructureType.TypeActionStatePose,
            };

            ActionStateGetInfo get_info = new ActionStateGetInfo()
            {
                Type = StructureType.TypeActionStateGetInfo,
                Action = myHandAction,
            };

            SpaceVelocity sv = new SpaceVelocity()
            {
                Type = StructureType.TypeSpaceVelocity
            };

            handLocation.Next = &sv;

            baseHMD.Xr.GetActionStatePose(baseHMD.globalSession, in get_info, ref hand_pose_state);

            baseHMD.Xr.LocateSpace(myHandSpace, baseHMD.globalPlaySpace, baseHMD.globalFrameState.PredictedDisplayTime,
                                   ref handLocation);

            currentPos.X = handLocation.Pose.Position.X;
            currentPos.Y = handLocation.Pose.Position.Y;
            currentPos.Z = handLocation.Pose.Position.Z;

            currentVel.X = sv.LinearVelocity.X;
            currentVel.Y = sv.LinearVelocity.Y;
            currentVel.Z = sv.LinearVelocity.Z;

            currentAngVel.X = sv.AngularVelocity.X;
            currentAngVel.Y = sv.AngularVelocity.Y;
            currentAngVel.Z = sv.AngularVelocity.Z;

            if (holdOffset.HasValue)
            {
                Quaternion orig = new Quaternion(handLocation.Pose.Orientation.X, handLocation.Pose.Orientation.Y,
                                                 handLocation.Pose.Orientation.Z, handLocation.Pose.Orientation.W);
                currentRot = holdOffset.Value * orig;
            }
            else
            {
                currentRot.X = handLocation.Pose.Orientation.X;
                currentRot.Y = handLocation.Pose.Orientation.Y;
                currentRot.Z = handLocation.Pose.Orientation.Z;
                currentRot.W = handLocation.Pose.Orientation.W;
            }
        }
    }
}
