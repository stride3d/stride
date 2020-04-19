// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;
using Stride.Input;

namespace Stride.Assets.Presentation.AssetEditors.UIEditor.Game
{
    internal sealed class UIEditorGameCameraService : EditorGameCameraService
    {
        public new static readonly Vector3 DefaultPosition = new Vector3(0, 0, 500);
        public new static readonly float DefaultPitch = 0.0f;
        public new static readonly float DefaultYaw = 0.0f;
        
        private float desiredYaw;
        private float desiredPitch;
        private bool isUpdating;

        public UIEditorGameCameraService(IEditorGameController controller)
            : base(controller)
        {
        }

        /// <inheritdoc/>
        public override void ResetCamera(Vector3 viewDirection)
        {
            var isViewVertical = MathUtil.NearEqual(viewDirection.X, 0) && MathUtil.NearEqual(viewDirection.Z, 0);
            desiredYaw = isViewVertical ? 0 : (float)Math.Atan2(-viewDirection.X, -viewDirection.Z);

            var horizontalViewDirection = new Vector2(viewDirection.X, viewDirection.Z);
            desiredPitch = (float)Math.Atan2(viewDirection.Y, horizontalViewDirection.Length());
        }

        protected override async Task<bool> Initialize(EditorServiceGame editorGame)
        {
            if (!await base.Initialize(editorGame))
                return false;

            // Make sure the camera is orthographic
            IsOrthographic = true;
            return true;
        }

        /// <inheritdoc/>
        protected override void Reset()
        {
            // Default values are different for the UI Editor
            SetCurrentPosition(DefaultPosition);
            SetCurrentYaw(DefaultYaw);
            SetCurrentPitch(DefaultPitch);

            desiredYaw = DefaultYaw;
            desiredPitch = DefaultPitch;

            Component.OrthographicSize = CameraComponent.DefaultOrthographicSize;
        }

        /// <inheritdoc/>
        protected override void SetCurrentPitch(float value)
        {
            base.SetCurrentPitch(value);
            if (!isUpdating)
                desiredPitch = value;
        }

        /// <inheritdoc/>
        protected override void SetCurrentYaw(float value)
        {
            base.SetCurrentYaw(value);
            if (!isUpdating)
                desiredYaw = value;
        }

        /// <inheritdoc/>
        protected override void UpdateCamera()
        {
            base.UpdateCamera();

            if (IsMouseAvailable && Game.Input.IsMouseButtonPressed(MouseButton.Middle))
            {
                // Capture mouse when a button is pressed and the mouse is available
                Game.Input.LockMousePosition();
                Game.IsMouseVisible = false;
                IsControllingMouse = true;
            }
            else if (Game.Input.IsMouseButtonReleased(MouseButton.Middle))
            {
                Game.Input.UnlockMousePosition();
                Game.IsMouseVisible = true;
            }

            // Compute translation speed according to framerate and modifiers
            var translationSpeed = MoveSpeed * SceneUnit * (float)Game.UpdateTime.Elapsed.TotalSeconds;
            if (Game.Input.IsKeyDown(Keys.LeftShift) || Game.Input.IsKeyDown(Keys.RightShift))
                translationSpeed *= 10;

            var yaw = Yaw;
            var pitch = Pitch;
            var position = Position;

            // Take shortest path
            var deltaPitch = desiredPitch - pitch;
            var deltaYaw = (desiredYaw - yaw) % MathUtil.TwoPi;
            if (deltaYaw < 0)
                deltaYaw += MathUtil.TwoPi;
            if (deltaYaw > MathUtil.Pi)
                deltaYaw -= MathUtil.TwoPi;
            desiredYaw = yaw + deltaYaw;

            // Perform orientation transition
            var rotationAdaptation = (float)Game.UpdateTime.Elapsed.TotalSeconds * RotationAdaptationSpeed;
            yaw = Math.Abs(deltaYaw) < rotationAdaptation ? desiredYaw : yaw + rotationAdaptation * Math.Sign(deltaYaw);
            pitch = Math.Abs(deltaPitch) < rotationAdaptation ? desiredPitch : pitch + rotationAdaptation * Math.Sign(deltaPitch);

            // Compute base vectors for camera movement
            var rotation = Matrix.RotationYawPitchRoll(yaw, pitch, 0);
            var forward = Vector3.TransformNormal(ForwardVector, rotation);
            var up = Vector3.TransformNormal(UpVector, rotation);
            var right = Vector3.Cross(forward, up);

            // Dolly (top, bottom, left and right)
            if (IsMouseAvailable && Game.Input.IsMouseButtonDown(MouseButton.Middle))
            {
                desiredYaw = yaw;
                desiredPitch = pitch;

                position += -right * Game.Input.MouseDelta.X * MouseMoveSpeedFactor * translationSpeed;
                position += up * Game.Input.MouseDelta.Y * MouseMoveSpeedFactor * translationSpeed;
            }
            // Dolly (forward and backward)
            else if (IsMouseAvailable && Math.Abs(Game.Input.MouseWheelDelta) > MathUtil.ZeroTolerance)
            {
                desiredYaw = yaw;
                desiredPitch = pitch;

                if (IsOrthographic)
                {
                    var newOrthographicSize = Component.OrthographicSize - translationSpeed*MouseWheelZoomSpeedFactor*Game.Input.MouseWheelDelta;
                    if (newOrthographicSize > 0)
                        Component.OrthographicSize = newOrthographicSize;
                }
            }

            isUpdating = true;
            SetCurrentPitch(pitch);
            SetCurrentPosition(position);
            SetCurrentYaw(yaw);
            UpdateViewMatrix();
            isUpdating = false;
        }
    }
}
