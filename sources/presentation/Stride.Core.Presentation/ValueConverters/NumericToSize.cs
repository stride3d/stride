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
    /// This converter will convert a double value to a <see cref="Size"/> structure.
    /// A <see cref="Size"/> must be passed as a parameter of this converter. You can use the <see cref="MarkupExtensions.SizeExtension"/>
    /// markup extension to easily pass one, with the following syntax: {sd:Size (arguments)}. The resulting size will
    /// be the parameter size multiplied bu the scalar double value.
    /// </summary>
    [ValueConversion(typeof(double), typeof(Size))]
    public class NumericToSize : ValueConverterBase<NumericToSize>
    {
        /// <inheritdoc/>
        [NotNull]
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double scalar;
            try
            {
                scalar = ConverterHelper.ConvertToDouble(value, culture);
            }
            catch (Exception exception)
            {
                throw new ArgumentException("The value of this converter must be convertible to a double.", exception);
            }

            if (!(parameter is Size))
            {
                throw new ArgumentException("The parameter of this converter must be an instance of the Size structure. Use {sd:Size (arguments)} to construct one.");
            }

            var size = (Size)parameter;
            var result = new Size(size.Width * scalar, size.Height * scalar);
            return result;
        }

        /// <inheritdoc/>
        public override object ConvertBack(object value, [NotNull] Type targetType, object parameter, CultureInfo culture)
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

            var scalar = 0.0;
            if (Math.Abs(sizeParameter.Width) > double.Epsilon)
            {
                scalar = sizeValue.Width / sizeParameter.Width;
            }
            else if (Math.Abs(sizeParameter.Height) > double.Epsilon)
            {
                scalar = sizeValue.Height / sizeParameter.Height;
            }

            return System.Convert.ChangeType(scalar, targetType);
        }
    }
}
