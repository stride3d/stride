// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Services;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Commands;

/// <summary>
/// The base of the Pick buttons of the entity and component properties: they show the entity picker of the editor that shows the
/// asset (<see cref="IEntityPickerService"/>), on the items of the asset.
/// </summary>
/// <remarks>
/// The parameter is the asset that owns the property, alone or as the first item of a tuple whose second item is the type of the
/// property. For an entity of a child scene, the editor is the one of its root scene, as for <see cref="FetchEntityCommand"/>.
/// </remarks>
public abstract class PickupSceneObjectCommandBase : ChangeValueWithPickerCommandBase
{
    /// <inheritdoc/>
    protected override async Task<PickerResult> ShowPicker(IReadOnlyCollection<INodePresenter> nodePresenters, object currentValue, object parameter)
    {
        var (asset, targetType) = parameter switch
        {
            Tuple<object?, object?> tuple => (tuple.Item1 as AssetViewModel, tuple.Item2 as Type),
            _ => (parameter as AssetViewModel, null),
        };
        // The editor that shows the hierarchy: for a child scene, the editor of its root scene
        if (asset is not EntityHierarchyViewModel { HierarchyRoot.Editor: EntityHierarchyEditorViewModel editor }
            || asset.ServiceProvider.TryGet<IEntityPickerService>() is not { } picker)
        {
            return new PickerResult { ProcessChange = false };
        }

        // The items of the asset, and the roots of the scenes whose items they may be in, as in WPF: a reference can't cross assets
        var picked = await picker.PickEntityAsync(editor, GetComponentType(targetType), item => item is EntityHierarchyRootViewModel || item.Asset == asset);
        return new PickerResult { ProcessChange = picked is not null, NewValue = picked! };
    }

    /// <summary>
    /// Gets the type of the component to pick, or <c>null</c> to pick an entity.
    /// </summary>
    /// <param name="targetType">The type of the property, when the parameter gives it.</param>
    protected abstract Type? GetComponentType(Type? targetType);
}
