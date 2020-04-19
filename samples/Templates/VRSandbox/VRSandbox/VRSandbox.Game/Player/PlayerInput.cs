// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using VRSandbox.Core;

namespace VRSandbox.Player
{
    public enum HandSide
    {
        Left    = 0,
        Right   = 1,
    }

    public class HandsInput
    {
        /// <summary>
        /// Movement of each hand since the last frame, in world space units (meters)
        /// </summary>
        public Vector3[] HandMovement   = new Vector3[System.Enum.GetNames(typeof(HandSide)).Length];

        /// <summary>
        /// Grabbing power of each hand, 0 being none and 1 being maximum power
        /// </summary>
        public float[]   HandGrab       = new float[System.Enum.GetNames(typeof(HandSide)).Length];
    }

    public class PlayerInput : SyncScript
    {
        public static readonly EventKey<HandsInput> HandsControlEventKey = new EventKey<HandsInput>();

        public int ControllerIndex { get; set; }

        public float DeadZone { get; set; } = 0.25f;

        public CameraComponent Camera { get; set; }

        private Vector3 AsWorldVector(Vector2 inputVector)
        {
            var worldSpeed = (Camera != null)
                ? Utils.LogicDirectionToWorldDirection(inputVector, Camera, Vector3.UnitY)
                : new Vector3(inputVector.X, 0, inputVector.Y);

            var moveLength = inputVector.Length();
            var isDeadZoneLeft = moveLength < DeadZone;
            if (isDeadZoneLeft)
                return Vector3.Zero;

            moveLength = (moveLength > 1) ? 1 : (moveLength - DeadZone) / (1f - DeadZone);

            worldSpeed *= moveLength;

            return worldSpeed;
        }

        public override void Update()
        {
            var handsInput = new HandsInput();
            handsInput.HandMovement[(int)HandSide.Left]     = AsWorldVector(Input.GetLeftThumb(ControllerIndex));
            handsInput.HandMovement[(int)HandSide.Right]    = AsWorldVector(Input.GetRightThumb(ControllerIndex));
            handsInput.HandGrab[(int)HandSide.Left]         = Input.GetLeftTrigger(ControllerIndex);
            handsInput.HandGrab[(int)HandSide.Right]        = Input.GetRightTrigger(ControllerIndex);

            HandsControlEventKey.Broadcast(handsInput);
        }
    }
}
