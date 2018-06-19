// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Xenko.Assets.Presentation.ViewModel;
using Xenko.Core.Presentation.Services;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;

namespace Xenko.Assets.Presentation.SceneEditor.Services
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
