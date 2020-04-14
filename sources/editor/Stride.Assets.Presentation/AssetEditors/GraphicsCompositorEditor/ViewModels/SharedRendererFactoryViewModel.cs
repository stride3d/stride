// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows.Input;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Quantum;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels
{
    public class SharedRendererFactoryViewModel : DispatcherViewModel
    {
        [NotNull] private readonly IObjectNode sharedRenderersNode;

        public SharedRendererFactoryViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] IObjectNode sharedRenderersNode, [NotNull] Type type)
            : base(serviceProvider)
        {
            this.sharedRenderersNode = sharedRenderersNode;
            if (sharedRenderersNode == null) throw new ArgumentNullException(nameof(sharedRenderersNode));
            if (type == null) throw new ArgumentNullException(nameof(type));
            Name = DisplayAttribute.GetDisplayName(type);
            Type = type;
            CreateCommand = new AnonymousCommand(serviceProvider, Create);
        }

        public string Name { get; }

        public Type Type { get; }

        public ICommand CreateCommand { get; }

        private void Create()
        {
            // Create renderer
            var renderer = (ISharedRenderer)Activator.CreateInstance(Type);

            // Add renderer
            using (var transaction = ServiceProvider.Get<IUndoRedoService>().CreateTransaction())
            {
                sharedRenderersNode.Add(renderer);
                ServiceProvider.Get<IUndoRedoService>().SetName(transaction, "Create renderer");
            }
        }
    }
}
