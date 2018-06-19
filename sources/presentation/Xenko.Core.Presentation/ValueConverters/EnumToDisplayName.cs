// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Xenko.Core;

namespace Xenko.Core.Presentation.ValueConverters
{
    public class EnumToDisplayName : OneWayValueConverter<EnumToDisplayName>
    {
        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "(None)";

            var stringValue = value.ToString();
            var type = value.GetType();
            var memberInfo = type.GetMember(stringValue).FirstOrDefault();
            if (memberInfo == null)
                return stringValue;

            var attribute = memberInfo.GetCustomAttribute(typeof(DisplayAttribute), false) as DisplayAttribute;
            if (attribute == null)
                return stringValue;

            return attribute.Name;
        }
    }
}
