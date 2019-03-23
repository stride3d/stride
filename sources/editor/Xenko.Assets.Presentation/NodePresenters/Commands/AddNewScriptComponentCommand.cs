// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
            if (!(nodePresenter.Root is AssetRootNodePresenter assetPresenter))
                return;

            var undoRedoService = assetPresenter.Asset.UndoRedoService;
            var session = assetPresenter.Asset.Session;
            var serviceProvider = assetPresenter.Asset.ServiceProvider;

            var template = ScriptTemplateGenerator.GetScriptTemplateAssetDescriptions(session.FindTemplates(TemplateScope.Asset)).FirstOrDefault();

            if (template != null)
            {
                var viewModel = new TemplateDescriptionViewModel(serviceProvider, template);

                var customParameters = ScriptTemplateGenerator.GetAssetOverrideParameters(parameter as string, true);

                var script = (await session.ActiveAssetView.RunAssetTemplate(viewModel, null, customParameters)).SingleOrDefault();
                if (script == null)
                    return;
            }

            //TODO: Add script component to entity.
            //using (var transaction = undoRedoService.CreateTransaction())
            //{
            //    undoRedoService.SetName(transaction, "Create Script");
            //}
        }

        
    }
}
