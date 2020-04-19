// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModel;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    public class EntityGizmosViewModel : DispatcherViewModel
    {
        private readonly IEditorGameController controller;
        private readonly List<GizmoViewModel> activeGizmos = new List<GizmoViewModel>();

        public EntityGizmosViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] IEditorGameController controller)
            : base(serviceProvider)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            this.controller = controller;
            activeGizmos.AddRange(typeof(EntityComponent).GetInheritedTypes().Where(x => StrideDefaultAssetsPlugin.GizmoTypeDictionary.ContainsKey(x)).OrderBy(DisplayAttribute.GetDisplayName).Select(x => new GizmoViewModel(ServiceProvider, x, controller)));
            FallbackGizmo = new GizmoViewModel(ServiceProvider, typeof(TransformComponent), controller);
            ToggleAllGizmosCommand = new AnonymousCommand<bool>(ServiceProvider, value => ActiveGizmos.ForEach(x => x.IsActive = value));
        }

        public IReadOnlyCollection<GizmoViewModel> ActiveGizmos => activeGizmos;

        public GizmoViewModel FallbackGizmo { get; }

        public double GizmoSize { get { return Service.GizmoSize; } set { SetValue(Math.Abs(Service.GizmoSize - (float)value) > MathUtil.ZeroTolerance, () => Service.GizmoSize = (float)value); } }

        public bool FixedSize { get { return Service.FixedSize; } set { SetValue(Service.FixedSize != value, () => Service.FixedSize = value); } }

        public ICommandBase ToggleAllGizmosCommand { get; }

        private IEditorGameComponentGizmoViewModelService Service => controller.GetService<IEditorGameComponentGizmoViewModelService>();

        public void LoadSettings([NotNull] SceneSettingsData sceneSettings)
        {
            GizmoSize = sceneSettings.ComponentGizmoSize;
            FixedSize = sceneSettings.FixedSizeGizmos;
            var hiddenGizmos = sceneSettings.HiddenGizmos;
            hiddenGizmos.Select(x => ActiveGizmos.FirstOrDefault(y => y.ComponentName == x)).NotNull().ForEach(x => x.IsActive = false);
            FallbackGizmo.IsActive = hiddenGizmos.All(x => x != FallbackGizmo.ComponentName);
        }

        public void SaveSettings([NotNull] SceneSettingsData sceneSettings)
        {
            sceneSettings.ComponentGizmoSize = GizmoSize;
            sceneSettings.FixedSizeGizmos = FixedSize;
            var hiddenComponents = sceneSettings.HiddenGizmos;
            hiddenComponents.Clear();
            ActiveGizmos.Where(x => !x.IsActive).Select(x => x.ComponentName).ForEach(hiddenComponents.Add);
            if (!FallbackGizmo.IsActive)
                hiddenComponents.Add(FallbackGizmo.ComponentName);
        }
    }
}
