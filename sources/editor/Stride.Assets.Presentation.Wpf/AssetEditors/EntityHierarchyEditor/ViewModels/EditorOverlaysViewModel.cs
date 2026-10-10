// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    /// <summary>
    /// The overlays popup of the scene editor: one section per <see cref="IEditorGameOverlayService"/>.
    /// </summary>
    public class EditorOverlaysViewModel : DispatcherViewModel
    {
        private readonly IEditorGameController controller;
        private readonly ObservableList<EditorOverlaySectionViewModel> sections = new ObservableList<EditorOverlaySectionViewModel>();
        private HashSet<string> visibleKeysFromSettings;

        public EditorOverlaysViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] IEditorGameController controller)
            : base(serviceProvider)
        {
            this.controller = controller ?? throw new ArgumentNullException(nameof(controller));
            ToggleAllCommand = new AnonymousCommand<bool>(ServiceProvider, value => sections.SelectMany(x => x.Overlays).ForEach(x => x.IsVisible = value));

            Dispatcher.InvokeTask(async () =>
            {
                await controller.GameContentLoaded;
                foreach (var service in controller.GetServices<IEditorGameOverlayService>())
                {
                    var section = new EditorOverlaySectionViewModel(ServiceProvider, service);
                    sections.Add(section);
                    service.OverlaysChanged += (sender, e) => Dispatcher.InvokeAsync(() => RebuildSection(section));
                    RebuildSection(section);
                }
            });
        }

        public IReadOnlyObservableList<EditorOverlaySectionViewModel> Sections => sections;

        public ICommandBase ToggleAllCommand { get; }

        public void LoadSettings([NotNull] SceneSettingsData settings)
        {
            // Scenes saved before the overlays popup listed visible navigation groups by id; those ids are the navigation overlay keys
            var visibleKeys = settings.VisibleOverlays.Count > 0 || settings.VisibleNavigationGroups.Count == 0
                ? settings.VisibleOverlays
                : settings.VisibleNavigationGroups.Select(x => x.ToString()).ToList();
            visibleKeysFromSettings = new HashSet<string>(visibleKeys);

            // Sections may already exist (game content and settings load together): the settings win
            Dispatcher.Invoke(() => sections.SelectMany(x => x.Overlays).ForEach(x => x.IsVisible = visibleKeysFromSettings.Contains(x.Key)));
        }

        public void SaveSettings([NotNull] SceneSettingsData settings)
        {
            settings.VisibleOverlays = sections.SelectMany(x => x.Overlays).Where(x => x.IsVisible).Select(x => x.Key).ToList();
            settings.VisibleNavigationGroups.Clear();
        }

        /// <summary>
        /// Rebuilds a section's toggles from its service; known overlays keep their visibility, new ones take the scene settings or their default.
        /// </summary>
        private void RebuildSection(EditorOverlaySectionViewModel section)
        {
            var previousVisibility = section.Overlays.ToDictionary(x => x.Key, x => x.IsVisible);
            section.Overlays.Clear();
            foreach (var overlay in section.Service.Overlays)
            {
                var isVisible = previousVisibility.TryGetValue(overlay.Key, out var previous) ? previous
                    : visibleKeysFromSettings?.Contains(overlay.Key) ?? overlay.VisibleByDefault;
                section.Overlays.Add(new EditorOverlayViewModel(ServiceProvider, section.Service, overlay, isVisible));
            }
        }
    }

    /// <summary>
    /// The toggles of one <see cref="IEditorGameOverlayService"/>, under its heading.
    /// </summary>
    public class EditorOverlaySectionViewModel : DispatcherViewModel
    {
        public EditorOverlaySectionViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] IEditorGameOverlayService service)
            : base(serviceProvider)
        {
            Service = service;
        }

        public IEditorGameOverlayService Service { get; }

        public string DisplayName => Service.DisplayName;

        public ObservableList<EditorOverlayViewModel> Overlays { get; } = new ObservableList<EditorOverlayViewModel>();
    }

    /// <summary>
    /// One overlay toggle.
    /// </summary>
    public class EditorOverlayViewModel : DispatcherViewModel
    {
        private readonly IEditorGameOverlayService service;
        private bool isVisible;

        public EditorOverlayViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] IEditorGameOverlayService service, [NotNull] EditorOverlay overlay, bool isVisible)
            : base(serviceProvider)
        {
            this.service = service;
            Key = overlay.Key;
            DisplayName = overlay.DisplayName;
            Color = overlay.Color;
            this.isVisible = isVisible;
            service.SetOverlayVisible(Key, isVisible);
        }

        public string Key { get; }

        public string DisplayName { get; }

        public Color4? Color { get; }

        public bool HasColor => Color.HasValue;

        public bool IsVisible
        {
            get { return isVisible; }
            set { SetValue(ref isVisible, value, () => service.SetOverlayVisible(Key, value)); }
        }
    }
}
