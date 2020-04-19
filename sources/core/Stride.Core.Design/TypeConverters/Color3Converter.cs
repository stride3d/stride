// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;

namespace Stride.Core.TypeConverters
{
    /// <summary>
    /// Defines a type converter for <see cref="Color3"/>.
    /// </summary>
    public class Color3Converter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Color3Converter"/> class.
        /// </summary>
        public Color3Converter()
        {
            var type = typeof(Color3);
            Properties = new PropertyDescriptorCollection(new PropertyDescriptor[]
            {
                new FieldPropertyDescriptor(type.GetField(nameof(Color3.R))),
                new FieldPropertyDescriptor(type.GetField(nameof(Color3.G))),
                new FieldPropertyDescriptor(type.GetField(nameof(Color3.B))),
            });
        }

        /// <inheritdoc/>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(Color) || destinationType == typeof(Color4) || base.CanConvertTo(context, destinationType);
        }

        /// <inheritdoc/>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null) throw new ArgumentNullException(nameof(destinationType));

            if (value is Color3)
            {
                var color = (Color3)value;

                if (destinationType == typeof(string))
                {
                    return color.ToString();
                }
                if (destinationType == typeof(Color))
                {
                    return (Color)color;
                }
                if (destinationType == typeof(Color4))
                {
                    return color.ToColor4();
                }

                if (destinationType == typeof(InstanceDescriptor))
                {
                    var constructor = typeof(Color3).GetConstructor(MathUtil.Array(typeof(float), 4));
                    if (constructor != null)
                        return new InstanceDescriptor(constructor, color.ToArray());
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <inheritdoc/>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(Color) || sourceType == typeof(Color4) || base.CanConvertFrom(context, sourceType);
        }

        /// <inheritdoc/>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is Color)
            {
                var color = (Color)value;
                return color.ToColor3();
            }
            if (value is Color4)
            {
                var color = (Color4)value;
                return color.ToColor3();
            }

            var str = value as string;
            if (str != null)
            {
                // First try to convert using StringToRgba
                if (ColorExtensions.CanConvertStringToRgba(str))
                {
                    var colorValue = ColorExtensions.StringToRgba(str);
                    return new Color3(colorValue);
                }
                // If we can't, use the default ConvertFromString method.
                return ConvertFromString<Color3, float>(context, culture, value);
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <inheritdoc/>
        [NotNull]
        public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
        {
            if (propertyValues == null) throw new ArgumentNullException(nameof(propertyValues));
            return new Color3((float)propertyValues[nameof(Color.R)], (float)propertyValues[nameof(Color.G)], (float)propertyValues[nameof(Color.B)]);
        }
    }
}
