// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using System;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Reflection;
using Stride.Core.Quantum;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Editor.ViewModels.GraphicsCompositorEditor
{
    public sealed class SharedRendererFactoryViewModel : DispatcherViewModel
    {
        [NotNull] private readonly IObjectNode sharedRenderersNode;

        public SharedRendererFactoryViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] IObjectNode sharedRenderersNode, [NotNull] Type type)
            : base(serviceProvider)
        {
            this.sharedRenderersNode = sharedRenderersNode;
            if (sharedRenderersNode == null) throw new ArgumentNullException(nameof(sharedRenderersNode));
            if (type == null) throw new ArgumentNullException(nameof(type));
            Name = TypeDescriptorFactory.Default.AttributeRegistry.GetDisplayName(type);
            Type = type;
            CreateCommand = new AnonymousCommand(serviceProvider, Create);
        }

        public string Name { get; }

        public Type Type { get; }

        public ICommandBase CreateCommand { get; }

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
