// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.ViewModel;
using Stride.Engine;

namespace Stride.Assets.Presentation.NodePresenters.Commands
{
    public class FetchEntityCommand : SyncNodePresenterCommandBase
    {
        /// <summary>
        /// The name of this command.
        /// </summary>
        public const string CommandName = "FetchEntity";

        /// <inheritdoc/>
        public override string Name => CommandName;

        /// <inheritdoc/>
        public override bool CanAttach(INodePresenter nodePresenter)
        {
            return typeof(Entity).IsAssignableFrom(nodePresenter.Type) || typeof(EntityComponent).IsAssignableFrom(nodePresenter.Type);
        }

        /// <inheritdoc/>
        protected override void ExecuteSync(INodePresenter nodePresenter, object parameter, object preExecuteResult)
        {
            var scene = (EntityHierarchyViewModel)parameter;
            Fetch(scene, nodePresenter.Value);
        }

        /// <summary>
        /// Fetches the entity corresponding to the given content.
        /// </summary>
        /// <param name="scene">The scene owning the entity to fetch.</param>
        /// <param name="content">The entity to fetch, or one of its components.</param>
        public static void Fetch(EntityHierarchyViewModel scene, object content)
        {
            Entity entity;
            var component = content as EntityComponent;

            if (component != null)
                entity = component.Entity;
            else
                entity = content as Entity;

            if (entity == null)
                return;

            var editor = scene.Editor;
            var partId = new AbsoluteId(scene.Id, entity.Id);
            var viewModel = editor.FindPartViewModel(partId) as EntityViewModel;
            if (viewModel == null)
                return;

            editor.SelectedItems.Clear();
            editor.SelectedItems.Add(viewModel);
            editor.Controller.GetService<IEditorGameEntityCameraViewModelService>().CenterOnEntity(viewModel);
        }
    }
}
