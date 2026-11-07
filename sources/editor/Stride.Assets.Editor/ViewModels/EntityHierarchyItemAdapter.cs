// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// An item of the hierarchy, as the entity factories and the add-asset policies of the shared assembly read it: the
/// cross-platform item view models do not reference that assembly.
/// </summary>
/// <param name="item">The item the entities are created for.</param>
internal sealed class EntityHierarchyItemAdapter(EntityHierarchyItemViewModel item) : IEntityHierarchyItemViewModel
{
    /// <inheritdoc/>
    public AssetViewModel Asset => item.Asset;
}
