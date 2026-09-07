// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services
{
    /// <summary>
    /// An editor game service that draws optional scene overlays, shown as toggles in the scene editor's overlays popup.
    /// </summary>
    public interface IEditorGameOverlayService : IEditorGameViewModelService
    {
        /// <summary>
        /// The heading of this service's toggles.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// The overlays this service can draw. After <see cref="OverlaysChanged"/>, the editor calls <see cref="SetOverlayVisible"/> for each one.
        /// </summary>
        IReadOnlyList<EditorOverlay> Overlays { get; }

        event EventHandler OverlaysChanged;

        /// <summary>
        /// Shows or hides one overlay. Called from the UI thread; the service marshals to the game thread itself.
        /// </summary>
        void SetOverlayVisible(string key, bool visible);
    }

    /// <summary>
    /// One toggle of an <see cref="IEditorGameOverlayService"/>.
    /// </summary>
    public sealed class EditorOverlay
    {
        public EditorOverlay(string key, string displayName, Color4? color = null, bool visibleByDefault = false)
        {
            Key = key;
            DisplayName = displayName;
            Color = color;
            VisibleByDefault = visibleByDefault;
        }

        /// <summary>
        /// Identifies the overlay across sessions (persisted in the scene settings); unique across services.
        /// </summary>
        public string Key { get; }

        public string DisplayName { get; }

        /// <summary>
        /// The color the overlay is drawn with, shown next to its toggle, or null when it has none.
        /// </summary>
        public Color4? Color { get; }

        /// <summary>
        /// Whether the overlay shows in a scene whose settings never mentioned it.
        /// </summary>
        public bool VisibleByDefault { get; }
    }
}
