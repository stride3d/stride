// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.Extensions;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.Reflection;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Presentation.View;
using Xenko.Editor.Preview;
using Xenko.Editor.Preview.ViewModel;

namespace Xenko.Editor
{
    public delegate IAssetPreview AssetPreviewFactory(IPreviewBuilder builder, PreviewGame game, AssetItem asset);

    public abstract class XenkoAssetsPlugin : AssetsPlugin
    {
        private readonly Dictionary<object, object> enumImagesDictionary = new Dictionary<object, object>();
        private readonly List<ITemplateProvider> templateProviderList = new List<ITemplateProvider>();

        protected virtual void RegisterResourceDictionary(ResourceDictionary dictionary)
        {
            foreach (object entry in dictionary.Keys)
            {
                if (entry is Enum)
                {
                    enumImagesDictionary.Add(entry, dictionary[entry]);
                }
            }

            foreach (object value in dictionary.Values)
            {
                var provider = value as ITemplateProvider;
                if (provider != null)
                {
                    templateProviderList.Add(provider);
                }
            }
        }

        protected abstract void Initialize(ILogger logger);

        /// <inheritdoc />
        public sealed override void InitializePlugin(ILogger logger)
        {
            enumImagesDictionary.Clear();
            templateProviderList.Clear();


            Initialize(logger);
        }

        /// <inheritdoc />
        public override void RegisterPrimitiveTypes(ICollection<Type> primitiveTypes)
        {
            foreach (var type in AssemblyRegistry.FindAll().SelectMany(x => x.GetTypes()))
            {
                var serializer = SerializerSelector.AssetWithReuse.GetSerializer(type);
                if (serializer != null)
                {
                    var serializerType = serializer.GetType();
                    if (serializerType.IsGenericType && serializerType.GetGenericTypeDefinition() == typeof(ReferenceSerializer<>))
                    {
                        primitiveTypes.Add(type);
                    }
                }
            }

        }

        /// <inheritdoc />
        public sealed override void RegisterEnumImages(IDictionary<object, object> enumImages)
        {
            foreach (var pair in enumImagesDictionary)
                enumImages.Add(pair);
        }

        /// <inheritdoc />
        public override void RegisterCopyProcessors(ICollection<ICopyProcessor> copyProcessors, SessionViewModel session)
        {
        }

        /// <inheritdoc />
        public override void RegisterPasteProcessors(ICollection<IPasteProcessor> pasteProcessors, SessionViewModel session)
        {
        }

        /// <inheritdoc />
        public override void RegisterPostPasteProcessors(ICollection<IAssetPostPasteProcessor> postPasteProcessors, SessionViewModel session)
        {
        }

        /// <inheritdoc />
        public override void RegisterTemplateProviders(ICollection<ITemplateProvider> templateProviders)
        {
            templateProviders.AddRange(templateProviderList);
        }

        public void RegisterAssetPreviewViewModelTypes(IDictionary<Type, Type> assetPreviewViewModelTypes)
        {
            var pluginAssembly = GetType().Assembly;
            foreach (var type in pluginAssembly.GetTypes())
            {
                if (typeof(IAssetPreviewViewModel).IsAssignableFrom(type))
                {
                    var attribute = type.GetCustomAttribute<AssetPreviewViewModelAttribute>();
                    if (attribute != null)
                    {
                        assetPreviewViewModelTypes.Add(attribute.AssetPreviewType, type);
                    }
                }
            }
        }
    }
}
