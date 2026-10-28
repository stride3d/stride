// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Presentation.Views;
using Stride.Editor.Annotations;
using Stride.Editor.Preview;
using Stride.Editor.Preview.View;
using Stride.Editor.Preview.ViewModel;

namespace Stride.Editor
{
    public delegate IAssetPreview AssetPreviewFactory(IPreviewBuilder builder, PreviewGame game, AssetItem asset);

    /// <summary>
    /// The base class of the plugins that extend the Stride editors: it registers the asset previews, their view models
    /// and views, the enum images and the template providers of the plugin's assembly. It is toolkit-neutral; the views
    /// themselves and their resources (such as WPF resource dictionaries) belong to a companion package written for one
    /// toolkit, see <c>StrideAssetsWpfPlugin</c>.
    /// </summary>
    public abstract class StrideAssetsPlugin : AssetsEditorPlugin
    {
        private readonly Dictionary<object, object> enumImagesDictionary = [];
        private readonly List<ITemplateProvider> templateProviderList = [];

        /// <summary>
        /// Registers an image for a value of an enum, shown in place of the value in the editor.
        /// </summary>
        /// <param name="value">The enum value.</param>
        /// <param name="image">The image, in the format of the editor's toolkit.</param>
        protected void RegisterEnumImage(Enum value, object image)
        {
            enumImagesDictionary.Add(value, image);
        }

        /// <summary>
        /// Registers a template provider for the editor's property grid.
        /// </summary>
        /// <param name="provider">The template provider.</param>
        protected void RegisterTemplateProvider(ITemplateProvider provider)
        {
            templateProviderList.Add(provider);
        }

        protected virtual void Initialize(ILogger logger)
        {
        }

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
        public override void RegisterTemplateProviders(ICollection<ITemplateProvider> templateProviders)
        {
            templateProviders.AddRange(templateProviderList);
        }

        /// <inheritdoc />
        public override void RegisterAssetPreviewViewModelTypes(IDictionary<Type, Type> assetPreviewViewModelTypes)
        {
            foreach (var type in AssemblyRegistry.GetScanTypes(GetType().Assembly, typeof(IAssetPreviewViewModel)))
            {
                if (typeof(IAssetPreviewViewModel).IsAssignableFrom(type) &&
                    type.GetCustomAttribute<AssetPreviewViewModelAttribute>() is { } attribute)
                {
                    assetPreviewViewModelTypes.Add(attribute.AssetPreviewType, type);
                }
            }
        }

        /// <inheritdoc />
        public override void RegisterAssetPreviewViewTypes(IDictionary<Type, Type> assetPreviewViewTypes)
        {
            foreach (var type in AssemblyRegistry.GetScanTypes(GetType().Assembly, typeof(IPreviewView)))
            {
                if (!typeof(IPreviewView).IsAssignableFrom(type))
                    continue;

                foreach (var attribute in type.GetCustomAttributes<AssetPreviewViewAttribute>())
                {
                    assetPreviewViewTypes.Add(attribute.AssetPreviewType, type);
                }
            }
        }
    }
}
