// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModel;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Navigation;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    public class EditorNavigationViewModel : DispatcherViewModel
    {
        private readonly IEditorGameController controller;
        private IEditorGameNavigationViewModelService service;
        private ObservableList<EditorNavigationGroupViewModel> visuals = new ObservableList<EditorNavigationGroupViewModel>();
        private EntityHierarchyEditorViewModel editor;
        private AssetViewModel gameSettingsAsset;
        private List<Guid> initiallyVisibleGroups = new List<Guid>();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="EditorNavigationViewModel"/> class.
        /// </summary>
        /// <param name="editor"></param>
        /// <param name="serviceProvider">The service provider for this view model.</param>
        /// <param name="controller">The controller object for the related editor game.</param>
        public EditorNavigationViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] IEditorGameController controller, [NotNull] EntityHierarchyEditorViewModel editor)
            : base(serviceProvider)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));
            this.editor = editor;
            this.controller = controller;

            ToggleAllGroupsCommand = new AnonymousCommand<bool>(ServiceProvider, value => Visuals.ForEach(x => x.IsVisible = value));

            gameSettingsAsset = editor.Session.AllAssets.FirstOrDefault(x => x.AssetType == typeof(GameSettingsAsset));
            if (gameSettingsAsset != null)
            {
                gameSettingsAsset.PropertyGraph.ItemChanged += GameSettingsPropertyGraphOnItemChanged;
                gameSettingsAsset.PropertyGraph.Changed += GameSettingsPropertyGraphOnChanged;
                UpdateNavigationMeshLayers();
            }

            editor.Dispatcher.InvokeTask(async () =>
            {
                await controller.GameContentLoaded;
                service = controller.GetService<EditorGameNavigationMeshService>();
                UpdateNavigationMeshLayers();
            });

        }

        public override void Destroy()
        {
            if (gameSettingsAsset != null)
            {
                gameSettingsAsset.PropertyGraph.Changed -= GameSettingsPropertyGraphOnChanged;
                gameSettingsAsset.PropertyGraph.ItemChanged -= GameSettingsPropertyGraphOnItemChanged;
            }
            base.Destroy();
        }

        public ICommandBase ToggleAllGroupsCommand { get; }

        /// <summary>
        /// The active debug visuals
        /// </summary>
        public IReadOnlyObservableList<EditorNavigationGroupViewModel> Visuals => visuals;

        public void SaveSettings(SceneSettingsData settings)
        {
            settings.VisibleNavigationGroups = new List<Guid>(Visuals.Where(x=>x.IsVisible).Select(x=>x.Id));
        }

        public void LoadSettings(SceneSettingsData settings)
        {
            initiallyVisibleGroups = settings.VisibleNavigationGroups;
            editor.Dispatcher.Invoke(UpdateNavigationMeshLayers);
        }

        private void GameSettingsPropertyGraphOnChanged(object sender, AssetMemberNodeChangeEventArgs assetMemberNodeChangeEventArgs)
        {
            editor.Dispatcher.Invoke(UpdateNavigationMeshLayers);
        }

        private void GameSettingsPropertyGraphOnItemChanged(object sender, AssetItemNodeChangeEventArgs assetItemNodeChangeEventArgs)
        {
            editor.Dispatcher.Invoke(UpdateNavigationMeshLayers);
        }

        private void UpdateNavigationMeshLayers()
        {
            if (service == null || gameSettingsAsset == null)
                return;

            var asset = gameSettingsAsset.Asset as GameSettingsAsset;
            var navigationSettings = asset.GetOrDefault<NavigationSettings>();

            // Either use the initial visiblity state from the settings or from the previous visuals collection
            ILookup<Guid, bool> previousGroupActiveStates;
            if (initiallyVisibleGroups != null)
            {
                previousGroupActiveStates = initiallyVisibleGroups.ToLookup(x=>x, x=>true);
                initiallyVisibleGroups = null;
            }
            else
                previousGroupActiveStates = Visuals.ToLookup(x => x.Id, x => x.IsVisible);
            
            int layerIndex = 0;
            visuals.Clear();
            foreach (var group in navigationSettings.Groups)
            {
                if (group != null)
                {
                    bool initialVisiblity = previousGroupActiveStates[group.Id].FirstOrDefault();
                    var newGroup = new EditorNavigationGroupViewModel(group, initialVisiblity, layerIndex, ServiceProvider, controller);
                    visuals.Add(newGroup);
                }
                ++layerIndex;
            }
            
            service?.UpdateGroups(visuals);
        }
    }
}
