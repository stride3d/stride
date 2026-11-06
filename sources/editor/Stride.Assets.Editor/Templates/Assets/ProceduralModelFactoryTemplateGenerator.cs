// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Models;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Rendering;
using Stride.Rendering.ProceduralModels;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// Creates a procedural model (a cube, a sphere...), asking the user for its material.
/// </summary>
public sealed class ProceduralModelFactoryTemplateGenerator : AssetFactoryTemplateGenerator
{
    private static readonly PropertyKey<Material?> MaterialKey = new("Material", typeof(ProceduralModelFactoryTemplateGenerator));

    public static readonly Guid TemplateId = new("8267F08C-3DC8-48F4-81EA-4888A1CEF9CE");

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

        var acceptedTypes = AssetRegistry.GetAssetTypes(typeof(Material));
        var materialViewModel = await BrowseForAssetAsync(parameters.Package, acceptedTypes, new UFile(parameters.Name).GetFullDirectory(), "Select a material to use for this model - _cancel to leave the material empty_");
        var material = materialViewModel is not null ? ContentReferenceHelper.CreateReference<Material>(materialViewModel) : null;
        parameters.SetTag(MaterialKey, material);
        return true;
    }

    /// <inheritdoc/>
    protected override void PostAssetCreation(AssetTemplateGeneratorParameters parameters, AssetItem assetItem)
    {
        base.PostAssetCreation(parameters, assetItem);
        var material = parameters.TryGetTag(MaterialKey);
        ((PrimitiveProceduralModelBase)((ProceduralModelAsset)assetItem.Asset).Type).MaterialInstance.Material = material;
    }
}
