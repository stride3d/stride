// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.IO;
using Stride.Core;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Templates
{
    /// <summary>
    /// A template for creating assets.
    /// </summary>
    [DataContract("TemplateAsset")]
    public class TemplateAssetDescription : TemplateDescription
    {
        public string AssetTypeName { get; set; }

        public bool RequireName { get; set; } = true;

        public bool ImportSource { get; set; }

        public Type GetAssetType()
        {
            return AssetRegistry.GetPublicTypes().FirstOrDefault(x => x.Name == AssetTypeName);
        }

        public FileExtensionCollection GetSupportedExtensions()
        {
            var allExtensions = new List<string>();
            var assetType = GetAssetType();
            foreach (var importer in AssetRegistry.RegisteredImporters)
            {
                if (importer.RootAssetTypes.Contains(assetType))
                {
                    allExtensions.Add(importer.SupportedFileExtensions);
                }
            }
            var assetTypeName = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(assetType)?.Name ?? assetType.Name;
            return new FileExtensionCollection($"Source files for {assetTypeName}", string.Join(";", allExtensions));
        }
    }

    [DataContract("TemplateAssetFactory")]
    public class TemplateAssetFactoryDescription : TemplateAssetDescription
    {
        private IAssetFactory<Asset> factory;

        public string FactoryTypeName { get; set; }

        public IAssetFactory<Asset> GetFactory()
        {
            if (factory != null)
                return factory;

            if (FactoryTypeName != null)
            {
                factory = AssetRegistry.GetAssetFactory(FactoryTypeName);
            }
            else
            {
                var assetType = GetAssetType();
                var factoryType = typeof(DefaultAssetFactory<>).MakeGenericType(assetType);
                factory = (IAssetFactory<Asset>)Activator.CreateInstance(factoryType);
            }
            return factory;
        }
    }
}
