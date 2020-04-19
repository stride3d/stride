// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Assets.Models;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.Assets.Presentation.AssetEditors.AssetHighlighters
{
    /// <summary>
    /// The <see cref="AssetHighlighter"/> for <see cref="ModelAsset"/> and <see cref="ProceduralModelAsset"/>.
    /// </summary>
    [AssetHighlighter(typeof(ModelAsset))]
    [AssetHighlighter(typeof(ProceduralModelAsset))]
    public class ModelAssetHighlighter : AssetHighlighter
    {
        private readonly Dictionary<Mesh, Color4> highlightedMeshes = new Dictionary<Mesh, Color4>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelAssetHighlighter"/> class.
        /// </summary>
        /// <param name="dependencyManager">The dependency manager of the current session.</param>
        public ModelAssetHighlighter([NotNull] IAssetDependencyManager dependencyManager)
            : base(dependencyManager)
        {
        }

        /// <inheritdoc/>
        public override void Highlight(IEditorGameController controller, EditorServiceGame game, AssetItem assetItem, float duration)
        {
            var model = controller.Loader.GetRuntimeObject<Model>(assetItem);
            if (model != null)
            {
                lock (highlightedMeshes)
                {
                    foreach (var mesh in model.Meshes)
                    {
                        highlightedMeshes[mesh] = DirectReferenceColor;
                        var currentMesh = mesh;
                        controller.InvokeTask(() => HighlightMesh(game, currentMesh, duration));
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void Clear()
        {
            lock (highlightedMeshes)
            {
                highlightedMeshes.Clear();
            }
        }

        private async Task HighlightMesh(Game game, Mesh mesh, float duration)
        {
            // TODO: this could be factorized between the different highlighters
            if (mesh == null)
                return;

            var currentTime = 0.0f;

            while (game.IsRunning)
            {
                Color4 color;
                lock (highlightedMeshes)
                {
                    if (!highlightedMeshes.TryGetValue(mesh, out color))
                    {
                        HighlightRenderFeature.MeshHighlightColors.Remove(mesh);
                        return;
                    }
                }
                var ratio = duration >= 0 ? 1.0f - currentTime / duration : 1.0f;
                if (ratio <= 0.0f)
                {
                    lock (highlightedMeshes)
                    {
                        highlightedMeshes.Remove(mesh);
                    }
                    HighlightRenderFeature.MeshHighlightColors.Remove(mesh);
                    return;
                }

                var currentColor = new Color4(color.R, color.G, color.B, color.A * ratio);
                HighlightRenderFeature.MeshHighlightColors[mesh] = Color4.PremultiplyAlpha(currentColor);

                await game.Script.NextFrame();
                currentTime += (float)game.UpdateTime.Elapsed.TotalSeconds;
            }
        }
    }
}
