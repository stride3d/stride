// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Windows;
using Stride.Core.Presentation.Drawing;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// This converter will convert a toolkit-free <see cref="DrawingRect"/> to a WPF <see cref="Rect"/>.
    /// </summary>
    public sealed class DrawingRectToRect : OneWayValueConverter<DrawingRectToRect>
    {
        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is DrawingRect rect ? rect.ToWpf() : Rect.Empty;
        }
    }
}
