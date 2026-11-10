// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;
using System.Windows;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.ValueConverters;

namespace Stride.Assets.Presentation.ValueConverters
{
    /// <summary>
    /// Turns the borders (<see cref="Vector4"/>: left, top, right, bottom) or the center (<see cref="Vector2"/>: left, top) of a
    /// sprite of the shared sprite editor into a <see cref="Thickness"/>, plus the <see cref="Thickness"/> given as parameter.
    /// </summary>
    public sealed class SpriteVectorToThickness : OneWayValueConverter<SpriteVectorToThickness>
    {
        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var thickness = value switch
            {
                Vector4 borders => new Thickness(borders.X, borders.Y, borders.Z, borders.W),
                Vector2 center => new Thickness(center.X, center.Y, 0.0, 0.0),
                _ => new Thickness(),
            };
            if (parameter is Thickness offset)
            {
                thickness = new Thickness(thickness.Left + offset.Left, thickness.Top + offset.Top, thickness.Right + offset.Right, thickness.Bottom + offset.Bottom);
            }
            return thickness;
        }
    }
}
