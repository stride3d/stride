// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.IO;
using Stride.Core.Assets.Templates;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Reflection;
using Stride.TextureConverter;
using Stride.Assets.Sprite;
using Stride.Assets.Textures;

namespace Stride.Assets.Presentation.Templates
{
    public class SpriteSheetFromFileTemplateGenerator : AssetFromFileTemplateGenerator
    {
        public new static SpriteSheetFromFileTemplateGenerator Default = new SpriteSheetFromFileTemplateGenerator();

        public static Guid SpriteSheetId = new Guid("3778061A-5621-46F0-9091-B7C7E725237D");

        public static Guid UISpriteSheetId = new Guid("EFA882D2-BA27-4E81-8553-83187DDCD6D2");

        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            return templateDescription.Id == SpriteSheetId || templateDescription.Id == UISpriteSheetId;
        }

        protected override IEnumerable<AssetItem> CreateAssets(AssetTemplateGeneratorParameters parameters)
        {
            var sources = parameters.Tags.Get(SourceFilesPathKey);
            if (sources == null)
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
                    int width = 0;
                    int height = 0;
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

            return new AssetItem(GenerateLocation(parameters), asset).Yield();
        }

        protected override async Task<IEnumerable<UFile>> BrowseForSourceFiles(TemplateAssetDescription description, bool allowMultiSelection)
        {
            var assetType = description.GetAssetType();
            var assetTypeName = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(assetType)?.Name ?? assetType.Name;
            var extensions = new FileExtensionCollection($"Source files for {assetTypeName}", TextureImporter.FileExtensions);
            var result = await BrowseForFiles(extensions, allowMultiSelection, true, InternalSettings.FileDialogLastImportDirectory.GetValue());
            if (result != null)
            {
                var list = result.ToList();
                InternalSettings.FileDialogLastImportDirectory.SetValue(list.First());
                InternalSettings.Save();
                return list;
            }
            return null;
        }

    }
}
