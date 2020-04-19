// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Linq;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.ValueConverters
{
    public class SumMultiConverter : OneWayMultiValueConverter<SumMultiConverter>
    {
        [NotNull]
        public override object Convert([NotNull] object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                throw new InvalidOperationException("This multi converter must be invoked with at least two elements");

            var result = 1.0;
            try
            {
                result = values.Select(x => ConverterHelper.ConvertToDouble(x, culture)).Aggregate(result, (current, next) => current + next);
            }
            catch (Exception exception)
            {
                throw new ArgumentException("The values of this converter must be convertible to a double.", exception);
            }

            return result;
        }
    }
}
