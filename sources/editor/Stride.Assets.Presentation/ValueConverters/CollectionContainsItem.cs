// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using Stride.Core.Presentation.ValueConverters;

namespace Stride.Assets.Presentation.ValueConverters
{
    public class CollectionContainsItem: OneWayMultiValueConverter<CollectionContainsItem>
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var item = values[0];
            var collection = values[1] as IEnumerable;
            return collection != null && collection.Cast<object>().Any(x => Equals(x, item));
        }
    }
}
