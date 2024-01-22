// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Windows.Data;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// An abstract implementation of <see cref="MultiValueConverterBase{T}"/> that does not support <see cref="ConvertBack"/>.
    /// Invoking <see cref="ConvertBack"/> on this value converter will throw a <see cref="NotSupportedException"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IMultiValueConverter"/> being implemented.</typeparam>
    public abstract class OneWayMultiValueConverter<T> : MultiValueConverterBase<T> where T : class, IMultiValueConverter, new()
    {
        /// <inheritdoc/>
        public sealed override object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ConvertBack is not supported with this ValueConverter.");
        }
    }
}
