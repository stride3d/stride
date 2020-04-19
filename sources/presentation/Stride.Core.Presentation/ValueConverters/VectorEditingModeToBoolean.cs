// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Windows.Data;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Controls;

namespace Stride.Core.Presentation.ValueConverters
{
    [ValueConversion(typeof(VectorEditingMode), typeof(bool?))]
    public class VectorEditingModeToBool : ValueConverterBase<VectorEditingModeToBool>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var toolMode = (VectorEditingMode)System.Convert.ChangeType(value, typeof(VectorEditingMode));
            switch (toolMode)
            {
                case VectorEditingMode.Normal:
                    return false;

                case VectorEditingMode.AllComponents:
                    return true;

                case VectorEditingMode.Length:
                    return null;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [NotNull]
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return VectorEditingMode.Length;
            
            return ConverterHelper.ConvertToBoolean(value, culture) ? VectorEditingMode.AllComponents : VectorEditingMode.Normal;
        }
    }
}
