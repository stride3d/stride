// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Extensions;
using Stride.Core.Presentation.ViewModel;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Navigation;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    public class EditorNavigationGroupViewModel : DispatcherViewModel
    {
        private readonly IEditorGameController controller;
        private bool isVisible;
        private Color4 color;
        private NavigationMeshGroup group;
        private int index;

        public EditorNavigationGroupViewModel(NavigationMeshGroup group, bool initialVisiblity, int index, [NotNull] IViewModelServiceProvider serviceProvider, [NotNull] IEditorGameController controller) : base(serviceProvider)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            this.controller = controller;
            this.group = group;
            this.index = index;
            isVisible = initialVisiblity;
            color = new ColorHSV(((float)index * 80.0f + 90.0f) % 360.0f, 0.95f, 0.75f, 1.0f).ToColor();
        }

        public bool IsVisible
        {
            get { return isVisible; }
            set { SetValue(ref isVisible, value); Service.UpdateGroupVisibility(@group.Id, value); }
        }

        public string DisplayName
        {
            get { return group.ToString(); }
        }

        public Color4 Color
        {
            get { return color; }
        }

        public Guid Id
        {
            get { return group.Id; }
        }

        public int Index
        {
            get { return index; }
        }

        private IEditorGameNavigationViewModelService Service => controller.GetService<IEditorGameNavigationViewModelService>();
    }
}
