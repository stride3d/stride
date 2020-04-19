// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModel;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Editor.EditorGame.ContentLoader;
using Stride.Editor.EditorGame.Game;

namespace Stride.Assets.Presentation.View
{
    /// <summary>
    /// Interaction logic for DebugEntityHierarchyEditorUserControl.xaml
    /// </summary>
    public partial class DebugEntityHierarchyEditorUserControl : IDebugPage
    {
        public class DebugEntityHierarchyEditorViewModel : DispatcherViewModel
        {
            private readonly EntityHierarchyEditorViewModel editor;
            private readonly Dictionary<AssetId, string> assetLoadingTimeUrls;
            private readonly EditorServiceGame game;

            public DebugEntityHierarchyEditorViewModel([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] EntityHierarchyEditorViewModel editor)
                : base(serviceProvider)
            {
                this.editor = editor;
                var sceneLoader = editor.Controller.Loader;

                // We use reflection to access private fields and protected properties.
                var propertyInfo = typeof(EditorContentLoader).GetProperty("AssetLoadingTimeUrls", BindingFlags.Instance | BindingFlags.NonPublic);
                if (propertyInfo == null) throw new MissingFieldException("EditorContentLoader misses the 'AssetLoadingTimeUrls' protected member.");
                assetLoadingTimeUrls = (Dictionary<AssetId, string>)propertyInfo.GetValue(sceneLoader);

                propertyInfo = typeof(EditorContentLoader).GetProperty("Game", BindingFlags.Instance | BindingFlags.NonPublic);
                if (propertyInfo == null) throw new MissingFieldException("EditorContentLoader misses the 'Game' protected member.");
                game = (EditorServiceGame)propertyInfo.GetValue(sceneLoader);

                LoadingTimeUrls = new ObservableList<string>();
                LoadedAssets = new ObservableList<string>();
                RefreshCommand = new AnonymousCommand(ServiceProvider, Refresh);
            }

            public ObservableList<string> LoadingTimeUrls { get; }

            public ObservableList<string> LoadedAssets { get; }

            public ICommandBase RefreshCommand { get; private set; }

            private void Refresh()
            {
                var list = new List<string>();
                foreach (var entry in assetLoadingTimeUrls)
                {
                    var asset = editor.Session.GetAssetById(entry.Key);
                    list.Add($"{(asset != null ? asset.Url : "<Unknown: " + entry.Key + ">")} -> {entry.Value}");
                }
                LoadingTimeUrls.Clear();
                LoadingTimeUrls.AddRange(list);

                list.Clear();
                // TODO: display a fallback message when this service is not available.
                var debug = editor.Controller.GetService<IEditorGameDebugViewModelService>();
                if (debug != null)
                {
                    foreach (var entry in debug.ContentManagerStats.LoadedAssets)
                    {
                        list.Add($"{entry.Url}: Pub:{entry.PublicReferenceCount} Priv:{entry.PrivateReferenceCount}");
                    }
                }
                LoadedAssets.Clear();
                LoadedAssets.AddRange(list);
            }
        }

        public DebugEntityHierarchyEditorUserControl([NotNull] EntityHierarchyEditorViewModel editor)
        {
            DataContext = new DebugEntityHierarchyEditorViewModel(editor.ServiceProvider, editor);
            Title = $"Scene '{((IAssetEditorViewModel)editor).Asset.Url}'";
            InitializeComponent();
        }

        public string Title { get; set; }
    }
}
