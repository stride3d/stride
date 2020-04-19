// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Rendering.Images;

namespace Stride.Assets.Presentation.NodePresenters.Updaters
{
    internal sealed class FXAAEffectNodeUpdater : NodePresenterUpdaterBase
    {
        public override void UpdateNode(INodePresenter node)
        {
            // Adjust Quality range depending on Dither mode
            // Note: other part of the behavior is in GraphicsCompositorViewModel.OnAssetPropertyChanged (clamp Quality based on Dither)
            var fxaaEffect = node.Value as FXAAEffect;
            if (fxaaEffect != null)
            {
                var ditherNode = node[nameof(FXAAEffect.Dither)];

                // If dither type changes, we will need to update quality sliders again
                node.AddDependency(ditherNode, false);

                // Adjust quality range according to dither level
                var (minQuality, maxQuality) = FXAAEffect.GetQualityRange(fxaaEffect.Dither);
                node[nameof(FXAAEffect.Quality)].AttachedProperties[NumericData.MinimumKey] = minQuality;
                node[nameof(FXAAEffect.Quality)].AttachedProperties[NumericData.MaximumKey] = maxQuality;

                // FXAA: Hide Quality if Dither is set to None (only 9 is a valid value)
                if (fxaaEffect.Dither == FXAAEffect.DitherType.None)
                {
                    node[nameof(FXAAEffect.Quality)].IsVisible = false;
                }
            }
        }
    }
}