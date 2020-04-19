// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Windows;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// A converter that resolve the specified value from the resources from the current application
    /// </summary>
    public class StaticResourceConverter : OneWayValueConverter<StaticResourceConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Application.Current.TryFindResource(value) ?? DependencyProperty.UnsetValue;
        }
    }
}
