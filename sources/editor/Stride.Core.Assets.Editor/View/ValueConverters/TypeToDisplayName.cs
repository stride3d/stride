// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Stride.Core;
using Stride.Core.Reflection;
using Stride.Core.Presentation.ValueConverters;

namespace Stride.Core.Assets.Editor.View.ValueConverters
{
    public class TypeToDisplayName : OneWayValueConverter<TypeToDisplayName>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Type ? DisplayAttribute.GetDisplayName((Type)value) : "None";
        }
    }
}
