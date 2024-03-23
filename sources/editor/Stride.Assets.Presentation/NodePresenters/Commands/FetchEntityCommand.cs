// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.ViewModel;
using Stride.Core;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Quantum.Presenters;
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
            Entity entity = content is EntityComponent component ? component.Entity : content as Entity;
            if (entity is null)
                return;

            scene.ServiceProvider.Get<IAssetEditorsManager>().TryGetAssetEditor<EntityHierarchyEditorViewModel>(scene, out var editor);
            var partId = new AbsoluteId(scene.Id, entity.Id);
            if (editor.FindPartViewModel(partId) is not EntityViewModel viewModel)
                return;

            editor.SelectedItems.Clear();
            editor.SelectedItems.Add(viewModel);
            editor.Controller.GetService<IEditorGameEntityCameraViewModelService>().CenterOnEntity(viewModel);
        }
    }
}
