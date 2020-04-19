// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModel;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Engine.Processors;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels
{
    public class EditorCameraViewModel : DispatcherViewModel
    {
        private readonly IEditorGameController controller;
        
        private bool orthographicProjection;
        private float orthographicSize;
        private float nearPlane;
        private float farPlane;
        private float fieldOfView;

        public static float[] AvailableMovementSpeed =
        {
            0.1f,
            1.0f,
            3.0f,
            10.0f,
            100.0f,
        };

        public int AvailableMovementSpeedCount => AvailableMovementSpeed.Length - 1;

        private static int FindValidMoveSpeedIndex(float value)
        {
            for (int i = 0; i < AvailableMovementSpeed.Length; i++)
            {
                if (MathUtil.NearEqual(value, AvailableMovementSpeed[i]))
                    return i;
            }
            return 2;
        }

        private static float FindValidMoveSpeedValue(int index)
        {
            return AvailableMovementSpeed[MathUtil.Clamp(index, 0, AvailableMovementSpeed.Length - 1)];
        }

        public EditorCameraViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] IEditorGameController controller)
            : base(serviceProvider)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            this.controller = controller;
            ResetCameraCommand = new AnonymousCommand(ServiceProvider, () => Service.ResetCamera());
            ResetCameraOrientationCommand = new AnonymousCommand<CameraOrientation>(ServiceProvider, value => Service.ResetCameraOrientation(value));
        }

        public bool OrthographicProjection { get { return orthographicProjection; } set { SetValue(OrthographicProjection != value, () => Service.SetOrthographicProjection(orthographicProjection = value)); } }

        public float OrthographicSize { get { return orthographicSize; } set { SetValue(Math.Abs(OrthographicSize - value) > MathUtil.ZeroTolerance, () => Service.SetOrthographicSize(orthographicSize = value)); } }

        public float NearPlane { get { return nearPlane; } set { SetValue(Math.Abs(NearPlane - value) > MathUtil.ZeroTolerance, () => Service.SetNearPlane(nearPlane = value)); } }

        public float FarPlane { get { return farPlane; } set { SetValue(Math.Abs(FarPlane - value) > MathUtil.ZeroTolerance, () => Service.SetFarPlane(farPlane = value)); } }

        public float FieldOfView { get { return fieldOfView; } set { SetValue(Math.Abs(FieldOfView - value) > MathUtil.ZeroTolerance, () => Service.SetFieldOfView(fieldOfView = value)); } }

        public float MoveSpeed { get { return Service.MoveSpeed; } set { SetValue(Math.Abs(MoveSpeed - value) > MathUtil.ZeroTolerance, () => Service.MoveSpeed = value); } }

        public int MoveSpeedIndex { get { return FindValidMoveSpeedIndex(MoveSpeed); } set { SetValue(value != MoveSpeedIndex, () => MoveSpeed = FindValidMoveSpeedValue(value)); } }

        public float SceneUnit { get { return Service.SceneUnit; } set { SetValue(Math.Abs(SceneUnit - value) > MathUtil.ZeroTolerance, () => Service.SceneUnit = value); } }

        public ICommandBase ResetCameraCommand { get; }

        public ICommandBase ResetCameraOrientationCommand { get; }

        private IEditorGameCameraViewModelService Service => controller.GetService<IEditorGameCameraViewModelService>();

        public void LoadSettings([NotNull] SceneSettingsData sceneSettings)
        {
            Service.LoadSettings(sceneSettings);
            OrthographicProjection = sceneSettings.CamProjection == CameraProjectionMode.Orthographic;
            OrthographicSize = sceneSettings.CamOrthographicSize;
            NearPlane = sceneSettings.CamNearClipPlane;
            FarPlane = sceneSettings.CamFarClipPlane;
            FieldOfView = sceneSettings.CamVerticalFieldOfView;
            SceneUnit = sceneSettings.SceneUnit <= MathUtil.ZeroTolerance ? 1.0f : sceneSettings.SceneUnit;
            MoveSpeed = sceneSettings.CamMoveSpeed;
        }

        public void SaveSettings([NotNull] SceneSettingsData sceneSettings)
        {
            Service.SaveSettings(sceneSettings);
            sceneSettings.CamProjection = OrthographicProjection ? CameraProjectionMode.Orthographic : CameraProjectionMode.Perspective;
            sceneSettings.CamOrthographicSize = OrthographicSize;
            sceneSettings.CamNearClipPlane = NearPlane;
            sceneSettings.CamFarClipPlane = FarPlane;
            sceneSettings.CamVerticalFieldOfView = FieldOfView;
            sceneSettings.SceneUnit = SceneUnit;
            sceneSettings.CamMoveSpeed = MoveSpeed;
        }

        public void IncreaseMovementSpeed()
        {
            MoveSpeedIndex++;
        }

        public void DecreaseMovementSpeed()
        {
            MoveSpeedIndex--;
        }
    }
}
