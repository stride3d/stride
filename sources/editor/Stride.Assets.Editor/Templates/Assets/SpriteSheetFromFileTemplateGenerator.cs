// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Sprite;
using Stride.Assets.Textures;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.IO;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Reflection;
using Stride.TextureConverter;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// Creates a sprite sheet (for sprites or for UI) with one sprite per picked image, sized like the image.
/// </summary>
public sealed class SpriteSheetFromFileTemplateGenerator : AssetFromFileTemplateGenerator
{
    public static readonly Guid SpriteSheetId = new("3778061A-5621-46F0-9091-B7C7E725237D");

    public static readonly Guid UISpriteSheetId = new("EFA882D2-BA27-4E81-8553-83187DDCD6D2");

    /// <inheritdoc/>
    public override bool IsSupportingTemplate(TemplateDescription templateDescription)
    {
        ArgumentNullException.ThrowIfNull(templateDescription);
        return templateDescription.Id == SpriteSheetId || templateDescription.Id == UISpriteSheetId;
    }

    /// <inheritdoc/>
    protected override IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters)
    {
        var sources = parameters.Tags.Get(SourceFilesPathKey);
        if (sources is null)
            return base.CreateAssets(parameters);

        SpriteSheetAsset asset;
        if (parameters.Description.Id == SpriteSheetId)
            asset = SpriteSheetSprite2DFactory.Create();
        else if (parameters.Description.Id == UISpriteSheetId)
            asset = SpriteSheetUIFactory.Create();
        else
            throw new ArgumentException("Invalid template description for this generator.");

        using (var textureTool = new TextureTool())
        {
            foreach (var source in sources)
            {
                var width = 0;
                var height = 0;
                parameters.Logger.Verbose($"Processing image \"{source}\"");
                try
                {
                    var image = textureTool.Load(source.ToString(), false);
                    width = image.Width;
                    height = image.Height;
                }
                catch (Exception)
                {
                    parameters.Logger.Warning($"Unable to retrieve the size of \"{source}\"");
                }
                var spriteInfo = new SpriteInfo
                {
                    Name = source.GetFileNameWithoutExtension(),
                    TextureRegion = new Rectangle(0, 0, width, height),
                    Source = source
                };
                asset.Sprites.Add(spriteInfo);
            }
        }

        return [new AssetItem(GenerateLocation(parameters), asset)];
    }

    /// <inheritdoc/>
    protected override Task<IEnumerable<UFile>?> BrowseForSourceFilesAsync(TemplateAssetDescription description, bool allowMultiSelection)
    {
        // The images of the sprites, not the sources of a sprite sheet (there is no importer for it)
        var assetType = description.GetAssetType();
        var assetTypeName = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(assetType)?.Name ?? assetType.Name;
        var extensions = new FileExtensionCollection($"Source files for {assetTypeName}", TextureImporter.FileExtensions);
        return BrowseForSourceFilesAsync(extensions, allowMultiSelection);
    }
}
