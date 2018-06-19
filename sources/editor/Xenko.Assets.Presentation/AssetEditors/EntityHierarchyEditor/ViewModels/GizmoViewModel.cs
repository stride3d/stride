// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.Services;

namespace Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    [DebuggerDisplay("Component: {ComponentName} IsActive: {IsActive}")]
    public class GizmoViewModel : DispatcherViewModel
    {
        private readonly IEditorGameController controller;
        private bool isActive;

        public GizmoViewModel([NotNull] IViewModelServiceProvider serviceProvider, Type componentType, [NotNull] IEditorGameController controller)
            : base(serviceProvider)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            this.controller = controller;
            ComponentType = componentType;
            isActive = true;
        }

        public Type ComponentType { get; }

        public string ComponentName => ComponentType != null ? DisplayAttribute.GetDisplayName(ComponentType) : string.Empty;

        public bool IsActive { get { return isActive; } set { SetValue(ref isActive, value, () => Service.ToggleGizmoVisibility(ComponentType, value)); } }

        private IEditorGameComponentGizmoViewModelService Service => controller.GetService<IEditorGameComponentGizmoViewModelService>();
    }
}
