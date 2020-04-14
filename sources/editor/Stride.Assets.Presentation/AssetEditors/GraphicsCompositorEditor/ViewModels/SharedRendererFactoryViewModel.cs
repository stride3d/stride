// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows.Input;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Core.Quantum;
using Xenko.Rendering.Compositing;

namespace Xenko.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels
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
