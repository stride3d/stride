// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Windows;
using Stride.Core.Presentation.Views;

namespace Stride.Editor
{
    /// <summary>
    /// A <see cref="StrideAssetsPlugin"/> whose enum images and template providers come from WPF resource dictionaries.
    /// </summary>
    public abstract class StrideAssetsWpfPlugin : StrideAssetsPlugin
    {
        /// <summary>
        /// Registers the enum images and the template providers found in a WPF resource dictionary.
        /// </summary>
        /// <param name="dictionary">The resource dictionary: its enum-keyed entries are enum images, its <see cref="ITemplateProvider"/> values template providers.</param>
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
