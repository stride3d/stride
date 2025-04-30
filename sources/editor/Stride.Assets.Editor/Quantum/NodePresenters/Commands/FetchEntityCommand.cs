// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Engine;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Commands;

internal sealed class FetchEntityCommand : SyncNodePresenterCommandBase
{
    /// <summary>
    /// The name of this command.
    /// </summary>
    public const string CommandName = "FetchEntity";

    /// <inheritdoc/>
    public override string Name => CommandName;

    public override bool CanAttach(INodePresenter nodePresenter)
    {
        return typeof(Entity).IsAssignableFrom(nodePresenter.Type) || typeof(EntityComponent).IsAssignableFrom(nodePresenter.Type);
    }

    protected override void ExecuteSync(INodePresenter nodePresenter, object? parameter, object? preExecuteResult)
    {
        if (parameter is EntityHierarchyViewModel hierarchy)
        {
            Fetch(hierarchy, nodePresenter.Value);
        }
    }

    /// <summary>
    /// Fetches the entity corresponding to the given content.
    /// </summary>
    /// <param name="hierarchy">The hierarchy owning the entity to fetch.</param>
    /// <param name="content">The entity to fetch, or one of its components.</param>
    public static void Fetch(EntityHierarchyViewModel hierarchy, object content)
    {
        var entity = content is EntityComponent component ? component.Entity : content as Entity;
        if (entity is null)
            return;

        if (!hierarchy.ServiceProvider.Get<IAssetEditorsManager>().TryGetAssetEditor<EntityHierarchyEditorViewModel>(hierarchy, out var editor))
            return;

        var partId = new AbsoluteId(hierarchy.Id, entity.Id);
        if (editor.FindPartViewModel(partId) is not EntityViewModel viewModel)
            return;

        editor.SelectedItems.Clear();
        editor.SelectedItems.Add(viewModel);
        // FIXME xplat-editor
        //editor.Controller.GetService<IEditorGameEntityCameraViewModelService>().CenterOnEntity(viewModel);
    }
}
