// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

public sealed class TypeToResource : OneWayValueConverter<TypeToResource>
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return FetchResourceFromType(value as Type, true);
    }

    public static object? FetchResourceFromType(Type? type, bool tryBase)
    {
        object? result = null;
        while (type is not null)
        {
            // FIXME xplat-editor
            //if (AssetsEditorPlugin.TypeImagesDictionary.TryGetValue(type, out result))
            //    break;

            type = type.BaseType;
        }

        return result;
    }
}
