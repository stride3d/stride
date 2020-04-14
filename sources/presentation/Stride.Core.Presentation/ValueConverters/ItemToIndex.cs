// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Extensions;

namespace Stride.Core.Presentation.ValueConverters
{
    public class ItemToIndex : ValueConverterBase<ItemToIndex>
    {
        /// <inheritdoc/>
        [NotNull]
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var collection = parameter as IList;
            if (collection == null || collection.Count <= 0)
                return -1;

            // 1st attempt using the default item lookup
            var res = collection.IndexOf(value);
            if (res != -1 || value == null)
                return res;

            try
            {
                // 2nd attempt using a normalizing conversion (to double):
                var asDoubles = collection.Cast<object>().Select(x => (double)System.Convert.ChangeType(x, typeof(double))).ToList();
                Debug.Assert(asDoubles.SequenceEqual(asDoubles.OrderBy(d => d)));

                var search = asDoubles.BinarySearch((double)value);
                if (search < 0) // Note: BinarySearch returns a 1-complement of the index when an exact match is not found.
                    search = Math.Min(~search, collection.Count - 1);
                return search;
            }
            catch (FormatException) { }
            catch (InvalidCastException) { }
            catch (OverflowException) { }

            return -1;
        }

        /// <inheritdoc/>
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var collection = parameter as IList;
            if (collection == null)
                return null;

            var index = ConverterHelper.ConvertToInt32(value ?? -1, culture);
            if (index < 0 || index >= collection.Count)
                return null;

            return collection[index];
        }
    }
}
