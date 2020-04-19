// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Presentation.ViewModel;
using Stride.Assets.Materials;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.SceneEditor;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    public class EditorRenderingViewModel : DispatcherViewModel
    {
        private readonly IEditorGameController controller;

        public EditorRenderingViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] IEditorGameController controller)
            : base(serviceProvider)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            this.controller = controller;
        }

        public EditorRenderMode RenderMode { get { return Service.RenderMode; } set { SetValue(Service.RenderMode != value, () => Service.RenderMode = value); } }

        [NotNull]
        public IEnumerable<EditorRenderMode> AvailableRenderModes => GetAvailableRenderModes();

        private IEditorGameRenderModeViewModelService Service => controller.GetService<IEditorGameRenderModeViewModelService>();

        public void LoadSettings([NotNull] SceneSettingsData sceneSettings)
        {
            var activeRenderMode = AvailableRenderModes.First(); // Default to first render mode (Editor)

            // Is there a matching render mode with same active stream?
            if (!string.IsNullOrEmpty(sceneSettings.ActiveStream))
            {
                var streamRenderMode = AvailableRenderModes.FirstOrDefault(x => x.Mode == GameEditor.RenderMode.SingleStream && x.StreamDescriptor.Name == sceneSettings.ActiveStream);
                if (streamRenderMode != null)
                    activeRenderMode = streamRenderMode;
            }

            RenderMode = activeRenderMode;
        }

        public void SaveSettings([NotNull] SceneSettingsData sceneSettings)
        {
            // Note: we don't save PreviewGameGraphicsCompositor on purpose (we want to start in editor mode for better UX and prevent crashes)
            sceneSettings.RenderMode = RenderMode.Mode;
            sceneSettings.ActiveStream = RenderMode.StreamDescriptor?.Name ?? "";
        }

        [NotNull]
        private static List<EditorRenderMode> GetAvailableRenderModes()
        {
            var renderModes = new List<EditorRenderMode>
            {
                EditorRenderMode.DefaultEditor,
                EditorRenderMode.DefaultGamePreview,
                new EditorRenderMode("-")
            };

            // Separator

            foreach (var materialMode in MaterialStreamFactory.GetAvailableStreams())
            {
                renderModes.Add(new EditorRenderMode(materialMode));
            }
            return renderModes;
        }
    }
}
