using System.Linq;
using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Assets.Textures;

namespace Stride.Assets.Presentation.NodePresenters.Updaters
{
    internal sealed class TextureAssetNodeUpdater : AssetNodePresenterUpdaterBase
    {
        private const string AbsoluteWidth = nameof(AbsoluteWidth);
        private const string AbsoluteHeight = nameof(AbsoluteHeight);

        protected override void UpdateNode(IAssetNodePresenter node)
        {
            var asset = node.Asset?.Asset as TextureAsset;
            if (asset != null && node.Name == nameof(TextureAsset.Width))
            {
                node.IsVisible = asset.IsSizeInPercentage;

                var absoluteWidth = node.Parent.Children.FirstOrDefault(x => x.Name == AbsoluteWidth)
                                    ?? node.Factory.CreateVirtualNodePresenter(node.Parent, AbsoluteWidth, typeof(int), node.Order,
                                        () => node.Value, node.UpdateValue, () => node.HasBase, () => node.IsInherited, () => node.IsOverridden);
                absoluteWidth.IsVisible = !asset.IsSizeInPercentage;
                absoluteWidth.AttachedProperties.Set(NumericData.MinimumKey, 0);
                absoluteWidth.AttachedProperties.Set(NumericData.MaximumKey, float.MaxValue);
                absoluteWidth.AttachedProperties.Set(NumericData.DecimalPlacesKey, 0);
            }
            if (asset != null && node.Name == nameof(TextureAsset.Height))
            {
                node.IsVisible = asset.IsSizeInPercentage;

                var absoluteHeight = node.Parent.Children.FirstOrDefault(x => x.Name == AbsoluteHeight)
                                  ?? node.Factory.CreateVirtualNodePresenter(node.Parent, AbsoluteHeight, typeof(int), node.Order,
                                  () => node.Value, node.UpdateValue, () => node.HasBase, () => node.IsInherited, () => node.IsOverridden);
                absoluteHeight.IsVisible = !asset.IsSizeInPercentage;
                absoluteHeight.AttachedProperties.Set(NumericData.MinimumKey, 0);
                absoluteHeight.AttachedProperties.Set(NumericData.MaximumKey, float.MaxValue);
                absoluteHeight.AttachedProperties.Set(NumericData.DecimalPlacesKey, 0);
            }
        }
        protected override void FinalizeTree(IAssetNodePresenter root)
        {
            var asset = root.Asset?.Asset as TextureAsset;
            if (asset != null)
            {
                var size = CategoryData.ComputeCategoryNodeName("Size");
                root[size][nameof(TextureAsset.Width)].AddDependency(root[size][nameof(TextureAsset.IsSizeInPercentage)], false);
                root[size][nameof(TextureAsset.Height)].AddDependency(root[size][nameof(TextureAsset.IsSizeInPercentage)], false);
            }
        }
    }
}