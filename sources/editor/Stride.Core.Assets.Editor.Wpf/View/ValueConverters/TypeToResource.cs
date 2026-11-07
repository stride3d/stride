// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.ValueConverters;

namespace Stride.Core.Assets.Editor.View.ValueConverters
{
    public class TypeToResource : OneWayValueConverter<TypeToResource>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return FetchResourceFromType(value as Type, true);
        }

        /// <summary>
        /// The image of <paramref name="type"/>, or of its closest base type that has one when <paramref name="tryBase"/> is
        /// set: an image of <see cref="AssetsEditorPlugin.TypeImagesDictionary"/>, then one that the plugins of the session
        /// register or the registered assemblies declare (<see cref="IAssetsPluginService.GetImageForType"/>).
        /// </summary>
        public static object FetchResourceFromType(Type type, bool tryBase)
        {
            var pluginService = type is null ? null : SessionViewModel.Instance?.ServiceProvider.TryGet<IAssetsPluginService>();
            return FetchResourceFromType(type, tryBase, pluginService);
        }

        /// <summary>
        /// The image of <paramref name="type"/>, as <see cref="FetchResourceFromType(Type, bool)"/>, with the plugin images of
        /// <paramref name="pluginService"/>.
        /// </summary>
        public static object FetchResourceFromType(Type type, bool tryBase, IAssetsPluginService pluginService)
        {
            object result = null;
            while (type != null)
            {
                if (AssetsEditorPlugin.TypeImagesDictionary.TryGetValue(type, out result))
                    break;

                if ((result = pluginService?.GetImageForType(type)) is not null)
                    break;

                if (!tryBase)
                    break;

                type = type.BaseType;
            }

            return result;
        }
    }
}
