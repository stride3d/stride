// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Input;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Game
{
    public abstract class EditorGameCameraService : EditorGameMouseServiceBase, IEditorGameCameraService, IEditorGameCameraViewModelService
    {
        public static readonly Vector3 DefaultPosition = new Vector3(4, 2, 4);
        public static readonly float DefaultPitch = -MathUtil.Pi / 12.0f;
        public static readonly float DefaultYaw = MathUtil.Pi / 4.0f;
        public static readonly float DefaultMoveSpeed = 3.0f;
        protected static readonly Vector3 UpVector = new Vector3(0, 1, 0);
        protected static readonly Vector3 ForwardVector = new Vector3(0, 0, -1);

        private Entity cameraEditorEntity;

        protected EditorGameCameraService(IEditorGameController controller)
        {
            Controller = controller;
        }

        /// <summary>
        /// Gets the camera component.
        /// </summary>
        public CameraComponent Component { get; private set; }

        /// <summary>
        /// Gets the current view matrix of the camera.
        /// </summary>
        public Matrix ViewMatrix => Component.ViewMatrix;

        /// <summary>
        /// Gets the current projection matrix of the camera.
        /// </summary>
        public Matrix ProjectionMatrix
        {
            get
            {
                Component.Update();
                return Component.ProjectionMatrix;
            }
        }

        /// <summary>
        /// Gets whether the camera is currently moving.
        /// </summary>
        // TODO: Verify this is correct. Preferably compute a value in the UpdateCamera method.
        public bool IsMoving => Game.Input.IsMouseButtonDown(MouseButton.Left) || Game.Input.IsMousePositionLocked;

        public float AspectRatio => Component.AspectRatio;

        public float VerticalFieldOfView => Component.VerticalFieldOfView;

        /// <summary>
        /// Gets the near plane distance.
        /// </summary>
        public float NearPlane => Component.NearClipPlane;

        /// <summary>
        /// Gets the far plane distance.
        /// </summary>
        public float FarPlane => Component.FarClipPlane;

        /// <summary>
        /// The scale used for grid spacing and camera speed.
        /// </summary>
        public float SceneUnit { get; set; }

        /// <summary>
        /// Gets or sets whether the camera uses an orthographic projection.
        /// </summary>
        public bool IsOrthographic
        {
            get { return Component.Projection == CameraProjectionMode.Orthographic; }
            set { Component.Projection = value ? CameraProjectionMode.Orthographic : CameraProjectionMode.Perspective; }
        }

        /// <summary>
        /// Gets or sets the moving speed of the camera (in units/second).
        /// </summary>
        public float MoveSpeed { get; set; }

        /// <summary>
        /// Gets or sets the rotation speed of the camera (in radian/screen units)
        /// </summary>
        public float RotationSpeed { get; set; }

        /// <summary>
        /// Gets or sets the mouse move speed factor.
        /// </summary>
        /// <value>
        /// The mouse move speed factor.
        /// </value>
        public float MouseMoveSpeedFactor { get; set; }

        /// <summary>
        /// Gets or sets the mouse wheel zoom speed factor.
        /// </summary>
        /// <value>
        /// The mouse wheel zoom speed factor.
        /// </value>
        public float MouseWheelZoomSpeedFactor { get; set; }

        /// <summary>
        /// Gets or sets the rate at which orientation is adapted to a target value.
        /// </summary>
        /// <value>
        /// The adaptation rate.
        /// </value>
        public float RotationAdaptationSpeed { get; set; }

        /// <summary>
        /// Gets or sets the Yaw rotation of the camera.
        /// </summary>
        public float Yaw { get; private set; }

        /// <summary>
        /// Gets or sets the Pitch rotation of the camera.
        /// </summary>
        public float Pitch { get; private set; }

        /// <summary>
        /// Gets or sets the position of the camera.
        /// </summary>
        public Vector3 Position { get; private set; }

        /// <inheritdoc/>
        public override bool IsControllingMouse { get; protected set; }

        protected IEditorGameController Controller { get; }

        protected EntityHierarchyEditorGame Game { get; private set; }

        void IEditorGameCameraViewModelService.SetOrthographicProjection(bool value) { Controller.InvokeAsync(() => IsOrthographic = value); }

        void IEditorGameCameraViewModelService.SetOrthographicSize(float value) { Controller.InvokeAsync(() => Component.OrthographicSize = value); }

        void IEditorGameCameraViewModelService.SetNearPlane(float value) { Controller.InvokeAsync(() => Component.NearClipPlane = value); }

        void IEditorGameCameraViewModelService.SetFarPlane(float value) { Controller.InvokeAsync(() => Component.FarClipPlane = value); }

        void IEditorGameCameraViewModelService.SetFieldOfView(float value) { Controller.InvokeAsync(() => Component.VerticalFieldOfView = value); }

        /// <summary>
        /// Resets the camera orientation.
        /// </summary>
        /// <param name="viewDirection">The new direction facing the camera.</param>
        public virtual void ResetCamera(Vector3 viewDirection)
        {
            var isViewVertical = MathUtil.NearEqual(viewDirection.X, 0) && MathUtil.NearEqual(viewDirection.Z, 0);
            Yaw = isViewVertical ? 0 : (float)Math.Atan2(-viewDirection.X, -viewDirection.Z);

            var horizontalViewDirection = new Vector2(viewDirection.X, viewDirection.Z);
            Pitch = (float)Math.Atan2(viewDirection.Y, horizontalViewDirection.Length());
        }

        public void ResetCamera(CameraOrientation orientation)
        {
            Vector3 viewDirection;

            switch (orientation)
            {
                case CameraOrientation.Front:
                    viewDirection = Vector3.UnitZ;
                    break;
                case CameraOrientation.Back:
                    viewDirection = -Vector3.UnitZ;
                    break;
                case CameraOrientation.Top:
                    viewDirection = -Vector3.UnitY;
                    break;
                case CameraOrientation.Bottom:
                    viewDirection = Vector3.UnitY;
                    break;
                case CameraOrientation.Left:
                    viewDirection = Vector3.UnitX;
                    break;
                case CameraOrientation.Right:
                    viewDirection = -Vector3.UnitX;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orientation));
            }

            ResetCamera(viewDirection);
        }

        /// <inheritdoc/>
        public void LoadSettings(SceneSettingsData sceneSettings)
        {
            // TODO: split SceneSettingsData to some kind of dictionary where each service can add whatever it want
            Controller.InvokeAsync(() =>
            {
                SetCurrentPosition(sceneSettings.CamPosition);
                var orientation = sceneSettings.CamPitchYaw;
                SetCurrentPitch(orientation.X);
                SetCurrentYaw(orientation.Y);
                UpdateViewMatrix();
            });
        }

        /// <inheritdoc/>
        public void SaveSettings(SceneSettingsData sceneSettings)
        {
            sceneSettings.CamPosition = Position;
            var orientation = new Vector2(Pitch, Yaw);
            sceneSettings.CamPitchYaw = orientation;
        }

        protected override Task<bool> Initialize(EditorServiceGame editorGame)
        {
            Game = (EntityHierarchyEditorGame)editorGame;
            Component = new CameraComponent();
            cameraEditorEntity = new Entity("Camera Editor Entity") { Component };

            Game.EditorScene.Entities.Add(cameraEditorEntity);

            var editorCameraTopLevel = Game.EditorSceneSystem.GraphicsCompositor.Game as EditorTopLevelCompositor;
            if (editorCameraTopLevel != null)
                editorCameraTopLevel.ExternalCamera = Component;

            // set the camera values
            Component.NearClipPlane = CameraComponent.DefaultNearClipPlane;
            Component.FarClipPlane = CameraComponent.DefaultFarClipPlane;
            Component.UseCustomViewMatrix = true;

            MoveSpeed = DefaultMoveSpeed;
            RotationSpeed = 0.75f * MathUtil.Pi;
            MouseMoveSpeedFactor = 100.0f;
            MouseWheelZoomSpeedFactor = 12.0f;
            RotationAdaptationSpeed = 5.0f;
            Component.UseCustomAspectRatio = true;
            Reset();

            // create the script
            Game.Script.AddTask(Update);

            return Task.FromResult(true);
        }

        public override void UpdateGraphicsCompositor(EditorServiceGame game)
        {
            base.UpdateGraphicsCompositor(game);

            var cameraTopLevel = Game.SceneSystem.GraphicsCompositor.Game as EditorTopLevelCompositor;
            if (cameraTopLevel != null)
                cameraTopLevel.ExternalCamera = Component;
        }

        /// <summary>
        /// Resets <see cref="Pitch"/>, <see cref="Position"/> and <see cref="Yaw"/> to default values.
        /// </summary>
        /// <seealso cref="DefaultPitch"/>
        /// <seealso cref="DefaultPosition"/>
        /// <seealso cref="DefaultYaw"/>
        protected virtual void Reset()
        {
            Yaw = DefaultYaw;
            Pitch = DefaultPitch;
            Position = DefaultPosition;
        }

        protected virtual void SetCurrentPitch(float value)
        {
            Pitch = MathUtil.Clamp(value, -MathUtil.PiOverTwo, MathUtil.PiOverTwo);
        }

        protected virtual void SetCurrentPosition(Vector3 value)
        {
            Position = value;
        }

        protected virtual void SetCurrentYaw(float value)
        {
            Yaw = value;
        }

        /// <summary>
        /// Updates the camera parameters.
        /// </summary>
        protected virtual void UpdateCamera()
        {
            // Update camera ratio
            var backBuffer = Game.GraphicsDevice.Presenter.BackBuffer;
            if (backBuffer != null)
            {
                Component.AspectRatio = backBuffer.Width / (float)backBuffer.Height;
            }
        }

        protected void UpdateViewMatrix()
        {
            var rotation = Quaternion.Invert(Quaternion.RotationYawPitchRoll(Yaw, Pitch, 0));
            var viewMatrix = Matrix.Translation(-Position) * Matrix.RotationQuaternion(rotation);
            Component.ViewMatrix = viewMatrix;
        }

        private async Task Update()
        {
            while (!IsDisposed)
            {
                if (IsActive)
                {
                    UpdateCamera();
                    UpdateViewMatrix();
                }

                await Game.Script.NextFrame();
            }
        }

        void IEditorGameCameraViewModelService.ResetCamera()
        {
            Reset();
        }

        void IEditorGameCameraViewModelService.ResetCameraOrientation(CameraOrientation orientation)
        {
            Controller.InvokeAsync(() => ResetCamera(orientation));
        }
    }
}
