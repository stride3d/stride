// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.ValueConverters;

namespace Stride.Core.Assets.Editor.View.ValueConverters
{
    public class TypeToResource : OneWayValueConverter<TypeToResource>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return FetchResourceFromType(value as Type, true);
        }

        public static object FetchResourceFromType(Type type, bool tryBase)
        {
            object result = null;
            while (type != null)
            {
                if (AssetsPlugin.TypeImagesDictionary.TryGetValue(type, out result))
                    break;

                type = type.BaseType;
            }

            return result;
        }
    }
}
