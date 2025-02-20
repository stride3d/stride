// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// -----------------------------------------------------------------------------
// Original code from SlimMath project. http://code.google.com/p/slimmath/
// Greetings to SlimDX Group. Original code published with the following license:
// -----------------------------------------------------------------------------
/*
* Copyright (c) 2007-2011 SlimDX Group
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/

using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using Stride.Core.Mathematics;

namespace Stride.Core.TypeConverters;

/// <summary>
/// Defines a type converter for <see cref="Color4"/>.
/// </summary>
public class Color4Converter : BaseConverter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Color4Converter"/> class.
    /// </summary>
    public Color4Converter()
    {
        var type = typeof(Color4);
        Properties = new PropertyDescriptorCollection(
        [
            new FieldPropertyDescriptor(type.GetField(nameof(Color4.R))!),
            new FieldPropertyDescriptor(type.GetField(nameof(Color4.G))!),
            new FieldPropertyDescriptor(type.GetField(nameof(Color4.B))!),
            new FieldPropertyDescriptor(type.GetField(nameof(Color4.A))!),
        ]);
    }

    /// <inheritdoc/>
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(Color) || destinationType == typeof(Color3) || base.CanConvertTo(context, destinationType);
    }

    /// <inheritdoc/>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(destinationType);
#else
        if (destinationType is null) throw new ArgumentNullException(nameof(destinationType));
#endif

        if (value is Color4 color4)
        {
            if (destinationType == typeof(string))
            {
                return color4.ToString();
            }
            if (destinationType == typeof(Color))
            {
                return (Color)color4;
            }
            if (destinationType == typeof(Color3))
            {
                return color4.ToColor3();
            }
            if (destinationType == typeof(InstanceDescriptor))
            {
                var constructor = typeof(Color4).GetConstructor(MathUtil.Array(typeof(float), 4));
                if (constructor != null)
                    return new InstanceDescriptor(constructor, color4.ToArray());
            }
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }

    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(Color) || sourceType == typeof(Color3) || base.CanConvertFrom(context, sourceType);
    }

    /// <inheritdoc/>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        switch (value)
        {
            case Color color2:
                return color2.ToColor4();
            case Color3 color3:
                return color3.ToColor4();
            case string str:
                // First try to convert using StringToRgba
                if (ColorExtensions.CanConvertStringToRgba(str))
                {
                    var colorValue = ColorExtensions.StringToRgba(str);
                    return new Color4(colorValue);
                }
                // If we can't, use the default ConvertFromString method.
                return ConvertFromString<Color4, float>(context, culture, str);

            default:
                return base.ConvertFrom(context, culture, value);
        }
    }

    /// <inheritdoc/>
    public override object CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(propertyValues);
#else
        if (propertyValues is null) throw new ArgumentNullException(nameof(propertyValues));
#endif
        return new Color4(
            (float)propertyValues[nameof(Color.R)]!,
            (float)propertyValues[nameof(Color.G)]!,
            (float)propertyValues[nameof(Color.B)]!,
            (float)propertyValues[nameof(Color.A)]!);
    }
}
