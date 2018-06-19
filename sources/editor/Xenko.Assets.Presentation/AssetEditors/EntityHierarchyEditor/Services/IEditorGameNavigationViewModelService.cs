// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core.Mathematics;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Xenko.Editor.EditorGame.ViewModels;
using Xenko.Rendering;

namespace Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services
{
    /// <summary>
    /// A service that renders navigation mesh overlays in the scene
    /// </summary>
    public interface IEditorGameNavigationViewModelService : IEditorGameViewModelService
    {
        /// <summary>
        /// Visibility of all the visuals
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// Updates how many groups there are and in what colors to show them
        /// </summary>
        void UpdateGroups(IList<EditorNavigationGroupViewModel> groups);

        /// <summary>
        /// Updates group visibility, based on display settings
        /// </summary>
        void UpdateGroupVisibility(Guid groupId, bool isVisible);
    }
}
