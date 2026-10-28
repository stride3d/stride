// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Rendering;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// Creates an asset derived from <see cref="TemplateAssetFactoryDescription.DerivedFrom"/>, or a fresh asset when that one is not found.
/// </summary>
public sealed class DerivedAssetTemplateGenerator : AssetFactoryTemplateGenerator
{
    /// <inheritdoc/>
    public override bool IsSupportingTemplate(TemplateDescription templateDescription)
    {
        ArgumentNullException.ThrowIfNull(templateDescription);
        return templateDescription is TemplateAssetFactoryDescription { DerivedFrom: not null };
    }

    /// <inheritdoc/>
    protected override IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters)
    {
        var archetype = parameters.Package.FindAsset(((TemplateAssetFactoryDescription)parameters.Description).DerivedFrom!);
        var assetItems = archetype is null
            ? base.CreateAssets(parameters).ToList()
            : [new AssetItem(GenerateLocation(parameters), archetype.CreateDerivedAsset())];

        // A new compositor gets the render features of its project's packages
        foreach (var assetItem in assetItems)
        {
            if (assetItem.Asset is GraphicsCompositorAsset compositor)
                RenderFeatureProviders.AddPackageRenderFeatures(compositor, assetItem.YamlMetadata, PackageTypeScope.For(parameters.Package));
        }
        return assetItems;
    }
}
