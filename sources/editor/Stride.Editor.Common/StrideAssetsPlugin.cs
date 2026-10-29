// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Presentation.Views;
using Stride.Editor.Preview;

namespace Stride.Editor
{
    public delegate IAssetPreview AssetPreviewFactory(IPreviewBuilder builder, PreviewGame game, AssetItem asset);

    /// <summary>
    /// The base class of the plugins that extend the Stride editors: it registers the types that the assets serialize as
    /// references as primitive types of the property grid; the editor registers the asset preview view models and views its
    /// assembly declares (<see cref="StrideEditorPluginHost"/>). It is toolkit-neutral; the views themselves and their
    /// resources (such as WPF resource dictionaries) belong to a companion package written for one toolkit, see
    /// <c>StrideAssetsWpfPlugin</c>.
    /// </summary>
    /// <remarks>
    /// A plugin registers what it adds in <see cref="Initialize(IEditorPluginContext)"/> and
    /// <see cref="AssetsPlugin.InitializeSession(ISessionPluginContext)"/>, calling the base method first. The enum images
    /// and template providers that a plugin written for the older API registers in <see cref="Initialize(ILogger)"/> with
    /// <see cref="RegisterEnumImage"/> and <see cref="RegisterTemplateProvider"/> still reach the editor.
    /// </remarks>
    public abstract class StrideAssetsPlugin : AssetsEditorPlugin
    {
        private readonly Dictionary<object, object> enumImagesDictionary = [];
        private readonly List<ITemplateProvider> templateProviderList = [];

        /// <summary>
        /// Registers an image for a value of an enum, shown in place of the value in the editor, from <see cref="Initialize(ILogger)"/>.
        /// </summary>
        /// <param name="value">The enum value.</param>
        /// <param name="image">The image, in the format of the editor's toolkit.</param>
        /// <remarks>A plugin that uses the plugin context registers it in <see cref="IEditorPluginContext.EnumImages"/>.</remarks>
        protected void RegisterEnumImage(Enum value, object image)
        {
            enumImagesDictionary.Add(value, image);
        }

        /// <summary>
        /// Registers a template provider for the editor's property grid, from <see cref="Initialize(ILogger)"/>.
        /// </summary>
        /// <param name="provider">The template provider.</param>
        /// <remarks>A plugin that uses the plugin context registers it in <see cref="IEditorPluginContext.TemplateProviders"/>.</remarks>
        protected void RegisterTemplateProvider(ITemplateProvider provider)
        {
            templateProviderList.Add(provider);
        }

        /// <summary>
        /// Called when the plugin joins the session, before <see cref="Initialize(IEditorPluginContext)"/>: the place where a
        /// plugin written for the older API registers its enum images and template providers.
        /// </summary>
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
        /// <remarks>
        /// Registers the types of every registered assembly that the assets serialize as references as primitive types, after
        /// the contributions of the <c>Register*</c> methods.
        /// </remarks>
        public override void Initialize(IEditorPluginContext context)
        {
            base.Initialize(context);

            foreach (var type in AssemblyRegistry.FindAll().SelectMany(x => x.GetTypes()))
            {
                var serializer = SerializerSelector.AssetWithReuse.GetSerializer(type);
                if (serializer != null)
                {
                    var serializerType = serializer.GetType();
                    if (serializerType.IsGenericType && serializerType.GetGenericTypeDefinition() == typeof(ReferenceSerializer<>))
                    {
                        context.PrimitiveTypes.Register(type);
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
    }
}
