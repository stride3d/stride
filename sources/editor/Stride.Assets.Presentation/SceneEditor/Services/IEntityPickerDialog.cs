// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Stride.Assets.Presentation.ViewModel;
using Stride.Core.Presentation.Services;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;

namespace Stride.Assets.Presentation.SceneEditor.Services
{
    /// <summary>
    /// This interface represents a dialog that can pick an entities, or specific parts of an entity (scripts, components) from a scene.
    /// </summary>
    public interface IEntityPickerDialog : IModalDialog
    {
        /// <summary>
        /// Gets or sets the filter to apply to the asset collection displayed in the asset picker.
        /// </summary>
        Func<EntityHierarchyItemViewModel, bool> Filter { get; set; }

        /// <summary>
        /// Gets the assets selected by the user when the asset picking has been validated.
        /// </summary>
        /// <remarks>This property returns an empty enumerable if accessed before the user pressed ok or </remarks>
        IPickedEntity SelectedEntity { get; }
    }
}
