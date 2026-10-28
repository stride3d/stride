// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Skyboxes;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Graphics;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// Creates a skybox, asking the user for its cubemap texture.
/// </summary>
public sealed class SkyboxFactoryTemplateGenerator : AssetFactoryTemplateGenerator
{
    private static readonly PropertyKey<Texture?> TextureKey = new("Texture", typeof(SkyboxFactoryTemplateGenerator));

    public static readonly Guid TemplateId = new("F16AB154-86A6-4020-B410-88FBD87D3CA5");

    /// <inheritdoc/>
    public override bool IsSupportingTemplate(TemplateDescription templateDescription)
    {
        ArgumentNullException.ThrowIfNull(templateDescription);
        return templateDescription.Id == TemplateId;
    }

    /// <inheritdoc/>
    protected override async Task<bool> PrepareAssetCreation(AssetTemplateGeneratorParameters parameters)
    {
        if (!await base.PrepareAssetCreation(parameters))
            return false;

        var acceptedTypes = AssetRegistry.GetAssetTypes(typeof(Texture));
        var textureViewModel = await BrowseForAssetAsync(parameters.Package, acceptedTypes, new UFile(parameters.Name).GetFullDirectory(), "Select a cubemap texture to use for this skybox - _cancel to leave the texture empty_");
        var texture = textureViewModel is not null ? ContentReferenceHelper.CreateReference<Texture>(textureViewModel) : null;
        parameters.SetTag(TextureKey, texture);
        return true;
    }

    /// <inheritdoc/>
    protected override void PostAssetCreation(AssetTemplateGeneratorParameters parameters, AssetItem assetItem)
    {
        base.PostAssetCreation(parameters, assetItem);
        var texture = parameters.TryGetTag(TextureKey);
        ((SkyboxAsset)assetItem.Asset).CubeMap = texture;
    }
}
