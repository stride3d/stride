// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Assets.Editor.Quantum.NodePresenters;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Xenko.Engine;
using Xenko.Engine.Processors;

namespace Xenko.Assets.Presentation.ViewModel
{
    public class CameraComponentViewModel : DispatcherViewModel
    {
        private readonly EntityViewModel entity;

        public CameraComponentViewModel(IViewModelServiceProvider serviceProvider, EntityViewModel entity) : base(serviceProvider)
        {
            this.entity = entity;
        }

        public void FinalizeNodePresenterTree(IAssetNodePresenter root)
        {
            foreach (var node in root.Children.BreadthFirst(x => x.Children))
            {
                if (node.Parent?.Value is CameraComponent)
                {
                    if (node.Name == nameof(CameraComponent.VerticalFieldOfView))
                    {
                        node.AddDependency(node.Parent[nameof(CameraComponent.Projection)], false); // Set to true if it has children properties
                        node.IsVisible = (CameraProjectionMode)node.Parent[nameof(CameraComponent.Projection)].Value == CameraProjectionMode.Perspective;
                    }

                    if (node.Name == nameof(CameraComponent.OrthographicSize))
                    {
                        node.AddDependency(node.Parent[nameof(CameraComponent.Projection)], false); // Set to true if it has children properties
                        node.IsVisible = (CameraProjectionMode)node.Parent[nameof(CameraComponent.Projection)].Value == CameraProjectionMode.Orthographic;
                    }

                    if (node.Name == nameof(CameraComponent.AspectRatio))
                    {
                        node.AddDependency(node.Parent[nameof(CameraComponent.UseCustomAspectRatio)], false); // Set to true if it has children properties
                        node.IsVisible = (bool)node.Parent[nameof(CameraComponent.UseCustomAspectRatio)].Value;
                    }
                }
            }
        }
    }
}
