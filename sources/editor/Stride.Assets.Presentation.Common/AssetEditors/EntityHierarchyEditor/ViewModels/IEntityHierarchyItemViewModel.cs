// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    /// <summary>
    /// An item of the entity hierarchy editor (the hierarchy itself, a folder or an entity): what the entity factories
    /// and the add-asset policies read of the item they create entities and components for.
    /// </summary>
    public interface IEntityHierarchyItemViewModel
    {
        /// <summary>
        /// Gets the asset (scene or prefab) the item belongs to.
        /// </summary>
        [NotNull]
        AssetViewModel Asset { get; }
    }
}
