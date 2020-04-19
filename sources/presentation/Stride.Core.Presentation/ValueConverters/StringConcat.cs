// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;

namespace Stride.Core.Presentation.ValueConverters
{
    public class StringConcat : OneWayValueConverter<StringConcat>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString() + parameter.ToString();
        }
    }
}
