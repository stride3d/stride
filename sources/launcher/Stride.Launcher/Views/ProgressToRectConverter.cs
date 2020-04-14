// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Linq;
using System.Windows;

using Stride.Core.Presentation.ValueConverters;

namespace Stride.LauncherApp.Views
{
    public class ProgressToRectConverter : OneWayMultiValueConverter<ProgressToRectConverter>
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(x => x == DependencyProperty.UnsetValue))
                return new Rect(0, 0, 1, 1);

            var width = (double)values[0];
            var height = (double)values[1];
            var progress = (double)System.Convert.ChangeType(values[2], typeof(double));
            return new Rect(0, 0, width * (progress > 0 ? progress * 0.01 : 1.0), height) ;
        }
    }
}
