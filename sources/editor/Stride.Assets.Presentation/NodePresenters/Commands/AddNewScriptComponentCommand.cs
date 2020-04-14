// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Xenko.Core.Annotations;
using Xenko.Core.Reflection;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Core.Quantum;
using System.Collections.Generic;
using Xenko.Core.Assets.Templates;
using Xenko.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Assets.Editor.Quantum.NodePresenters;
using Xenko.Core.Assets;
using Xenko.Assets.Presentation.Templates;
using Xenko.Core;
using Xenko.Engine;
using Xenko.Assets.Presentation.ViewModel;
using Xenko.Core.Assets.Editor.Components.Properties;
using Xenko.Assets.Presentation.AssetEditors;
using Xenko.Core.Presentation.Dirtiables;

namespace Xenko.Assets.Presentation.NodePresenters.Commands
{
    public class AddNewScriptComponentCommand : SyncNodePresenterCommandBase
    {
        /// <summary>
        /// The name of this command.
        /// </summary>
        public const string CommandName = "AddNewScriptComponent";

        /// <inheritdoc/>
        public override string Name => CommandName;

        /// <inheritdoc/>
        public override CombineMode CombineMode => CombineMode.CombineOnlyForAll;

        /// <inheritdoc/>
        public override bool CanAttach(INodePresenter nodePresenter)
        {
            return typeof(EntityComponentCollection).IsAssignableFrom(nodePresenter.Descriptor.Type);
        }

        /// <inheritdoc/>
        protected override async void ExecuteSync(INodePresenter nodePresenter, object parameter, object preExecuteResult)
        {
            if (!(nodePresenter is AssetMemberNodePresenter assetPresenter))
                return;

            var undoRedoService = assetPresenter.Asset.UndoRedoService;
            var session = assetPresenter.Asset.Session;
            var serviceProvider = assetPresenter.Asset.ServiceProvider;

            var scriptSourceCodeProvider = serviceProvider.TryGet<IScriptSourceCodeResolver>();

            if (scriptSourceCodeProvider == null)
                return;

            var template = ScriptTemplateGenerator.GetScriptTemplateAssetDescriptions(session.FindTemplates(TemplateScope.Asset)).FirstOrDefault();

            if (template == null)
                return;

            var viewModel = new TemplateDescriptionViewModel(serviceProvider, template);
            var customParameters = ScriptTemplateGenerator.GetAssetOverrideParameters(parameter as string, true);
            var assetViewModel = (await session.ActiveAssetView.RunAssetTemplate(viewModel, null, customParameters)).FirstOrDefault();

            if (assetViewModel == null)
                return;

            //TODO: Maybe situations where this asset/node are no longer valid.
            if (assetViewModel.IsDeleted)
            {
                return;
            }

            IEnumerable<Type> componentTypes = scriptSourceCodeProvider.GetTypesFromSourceFile(assetViewModel.AssetItem.FullPath);
            var componentType = componentTypes.FirstOrDefault();
            if (componentType != null)
            {
                using (var transaction = session.UndoRedoService.CreateTransaction())
                {
                    object component = Activator.CreateInstance(componentType);
                    var index = new NodeIndex(nodePresenter.Children.Count);
                    nodePresenter.AddItem(component);
                    session.UndoRedoService.PushOperation(
                        new AnonymousDirtyingOperation(
                            assetPresenter.Asset.Dirtiables,
                            () => nodePresenter.RemoveItem(component, index),
                            () => nodePresenter.AddItem(component)));

                    session.UndoRedoService.SetName(transaction, "Add new script component.");
                }
            }
        }
    }
}
