// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Assets.Materials;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Assets.Textures;
using Stride.Editor.EditorGame.Game;
using Stride.Rendering;

namespace Stride.Assets.Presentation.AssetEditors.AssetHighlighters
{
    /// <summary>
    /// The <see cref="AssetHighlighter"/> for <see cref="TextureAsset"/>.
    /// </summary>
    [AssetHighlighter(typeof(TextureAsset))]
    public class TextureAssetHighlighter : AssetHighlighter
    {
        private readonly Dictionary<Material, Color4> highlightedMaterials = new Dictionary<Material, Color4>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureAssetHighlighter"/> class.
        /// </summary>
        /// <param name="dependencyManager">The dependency manager of the current session.</param>
        public TextureAssetHighlighter([NotNull] IAssetDependencyManager dependencyManager)
            : base(dependencyManager)
        {
        }

        /// <inheritdoc/>
        public override void Highlight(IEditorGameController controller, EditorServiceGame game, AssetItem assetItem, float duration)
        {
            var dependencies = DependencyManager.ComputeDependencies(assetItem.Id, AssetDependencySearchOptions.In, ContentLinkType.Reference);
            if (dependencies == null)
                return;
            foreach (var dependency in dependencies.LinksIn.Where(x => x.Item.Asset is MaterialAsset))
            {
                var referencer = assetItem.Package.Session.FindAsset(dependency.Item.Id);
                if (referencer == null)
                    continue;

                var material = controller.Loader.GetRuntimeObject<Material>(referencer);
                if (material != null)
                {
                    lock (highlightedMaterials)
                    {
                        highlightedMaterials[material] = DirectReferenceColor;
                    }
                    var currentMaterial = material;
                    controller.InvokeTask(() => HighlightMaterial(game, currentMaterial, duration));
                }
                var subDependencies = DependencyManager.ComputeDependencies(referencer.Id, AssetDependencySearchOptions.In | AssetDependencySearchOptions.Recursive, ContentLinkType.Reference);
                if (subDependencies == null)
                    continue;

                foreach (var subDependency in subDependencies.LinksIn.Where(x => x.Item.Asset is MaterialAsset))
                {
                    referencer = assetItem.Package.Session.FindAsset(subDependency.Item.Id);
                    if (referencer == null)
                        continue;

                    material = controller.Loader.GetRuntimeObject<Material>(referencer);
                    if (material != null)
                    {
                        lock (highlightedMaterials)
                        {
                            highlightedMaterials[material] = IndirectReferenceColor;
                        }
                        var currentMaterial = material;
                        controller.InvokeTask(() => HighlightMaterial(game, currentMaterial, duration));
                    }
                }

            }
        }

        /// <inheritdoc/>
        public override void Clear()
        {
            lock (highlightedMaterials)
            {
                highlightedMaterials.Clear();
            }
        }

        private async Task HighlightMaterial(EditorServiceGame game, Material material, float duration)
        {
            // TODO: this could be factorized between the different highlighters
            if (material == null)
                return;

            var currentTime = 0.0f;

            while (game.IsRunning)
            {
                Color4 color;
                lock (highlightedMaterials)
                {
                    if (!highlightedMaterials.TryGetValue(material, out color))
                    {
                        HighlightRenderFeature.MaterialHighlightColors.Remove(material);
                        return;
                    }
                }
                var ratio = duration >= 0 ? 1.0f - currentTime / duration : 1.0f;
                if (ratio <= 0.0f)
                {
                    lock (highlightedMaterials)
                    {
                        highlightedMaterials.Remove(material);
                    }
                    HighlightRenderFeature.MaterialHighlightColors.Remove(material);
                    return;
                }

                var currentColor = new Color4(color.R, color.G, color.B, color.A * ratio);
                HighlightRenderFeature.MaterialHighlightColors[material] = Color4.PremultiplyAlpha(currentColor);

                await game.Script.NextFrame();
                currentTime += (float)game.UpdateTime.Elapsed.TotalSeconds;
            }
        }
    }
}
