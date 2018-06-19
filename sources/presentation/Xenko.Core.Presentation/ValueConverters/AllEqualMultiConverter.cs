// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.Internal;

namespace Xenko.Core.Presentation.ValueConverters
{
    public class AllEqualMultiConverter : OneWayMultiValueConverter<AllEqualMultiConverter>
    {
        [NotNull]
        public override object Convert([NotNull] object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                throw new InvalidOperationException("This multi converter must be invoked with at least two elements");

            var fallbackValue = parameter is bool && (bool)parameter;
            var first = values[0];
            var result = values.All(x => x == DependencyProperty.UnsetValue ? fallbackValue : Equals(x, first));
            return result.Box();
        }
    }
}
