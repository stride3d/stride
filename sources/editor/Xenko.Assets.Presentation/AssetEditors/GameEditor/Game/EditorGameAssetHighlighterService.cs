// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Annotations;
using Xenko.Core.Extensions;
using Xenko.Assets.Presentation.AssetEditors.AssetHighlighters;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.Services;
using Xenko.Editor.EditorGame.Game;

namespace Xenko.Assets.Presentation.AssetEditors.GameEditor.Game
{
    public class EditorGameAssetHighlighterService : EditorGameServiceBase, IEditorGameAssetHighlighterViewModelService
    {
        private readonly IEditorGameController controller;
        private readonly Dictionary<Type, AssetHighlighter> assetHighlighters = new Dictionary<Type, AssetHighlighter>();
        private EditorServiceGame game;

        public EditorGameAssetHighlighterService(IEditorGameController controller, [NotNull] IAssetDependencyManager dependencyManager)
        {
            this.controller = controller;
            foreach (var assetHighlighterType in XenkoDefaultAssetsPlugin.AssetHighlighterTypesDictionary)
            {
                var instance = (AssetHighlighter)Activator.CreateInstance(assetHighlighterType.Value, dependencyManager);
                assetHighlighters.Add(assetHighlighterType.Key, instance);
            }
        }

        /// <inheritdoc/>
        public override Task DisposeAsync()
        {
            EnsureNotDestroyed(nameof(EditorGameAssetHighlighterService));
            assetHighlighters.Select(x => x.Value).ForEach(x => x.Clear());
            return base.DisposeAsync();
        }

        public void HighlightAssets(IEnumerable<AssetViewModel> assets)
        {
            const float duration = 1.0f;
            var assetItems = assets.Select(x => x.AssetItem).ToList();
            controller.InvokeAsync(() =>
            {
                assetHighlighters.Select(x => x.Value).ForEach(x => x.Clear());

                foreach (var assetItem in assetItems)
                {
                    AssetHighlighter highlighter;
                    if (assetHighlighters.TryGetValue(assetItem.Asset.GetType(), out highlighter))
                    {
                        highlighter.Highlight(controller, game, assetItem, duration);
                    }
                }
            });
        }

        protected override Task<bool> Initialize(EditorServiceGame editorGame)
        {
            game = editorGame;
            return Task.FromResult(true);
        }
    }
}
