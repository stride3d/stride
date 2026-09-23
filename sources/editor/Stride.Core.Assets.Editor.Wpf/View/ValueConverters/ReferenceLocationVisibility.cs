// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Windows;

using Stride.Core.Presentation.ValueConverters;

namespace Stride.Core.Assets.Editor.View.ValueConverters
{
    /// <summary>
    /// Visibility of the dimmed location prefix (and its slash) in a reference field. Visible only when the
    /// location is non-empty AND enough width remains after the asset name — so the name keeps priority and
    /// the prefix is dropped whole rather than leaving a lone separator when space runs out.
    /// Values: [0] container width, [1] name width, [2] location string. Parameter: minimum remaining width.
    /// </summary>
    public class ReferenceLocationVisibility : OneWayMultiValueConverter<ReferenceLocationVisibility>
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3 || values[2] is not string location || string.IsNullOrEmpty(location))
                return Visibility.Collapsed;

            var containerWidth = System.Convert.ToDouble(values[0], culture);
            var nameWidth = System.Convert.ToDouble(values[1], culture);
            var threshold = parameter != null ? System.Convert.ToDouble(parameter, culture) : 0.0;
            return containerWidth - nameWidth >= threshold ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
