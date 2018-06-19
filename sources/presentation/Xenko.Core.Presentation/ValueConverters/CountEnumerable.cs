// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using Xenko.Core.Annotations;

namespace Xenko.Core.Presentation.ValueConverters
{
    /// <summary>
    /// This converter will take an enumerable as input and return the number of items it contains.
    /// </summary>
    public class CountEnumerable : OneWayValueConverter<CountEnumerable>
    {
        /// <inheritdoc/>
        [NotNull]
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return 0;

            var enumerable = value as IEnumerable;
            if (enumerable == null)
                throw new ArgumentException(@"The given value must implement IEnumerable", nameof(value));

            return (value as ICollection)?.Count ?? enumerable.Cast<object>().Count();
        }
    }
}
