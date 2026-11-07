// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Windows;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Views;

namespace Stride.Editor
{
    /// <summary>
    /// A <see cref="StrideAssetsPlugin"/> whose enum images and template providers come from WPF resource dictionaries.
    /// </summary>
    public abstract class StrideAssetsWpfPlugin : StrideAssetsPlugin
    {
        /// <summary>
        /// Registers in <paramref name="context"/> the enum images and the template providers found in a WPF resource dictionary.
        /// </summary>
        /// <param name="context">The context of the plugin, given to <see cref="StrideAssetsPlugin.Initialize(IEditorPluginContext)"/>.</param>
        /// <param name="dictionary">The resource dictionary: its enum-keyed entries are enum images, its <see cref="ITemplateProvider"/> values template providers.</param>
        protected static void RegisterResourceDictionary(IEditorPluginContext context, ResourceDictionary dictionary)
        {
            foreach (object entry in dictionary.Keys)
            {
                if (entry is Enum enumValue)
                {
                    context.EnumImages.Register(enumValue, dictionary[entry]);
                }
            }

            foreach (object value in dictionary.Values)
            {
                if (value is ITemplateProvider provider)
                {
                    context.TemplateProviders.Register(provider);
                }
            }
        }

        /// <summary>
        /// Registers the enum images and the template providers found in a WPF resource dictionary, from
        /// <see cref="StrideAssetsPlugin.Initialize(Stride.Core.Diagnostics.ILogger)"/>.
        /// </summary>
        /// <param name="dictionary">The resource dictionary: its enum-keyed entries are enum images, its <see cref="ITemplateProvider"/> values template providers.</param>
        /// <remarks>A plugin that uses the plugin context calls <see cref="RegisterResourceDictionary(IEditorPluginContext, ResourceDictionary)"/>.</remarks>
        protected virtual void RegisterResourceDictionary(ResourceDictionary dictionary)
        {
            foreach (object entry in dictionary.Keys)
            {
                if (entry is Enum enumValue)
                {
                    RegisterEnumImage(enumValue, dictionary[entry]);
                }
            }

            foreach (object value in dictionary.Values)
            {
                if (value is ITemplateProvider provider)
                {
                    RegisterTemplateProvider(provider);
                }
            }
        }
    }
}
