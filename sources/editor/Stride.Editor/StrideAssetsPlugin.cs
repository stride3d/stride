// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Presentation.View;
using Stride.Editor.Preview;
using Stride.Editor.Preview.ViewModel;

namespace Stride.Editor
{
    public delegate IAssetPreview AssetPreviewFactory(IPreviewBuilder builder, PreviewGame game, AssetItem asset);

    public abstract class StrideAssetsPlugin : AssetsPlugin
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
