// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using Xenko.Core.Presentation.ValueConverters;

namespace Xenko.Assets.Presentation.ValueConverters
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
