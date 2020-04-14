// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Assets.Textures;
using Xenko.Core;
using Xenko.Core.Assets.Editor.Settings;
using Xenko.Core.Assets.IO;
using Xenko.Core.Assets.Templates;
using Xenko.Core.IO;
using Xenko.Core.Reflection;

namespace Xenko.Assets.Presentation.Templates
{
    public class HeightmapFactoryTemplateGenerator : AssetFromFileTemplateGenerator
    {
        public new static readonly HeightmapFactoryTemplateGenerator Default = new HeightmapFactoryTemplateGenerator();

        public static readonly Guid TemplateId = new Guid("127EC64F-6E15-4964-98F4-DB735B39AE09");

        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            if (templateDescription == null) throw new ArgumentNullException(nameof(templateDescription));
            return templateDescription.Id == TemplateId;
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
