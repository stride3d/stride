// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Engine;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Commands;

/// <summary>
/// Selects, in its editor, the entity of an entity or component property, and moves the camera onto it.
/// </summary>
public sealed class FetchEntityCommand : SyncNodePresenterCommandBase
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

        // The editor that shows the hierarchy: for a child scene, the editor of its root scene
        if (hierarchy.HierarchyRoot?.Editor is not { } editor)
            return;

        var partId = new AbsoluteId(hierarchy.Id, entity.Id);
        if (editor.FindPartViewModel(partId) is not EntityViewModel viewModel)
            return;

        editor.ClearSelection();
        editor.SelectedContent.Add(viewModel);
        editor.FocusOnEntityAsync(viewModel).Forget();
    }
}
