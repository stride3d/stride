// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// This converter will sum a given <see cref="Size"/> with a <see cref="Size"/> passed as parameter. You can use the <see cref="MarkupExtensions.SizeExtension"/>
    /// markup extension to easily pass one, with the following syntax: {sd:Size (arguments)}. 
    /// </summary>
    [ValueConversion(typeof(Size), typeof(Size))]
    public class SumSize : ValueConverterBase<SumSize>
    {
        /// <inheritdoc/>
        [NotNull]
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Size))
            {
                throw new ArgumentException("The value of the ConvertBack method of this converter must be a an instance of the Size structure.");
            }
            if (!(parameter is Size))
            {
                throw new ArgumentException("The parameter of the ConvertBack method of this converter must be a an instance of the Size structure.");
            }

            var sizeValue = (Size)value;
            var sizeParameter = (Size)parameter;
            var result = new Size(sizeValue.Width + sizeParameter.Width, sizeValue.Height + sizeParameter.Height);
            return result;
        }

        /// <inheritdoc/>
        [NotNull]
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Size))
            {
                throw new ArgumentException("The value of the ConvertBack method of this converter must be a an instance of the Size structure.");
            }
            if (!(parameter is Size))
            {
                throw new ArgumentException("The parameter of the ConvertBack method of this converter must be a an instance of the Size structure.");
            }
            var sizeValue = (Size)value;
            var sizeParameter = (Size)parameter;

            var result = new Size(sizeValue.Width - sizeParameter.Width, sizeValue.Height - sizeParameter.Height);
            return result;
        }
    }
}
