// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels
{
    /// <summary>
    /// A view model controlling a grid in an editor game.
    /// </summary>
    public class EditorGridViewModel : DispatcherViewModel
    {
        private readonly IEditorGameController controller;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorGridViewModel"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for this view model.</param>
        /// <param name="controller">The controller object for the related editor game.</param>
        public EditorGridViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] IEditorGameController controller)
            : base(serviceProvider)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            this.controller = controller;
            ToggleCommand = new AnonymousCommand(ServiceProvider, () => IsVisible = !IsVisible);
        }

        /// <summary>
        /// Gets or sets whether the grid is currently visible.
        /// </summary>
        public bool IsVisible { get { return Service.IsActive; } set { SetValue(IsVisible != value, () => Service.IsActive = value); } }

        /// <summary>
        /// Gets or sets the color to apply to the grid.
        /// </summary>
        public Color3 Color { get { return Service.Color; } set { SetValue(Color != value, () => Service.Color = value); } }

        /// <summary>
        /// Gets or sets the alpha level of the grid.
        /// </summary>
        public float Alpha { get { return Service.Alpha; } set { SetValue(Math.Abs(Alpha - value) > MathUtil.ZeroTolerance, () => Service.Alpha = value); } }

        /// <summary>
        /// Gets or sets the axis of the grid.
        /// </summary>
        public int AxisIndex { get { return Service.AxisIndex; } set { SetValue(AxisIndex != value, () => Service.AxisIndex = value); } }

        /// <summary>
        /// Gets or sets whether the X axis of the grid is currently visible.
        /// </summary>
        public bool IsXAxisVisible { get => Convert.ToBoolean(AxisSelection & 0b001); set => AxisSelection = Convert.ToInt16(value) ^ (AxisSelection & 0b110); }

        /// <summary>
        /// Gets or sets whether the Y axis of the grid is currently visible.
        /// </summary>
        public bool IsYAxisVisible { get => Convert.ToBoolean(AxisSelection & 0b010); set => AxisSelection = (Convert.ToInt16(value) << 1) ^ (AxisSelection & 0b101); }

        /// <summary>
        /// Gets or sets whether the Z axis of the grid is currently visible.
        /// </summary>
        public bool IsZAxisVisible { get => Convert.ToBoolean(AxisSelection & 0b100); set => AxisSelection = (Convert.ToInt16(value) << 2) ^ (AxisSelection & 0b011); }

        private int AxisSelection { get => ConvertAxisIndexToSelection(AxisIndex); set => AxisIndex = ConvertAxisSelectionToIndex(value); }

        /// <summary>
        /// Gets a command that will toggle the visibility of the grid.
        /// </summary>
        public ICommandBase ToggleCommand { get; }

        private IEditorGameGridViewModelService Service => controller.GetService<IEditorGameGridViewModelService>();

        private int ConvertAxisIndexToSelection(int index)
        {
            switch (index)
            {
                case 0: return 0b001;
                case 1: return 0b010;
                case 2: return 0b100;
                case 3: return 0b011;
                case 4: return 0b101;
                case 5: return 0b110;
                case 6: return 0b111;
            }
            return 0b000;
        }

        private int ConvertAxisSelectionToIndex(int selection)
        {
            switch (selection)
            {
                case 0b001: return 0;
                case 0b010: return 1;
                case 0b100: return 2;
                case 0b011: return 3;
                case 0b101: return 4;
                case 0b110: return 5;
                case 0b111: return 6;
            }
            return 7;
        }

        public void LoadSettings([NotNull] SceneSettingsData sceneSettings)
        {
            IsVisible = sceneSettings.GridVisible;
            Color = sceneSettings.GridColor;
            Alpha = sceneSettings.GridOpacity;
            AxisIndex = sceneSettings.GridAxisIndex;
            AxisSelection = ConvertAxisIndexToSelection(sceneSettings.GridAxisIndex);
        }

        public void SaveSettings([NotNull] SceneSettingsData sceneSettings)
        {
            sceneSettings.GridVisible = IsVisible;
            sceneSettings.GridColor = Color;
            sceneSettings.GridOpacity = Alpha;
            sceneSettings.GridAxisIndex = AxisIndex;
        }
    }
}
