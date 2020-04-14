// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.ValueConverters
{
    public class MultiBindingToTuple : OneWayMultiValueConverter<MultiBindingToTuple>
    {
        [NotNull]
        public override object Convert([NotNull] object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            switch (values.Length)
            {
                case 2: return new Tuple<object, object>(values[0], values[1]);
                case 3: return new Tuple<object, object, object>(values[0], values[1], values[2]);
                case 4: return new Tuple<object, object, object, object>(values[0], values[1], values[2], values[3]);
                case 5: return new Tuple<object, object, object, object, object>(values[0], values[1], values[2], values[3], values[4]);
                case 6: return new Tuple<object, object, object, object, object, object>(values[0], values[1], values[2], values[3], values[4], values[5]);
                case 7: return new Tuple<object, object, object, object, object, object, object>(values[0], values[1], values[2], values[3], values[4], values[5], values[6]);
                case 8: return new Tuple<object, object, object, object, object, object, object, object>(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);
                default: throw new ArgumentException("This converter supports only between two and eight elements");
            }
        }
    }
}
