// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.ViewModel;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Engine;
using Stride.Engine.Processors;

namespace Stride.Assets.Presentation.ViewModel
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
