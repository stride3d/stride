// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.Game;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.Services;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Xenko.Assets.Presentation.SceneEditor;
using Xenko.Editor.Engine;
using Xenko.Engine;
using Xenko.Engine.Processors;
using Xenko.Input;
using static Xenko.Assets.Presentation.SceneEditor.SceneEditorSettings;

namespace Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game
{
    public class EditorGameEntityCameraService : EditorGameCameraService, IEditorGameEntityCameraViewModelService
    {
        protected struct Input
        {
            public bool isPanning;
            public bool isRotating;
            public bool isMoving;
            public bool isZooming;
            public bool isOrbiting;
            public bool isShiftDown;
        };

        private readonly EntityHierarchyEditorViewModel editor;
        private float revolutionRadius;
        private Vector3 targetPos;

        public EditorGameEntityCameraService([NotNull] EntityHierarchyEditorViewModel editor, IEditorGameController controller)
            : base(controller)
        {
            this.editor = editor;
        }

        /// <inheritdoc/>
        public EditorCameraViewModel Camera => editor.Camera;

        /// <inheritdoc/>
        void IEditorGameEntityCameraViewModelService.CenterOnEntity(EntityViewModel entity, int meshIndex)
        {
            entity.Dispatcher.EnsureAccess();
            var id = entity.Id;
            Controller.InvokeAsync(() => SetTarget((Entity)Controller.FindGameSidePart(id), false, meshIndex));
        }

        /// <inheritdoc/>
        public override void ResetCamera(Vector3 viewDirection)
        {
            var isViewVertical = MathUtil.NearEqual(viewDirection.X, 0) && MathUtil.NearEqual(viewDirection.Z, 0);
            SetCurrentYaw(isViewVertical ? 0 : (float)Math.Atan2(-viewDirection.X, -viewDirection.Z));

            var horizontalViewDirection = new Vector2(viewDirection.X, viewDirection.Z);
            SetCurrentPitch((float)Math.Atan2(viewDirection.Y, horizontalViewDirection.Length()));
        }

        /// <summary>
        /// Sets the target of the camera.
        /// </summary>
        public void SetTarget(Entity target, bool keepActualTargetDistance, int meshIndex = -1, float deltaDistance = 0, Vector3? targetOffset = null)
        {
            var ypr = Quaternion.RotationYawPitchRoll(Yaw, Pitch, 0);
            var direction = Vector3.TransformNormal(ForwardVector, Matrix.RotationQuaternion(ypr));
            var sphere = meshIndex == -1 ? target.CalculateBoundSphere() : target.CalculateBoundSphere(false, model => new[] { model.Meshes[meshIndex] });
            targetPos = sphere.Center;

            if (targetOffset.HasValue)
            {
                targetPos += targetOffset.Value;
            }
            float distance;
            if (keepActualTargetDistance)
            {
                distance = (targetPos - Position).Length() + deltaDistance;
            }
            else
            {
                distance = 3*(Math.Abs(sphere.Radius) > MathUtil.ZeroTolerance ? sphere.Radius : 0.5f * SceneUnit);
            }

            SetCurrentPosition(targetPos - direction*distance);
            revolutionRadius = (targetPos - Position).Length();
        }

        /// <inheritdoc/>
        protected override void Reset()
        {
            base.Reset();

            SetCurrentPitch(DefaultPitch);
            SetCurrentYaw(DefaultYaw);

            targetPos = Vector3.Zero;
            revolutionRadius = (targetPos - Position).Length();
        }

        /// <inheritdoc/>
        protected override void UpdateCamera()
        {
            base.UpdateCamera();

            // Movement speed control
            if (Game.Input.IsKeyPressed(Keys.OemPlus) || Game.Input.IsKeyPressed(Keys.Add))
                editor.Dispatcher.InvokeAsync(() => Camera.IncreaseMovementSpeed());
            else if (Game.Input.IsKeyPressed(Keys.OemMinus) || Game.Input.IsKeyPressed(Keys.Subtract))
                editor.Dispatcher.InvokeAsync(() => Camera.DecreaseMovementSpeed());

            var duplicating = Game.Input.IsKeyDown(Keys.LeftCtrl) || Game.Input.IsKeyDown(Keys.RightCtrl);
            if (duplicating)
                return;

            var isAnyMouseButtonDown = (Game.Input.IsMouseButtonDown(MouseButton.Left) || Game.Input.IsMouseButtonDown(MouseButton.Middle) || Game.Input.IsMouseButtonDown(MouseButton.Right));
            var shouldControlMouse = IsMouseAvailable && isAnyMouseButtonDown;
            if (shouldControlMouse != IsControllingMouse)
            {
                IsControllingMouse = shouldControlMouse;

                if (IsControllingMouse)
                {
                    Game.Input.LockMousePosition();
                    Game.IsMouseVisible = false;
                }
                else
                {
                    Game.Input.UnlockMousePosition();
                    Game.IsMouseVisible = true;
                }
            }

            if (!IsMouseAvailable)
                return;

            var yaw = Yaw;
            var pitch = Pitch;
            var position = Position;
            Input input = GetInput();

            if (IsOrthographic)
            {
                UpdateCameraAsOrthographic(ref yaw, ref pitch, ref position, input);
            }
            else
            {
                UpdateCameraAsPerspective(ref yaw, ref pitch, ref position, input);
            }

            SetCurrentPitch(pitch);
            SetCurrentPosition(position);
            SetCurrentYaw(yaw);
            UpdateViewMatrix();
        }

        private Input GetInput()
        {
            Input input;

            bool lbDown = Game.Input.IsMouseButtonDown(MouseButton.Left);    // TODO: Combine this with UpdateCameraAsOrthographic!
            bool mbDown = Game.Input.IsMouseButtonDown(MouseButton.Middle);
            bool rbDown = Game.Input.IsMouseButtonDown(MouseButton.Right);
            bool isAltDown = Game.Input.IsKeyDown(Keys.LeftAlt) || Game.Input.IsKeyDown(Keys.RightAlt);
            input.isShiftDown = Game.Input.IsKeyDown(Keys.LeftShift) || Game.Input.IsKeyDown(Keys.RightShift);

            input.isPanning = !isAltDown && mbDown && !rbDown;
            input.isRotating = !isAltDown && !mbDown && rbDown;
            input.isMoving = !isAltDown && mbDown && rbDown;
            input.isZooming = (isAltDown && !lbDown && !mbDown && rbDown) || (Math.Abs(Game.Input.MouseWheelDelta) > MathUtil.ZeroTolerance);
            input.isOrbiting = isAltDown && lbDown && !mbDown && !rbDown;

            return input;
        }

        protected void UpdateCameraBase(ref float yaw, ref float pitch, ref Vector3 position, bool asOrthographic, Input input)
        {
            // Get clamped delta time (more stable during lags)
            float dt = Math.Min((float)Game.UpdateTime.Elapsed.TotalSeconds, 1.0f);

            // Compute translation speed according to framerate and modifiers
            float baseSpeed = MoveSpeed * SceneUnit * (input.isShiftDown ? 10 : 1) * (1f/60f);

            float zoomDelta = 0f;

            // Update yaw and pitch first to keep dependencies on 'rotation' up to date with current frame changes
            if (input.isMoving || input.isRotating || input.isOrbiting)
            {
                var rotationSpeed = RotationSpeed * (input.isOrbiting ? 2 : 1); // we want to rotate faster when rotating around an object.
                yaw -= 1.333f * Game.Input.MouseDelta.X * rotationSpeed; // we want to rotate faster Horizontally and Vertically
                if (input.isRotating || input.isOrbiting)
                    pitch = MathUtil.Clamp(pitch - Game.Input.MouseDelta.Y * rotationSpeed, -MathUtil.PiOverTwo, MathUtil.PiOverTwo);
            }

            var rotation = Quaternion.RotationYawPitchRoll(yaw, pitch, 0);

            // If scene has changed since last time
            if (asOrthographic && Game?.ContentScene?.Entities != null)
            {
                // Position the camera outside the bounding volume
                var sceneBounds = BoundingSphere.Empty;
                foreach (var entity in Game.ContentScene.Entities)
                {
                    var sphere = entity.CalculateBoundSphere();
                    sceneBounds = BoundingSphere.Merge(sceneBounds, sphere);
                }
                revolutionRadius = Math.Max(10f, sceneBounds.Radius * 2f);

                position = targetPos - Vector3.Normalize(Vector3.Transform(ForwardVector, rotation)) * revolutionRadius;
            }


            // Dolly (WASD model/arrow keys)
            if (input.isPanning || input.isMoving || input.isRotating)
            {
                var inputSystem = Game.Input;
                float x, y, z;
                x = y = z = 0f;
                
                if (inputSystem.IsKeyDown(MoveCamLeft.GetValue()) || inputSystem.IsKeyDown(Keys.Left))
                    x -= 1f;
                if (inputSystem.IsKeyDown(MoveCamRight.GetValue()) || inputSystem.IsKeyDown(Keys.Right))
                    x += 1f;
                if (inputSystem.IsKeyDown(MoveCamBackward.GetValue()) || inputSystem.IsKeyDown(Keys.Down))
                    z -= 1f;
                if (inputSystem.IsKeyDown(MoveCamForward.GetValue()) || inputSystem.IsKeyDown(Keys.Up))
                    z += 1f;
                if (inputSystem.IsKeyDown(MoveCamDownward.GetValue()) || inputSystem.IsKeyDown(Keys.PageDown))
                    y -= 1f;
                if (inputSystem.IsKeyDown(MoveCamUpward.GetValue()) || inputSystem.IsKeyDown(Keys.PageUp))
                    y += 1f;

                if (asOrthographic)
                {
                    zoomDelta += y;
                    y = z;
                    z = 0f;
                }

                var localDirection = Vector3.Normalize(new Vector3(x, y, -z));
                position += Vector3.Transform(localDirection, rotation) * baseSpeed * dt * 60f;
            }

            // Pan
            if (input.isPanning)
            {
                float panningSpeed = asOrthographic ? Component.OrthographicSize : revolutionRadius;
                panningSpeed *= MouseMoveSpeedFactor * baseSpeed;
                if (InvertPanningAxis.GetValue())
                    panningSpeed = -panningSpeed;

                var localDirection = new Vector3(Game.Input.MouseDelta.X, -Game.Input.MouseDelta.Y, 0f);
                position += Vector3.Transform(localDirection, rotation) * panningSpeed;
            }

            // Move
            if (input.isMoving)
            {
                if(asOrthographic)
                {
                    zoomDelta -= MouseMoveSpeedFactor * Game.Input.MouseDelta.Y;
                }
                else
                {
                    var forward = Vector3.Transform(ForwardVector, rotation);
                    var projectedForward = Vector3.Normalize(new Vector3(forward.X, 0, forward.Z)); // camera forward vector project on the XZ plane
                    position -= projectedForward * baseSpeed * MouseMoveSpeedFactor * Game.Input.MouseDelta.Y;
                }
            }

            // Forward/backward
            if (input.isZooming)
            {
                if (asOrthographic)
                {
                    zoomDelta += MouseWheelZoomSpeedFactor * Game.Input.MouseWheelDelta;
                    if (Game.Input.HasDownMouseButtons)
                    {
                        zoomDelta += MouseMoveSpeedFactor * (Game.Input.MouseDelta.X + Game.Input.MouseDelta.Y);
                    }
                }
                else
                {
                    // Perspective
                    var forward = Vector3.Transform(ForwardVector, rotation);
                    position += forward * MouseWheelZoomSpeedFactor * Game.Input.MouseWheelDelta * 0.1f;    // Multiply by 0.1f so it matches the zoom "speed" of the orthographic mode.
                    if (Game.Input.HasDownMouseButtons)
                    {
                        position += forward * baseSpeed * MouseMoveSpeedFactor * (Game.Input.MouseDelta.X + Game.Input.MouseDelta.Y);
                    }
                    revolutionRadius = Vector3.Distance(targetPos, position);
                }
            }

            // Apply zoom
            if (asOrthographic)
            {
                var newOrthographicSize = Component.OrthographicSize - baseSpeed * zoomDelta;
                if (newOrthographicSize > 0)
                    Component.OrthographicSize = newOrthographicSize;
            }
            else
            {
                var newFov = Component.VerticalFieldOfView - baseSpeed * zoomDelta;
                newFov = newFov > 120 ? 120 : newFov < 20 ? 20 : newFov;
                Component.VerticalFieldOfView = newFov;
            }

            // Orbit
            // The connection between position and target is pretty straight-forward
            var direction = Vector3.Transform(ForwardVector, rotation);
            if (input.isOrbiting)
            {
                position = targetPos - direction * revolutionRadius;
            }
            else
            {
                targetPos = position + direction * revolutionRadius;
            }
        }

        protected void UpdateCameraAsOrthographic(ref float yaw, ref float pitch, ref Vector3 position, Input input)
        {
            UpdateCameraBase(ref yaw, ref pitch, ref position, true, input);
        }

        protected void UpdateCameraAsPerspective(ref float yaw, ref float pitch, ref Vector3 position, Input input)
        {
            UpdateCameraBase(ref yaw, ref pitch, ref position, false, input);
        }
    }
}
